using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Timberborn.BaseComponentSystem;
using Timberborn.BlockSystem;
using Timberborn.EntitySystem;
using Timberborn.Persistence;
using Timberborn.TerrainSystem;
using Timberborn.TickSystem;
using Timberborn.WorldPersistence;
using Timberborn.WaterSystem;
using UnityEngine;

namespace FulgurFangs.Code.Hydraulics;

public sealed class HydraulicTransferComponent : TickableComponent, IAwakableComponent, IInitializableEntity, IPostPlacementChangeListener, IFinishedStateListener, IPersistentEntity
{
    private const float WaterUpperSafetySpace = 0.1f;
    private const float Epsilon = 0.0001f;
    private const int DebugLogIntervalTicks = 30;
    private static readonly BlockOccupations InvalidOccupations = ~(BlockOccupations.Floor | BlockOccupations.Corners);
    private static readonly ComponentKey SaveKey = new("FulgurFangs.HydraulicTransfer");
    private static readonly PropertyKey<float> ThrottleKey = new("Throttle");

    private readonly ITerrainService _terrainService;
    private readonly IBlockService _blockService;
    private readonly ITickService _tickService;
    private readonly IThreadSafeWaterMap _threadSafeWaterMap;
    private readonly IWaterService _waterService;
    private readonly List<BlockObject> _blockObjectCache = new();
    private BlockObject? _blockObject;
    private bool _isFinished;
    private bool _loadedFromSave;
    private ImmutableArray<Vector3Int> _intakeCoordinates = ImmutableArray<Vector3Int>.Empty;
    private ImmutableArray<Vector3Int> _outputCoordinates = ImmutableArray<Vector3Int>.Empty;
    private ImmutableArray<Vector3Int> _transformedIntakeCoordinates = ImmutableArray<Vector3Int>.Empty;
    private ImmutableArray<Vector3Int> _transformedOutputCoordinates = ImmutableArray<Vector3Int>.Empty;
    private float _maxWaterPerSecond;
    private float _defaultThrottle = 1f;
    private float _throttleStep = 0.05f;
    private int _intakeMaxDepth = 4;
    private int _outputMaxDrop = 4;
    private bool _moveCleanWater = true;
    private bool _moveContaminatedWater = true;
    private float _throttle = 1f;
    private int _tickCounter;
    private int _ticksSinceLastDebugLog = DebugLogIntervalTicks;
    private string _lastDebugState = string.Empty;

    public HydraulicTransferComponent(
        ITerrainService terrainService,
        IBlockService blockService,
        ITickService tickService,
        IThreadSafeWaterMap threadSafeWaterMap,
        IWaterService waterService)
    {
        _terrainService = terrainService;
        _blockService = blockService;
        _tickService = tickService;
        _threadSafeWaterMap = threadSafeWaterMap;
        _waterService = waterService;
    }

    public float Throttle => _throttle;

    public float MaxWaterPerSecond => _maxWaterPerSecond;

    public float FlowLimitPerSecond => _maxWaterPerSecond * _throttle;

    public float ThrottleStep => _throttleStep;

    public float CurrentTransferPerSecond { get; private set; }

    public float CurrentTransferPerTick { get; private set; }

    public float CurrentCleanTransferPerSecond { get; private set; }

    public float CurrentContaminatedTransferPerSecond { get; private set; }

    public int IntakePortCount => _transformedIntakeCoordinates.Length;

    public int OutputPortCount => _transformedOutputCoordinates.Length;

    public void Awake()
    {
        _blockObject = GetComponent<BlockObject>() ?? Transform.GetComponentInParent<BlockObject>();
        _isFinished = _blockObject != null && _blockObject.IsFinished;
        if (!_isFinished)
        {
            DisableComponent();
        }
    }

    public void InitializeEntity()
    {
        UpdateTransformedCoordinates();
        if (!_loadedFromSave)
        {
            _throttle = Mathf.Clamp01(_defaultThrottle);
        }

        LogDebugSnapshot("InitializeEntity");
    }

    public void OnPostPlacementChanged()
    {
        UpdateTransformedCoordinates();
        LogDebugSnapshot("OnPostPlacementChanged");
    }

    public override void Tick()
    {
        _tickCounter++;
        _ticksSinceLastDebugLog++;

        if (!_isFinished)
        {
            ResetTransferStats();
            MaybeLogTick("not-finished");
            return;
        }

        float tickSeconds = Mathf.Max(0f, _tickService.TickIntervalInSeconds);
        float requestedTransfer = tickSeconds * _maxWaterPerSecond * _throttle;
        if (tickSeconds <= 0f ||
            requestedTransfer <= Epsilon ||
            _transformedIntakeCoordinates.IsDefaultOrEmpty ||
            _transformedOutputCoordinates.IsDefaultOrEmpty ||
            (!_moveCleanWater && !_moveContaminatedWater))
        {
            ResetTransferStats();
            MaybeLogTick("invalid-request");
            return;
        }

        List<IntakePortState> intakeStates = CollectIntakeStates();
        if (intakeStates.Count == 0)
        {
            ResetTransferStats();
            MaybeLogTick("no-intake");
            return;
        }

        List<OutputPortState> outputStates = CollectOutputStates();
        if (outputStates.Count == 0)
        {
            ResetTransferStats();
            MaybeLogTick("no-output");
            return;
        }

        float totalCleanAvailable = intakeStates.Sum(static state => state.CleanAvailable);
        float totalContaminatedAvailable = intakeStates.Sum(static state => state.ContaminatedAvailable);
        float totalAvailable = totalCleanAvailable + totalContaminatedAvailable;
        float totalOutputSpace = outputStates.Sum(static state => state.AvailableSpace);
        float actualTransfer = Mathf.Min(requestedTransfer, totalAvailable, totalOutputSpace);
        if (actualTransfer <= Epsilon)
        {
            ResetTransferStats();
            MaybeLogTick("no-transfer");
            return;
        }

        float cleanTransfer = Mathf.Min(actualTransfer * GetCleanRatio(totalCleanAvailable, totalContaminatedAvailable), totalCleanAvailable);
        float contaminatedTransfer = Mathf.Min(actualTransfer - cleanTransfer, totalContaminatedAvailable);
        float normalizedTransfer = cleanTransfer + contaminatedTransfer;
        if (normalizedTransfer <= Epsilon)
        {
            ResetTransferStats();
            return;
        }

        RemoveWaterFromInputs(intakeStates, cleanTransfer, contaminatedTransfer);
        AddWaterToOutputs(outputStates, cleanTransfer, contaminatedTransfer, normalizedTransfer);

        CurrentTransferPerTick = normalizedTransfer;
        CurrentTransferPerSecond = normalizedTransfer / tickSeconds;
        CurrentCleanTransferPerSecond = cleanTransfer / tickSeconds;
        CurrentContaminatedTransferPerSecond = contaminatedTransfer / tickSeconds;
        MaybeLogTick("transferring");
    }

    public void Save(IEntitySaver entitySaver)
    {
        entitySaver.GetComponent(SaveKey).Set(ThrottleKey, _throttle);
    }

    public void Load(IEntityLoader entityLoader)
    {
        if (entityLoader.TryGetComponent(SaveKey, out IObjectLoader componentLoader))
        {
            _loadedFromSave = true;
            _throttle = Mathf.Clamp01(componentLoader.Get(ThrottleKey));
        }
    }

    public void SetParameters(HydraulicTransferSpec spec)
    {
        _intakeCoordinates = spec.IntakeCoordinates;
        _outputCoordinates = spec.OutputCoordinates;
        _maxWaterPerSecond = Mathf.Max(0f, spec.MaxWaterPerSecond);
        _defaultThrottle = Mathf.Clamp01(spec.DefaultThrottle);
        _throttleStep = Mathf.Max(0.001f, spec.ThrottleStep);
        _intakeMaxDepth = Mathf.Max(0, spec.IntakeMaxDepth);
        _outputMaxDrop = Mathf.Max(0, spec.OutputMaxDrop);
        _moveCleanWater = spec.MoveCleanWater;
        _moveContaminatedWater = spec.MoveContaminatedWater;
        if (!_loadedFromSave)
        {
            _throttle = _defaultThrottle;
        }

        UpdateTransformedCoordinates();
    }

    public void SetThrottle(float throttle)
    {
        _throttle = Mathf.Clamp01(throttle);
    }

    public void SetFlowLimitPerSecond(float flowLimitPerSecond)
    {
        if (_maxWaterPerSecond <= Epsilon)
        {
            _throttle = 0f;
            return;
        }

        _throttle = Mathf.Clamp01(flowLimitPerSecond / _maxWaterPerSecond);
    }

    public void OnEnterFinishedState()
    {
        _isFinished = true;
        EnableComponent();
        LogDebugSnapshot("OnEnterFinishedState");
    }

    public void OnExitFinishedState()
    {
        _isFinished = false;
        ResetTransferStats();
        DisableComponent();
        LogDebugSnapshot("OnExitFinishedState");
    }

    private void UpdateTransformedCoordinates()
    {
        if (_blockObject == null)
        {
            _transformedIntakeCoordinates = ImmutableArray<Vector3Int>.Empty;
            _transformedOutputCoordinates = ImmutableArray<Vector3Int>.Empty;
            return;
        }

        _transformedIntakeCoordinates = _intakeCoordinates
            .Select(_blockObject.TransformCoordinates)
            .ToImmutableArray();
        _transformedOutputCoordinates = _outputCoordinates
            .Select(_blockObject.TransformCoordinates)
            .ToImmutableArray();
    }

    private List<IntakePortState> CollectIntakeStates()
    {
        List<IntakePortState> states = new(_transformedIntakeCoordinates.Length);
        foreach (Vector3Int intakeCoordinates in _transformedIntakeCoordinates)
        {
            if (!TryResolveIntakeCoordinates(intakeCoordinates, out Vector3Int coordinates))
            {
                continue;
            }

            float availableWater = GetAvailableWater(coordinates);
            if (availableWater <= Epsilon)
            {
                continue;
            }

            float contamination = Mathf.Clamp01(_threadSafeWaterMap.ColumnContamination(coordinates));
            float cleanAvailable = _moveCleanWater ? availableWater * (1f - contamination) : 0f;
            float contaminatedAvailable = _moveContaminatedWater ? availableWater * contamination : 0f;
            if (cleanAvailable <= Epsilon && contaminatedAvailable <= Epsilon)
            {
                continue;
            }

            states.Add(new IntakePortState(coordinates, cleanAvailable, contaminatedAvailable));
        }

        return states;
    }

    private bool TryResolveIntakeCoordinates(Vector3Int intakeCoordinates, out Vector3Int resolvedCoordinates)
    {
        if (_intakeMaxDepth == 0)
        {
            if (HasWaterColumn(intakeCoordinates) && _threadSafeWaterMap.CellIsUnderwater(intakeCoordinates))
            {
                resolvedCoordinates = intakeCoordinates;
                return true;
            }

            resolvedCoordinates = default;
            return false;
        }

        if (_blockObject == null)
        {
            resolvedCoordinates = default;
            return false;
        }

        Vector3Int startCoordinates = intakeCoordinates + new Vector3Int(0, 0, _blockObject.BaseZ + 1);
        int resolvedZ = GetIntakeZCoordinateLimitedByDepth(startCoordinates);
        int depth = startCoordinates.z - resolvedZ;
        if (depth <= 0)
        {
            resolvedCoordinates = default;
            return false;
        }

        Vector3Int candidate = new(startCoordinates.x, startCoordinates.y, resolvedZ);
        if (!HasWaterColumn(candidate) || !_threadSafeWaterMap.CellIsUnderwater(candidate))
        {
            resolvedCoordinates = default;
            return false;
        }

        resolvedCoordinates = candidate;
        return true;
    }

    private List<OutputPortState> CollectOutputStates()
    {
        List<OutputPortState> states = new(_transformedOutputCoordinates.Length);
        foreach (Vector3Int outputCoordinates in _transformedOutputCoordinates)
        {
            if (!TryResolveOutputCoordinates(outputCoordinates, out Vector3Int coordinates, out float availableSpace))
            {
                continue;
            }

            states.Add(new OutputPortState(coordinates, availableSpace));
        }

        return states;
    }

    private bool TryResolveOutputCoordinates(Vector3Int outputCoordinates, out Vector3Int resolvedCoordinates, out float availableSpace)
    {
        if (_outputMaxDrop == 0)
        {
            if (HasWaterColumn(outputCoordinates))
            {
                float exactSpace = GetAvailableSpace(outputCoordinates);
                if (exactSpace > Epsilon)
                {
                    resolvedCoordinates = outputCoordinates;
                    availableSpace = exactSpace;
                    return true;
                }
            }

            resolvedCoordinates = default;
            availableSpace = 0f;
            return false;
        }

        for (int offset = 0; offset <= _outputMaxDrop; offset++)
        {
            Vector3Int candidate = new(outputCoordinates.x, outputCoordinates.y, outputCoordinates.z - offset);
            if (!HasWaterColumn(candidate))
            {
                continue;
            }

            float candidateSpace = GetAvailableSpace(candidate);
            if (candidateSpace <= Epsilon)
            {
                continue;
            }

            resolvedCoordinates = candidate;
            availableSpace = candidateSpace;
            return true;
        }

        resolvedCoordinates = default;
        availableSpace = 0f;
        return false;
    }

    private int GetIntakeZCoordinateLimitedByDepth(Vector3Int startCoordinates)
    {
        for (int z = startCoordinates.z - 1; z >= startCoordinates.z - _intakeMaxDepth; z--)
        {
            Vector3Int candidate = new(startCoordinates.x, startCoordinates.y, z);
            if (IsTileOccupied(candidate))
            {
                return z + 1;
            }
        }

        return startCoordinates.z - _intakeMaxDepth;
    }

    private bool IsTileOccupied(Vector3Int coordinates)
    {
        if (_terrainService.Underground(coordinates))
        {
            return true;
        }

        _blockService.GetIntersectingObjectsAt(coordinates, InvalidOccupations, _blockObjectCache);
        bool hasOccupyingObject = HasOccupyingObject();
        _blockObjectCache.Clear();
        return hasOccupyingObject;
    }

    private bool HasOccupyingObject()
    {
        foreach (BlockObject blockObject in _blockObjectCache)
        {
            if (!blockObject.Overridable && blockObject != _blockObject)
            {
                return true;
            }
        }

        return false;
    }

    private bool HasWaterColumn(Vector3Int coordinates)
    {
        return _terrainService.Contains(new Vector2Int(coordinates.x, coordinates.y)) &&
               _threadSafeWaterMap.TryGetColumnFloor(coordinates, out _);
    }

    private float GetAvailableWater(Vector3Int coordinates)
    {
        return Mathf.Max(0f, _threadSafeWaterMap.WaterHeightOrFloor(coordinates) - coordinates.z);
    }

    private float GetAvailableSpace(Vector3Int coordinates)
    {
        return Mathf.Max(0f, coordinates.z - WaterUpperSafetySpace - _threadSafeWaterMap.WaterHeightOrFloor(coordinates));
    }

    private static float GetCleanRatio(float totalCleanAvailable, float totalContaminatedAvailable)
    {
        float totalAvailable = totalCleanAvailable + totalContaminatedAvailable;
        if (totalAvailable <= Epsilon)
        {
            return 0f;
        }

        return totalCleanAvailable / totalAvailable;
    }

    private void RemoveWaterFromInputs(IReadOnlyList<IntakePortState> intakeStates, float cleanTransfer, float contaminatedTransfer)
    {
        DistributeRemoval(
            intakeStates,
            cleanTransfer,
            static state => state.CleanAvailable,
            (coordinates, amount) => _waterService.RemoveCleanWater(coordinates, amount));
        DistributeRemoval(
            intakeStates,
            contaminatedTransfer,
            static state => state.ContaminatedAvailable,
            (coordinates, amount) => _waterService.RemoveContaminatedWater(coordinates, amount));
    }

    private void AddWaterToOutputs(IReadOnlyList<OutputPortState> outputStates, float cleanTransfer, float contaminatedTransfer, float totalTransfer)
    {
        float totalAvailableSpace = outputStates.Sum(static state => state.AvailableSpace);
        if (totalAvailableSpace <= Epsilon)
        {
            return;
        }

        float cleanRatio = totalTransfer > Epsilon ? cleanTransfer / totalTransfer : 0f;
        float remainingTransfer = totalTransfer;
        float remainingSpace = totalAvailableSpace;

        for (int index = 0; index < outputStates.Count; index++)
        {
            OutputPortState state = outputStates[index];
            float outputTransfer = index == outputStates.Count - 1
                ? remainingTransfer
                : Mathf.Min(remainingTransfer, totalTransfer * (state.AvailableSpace / totalAvailableSpace));

            outputTransfer = Mathf.Min(outputTransfer, remainingSpace);
            if (outputTransfer <= Epsilon)
            {
                continue;
            }

            float cleanOutput = outputTransfer * cleanRatio;
            float contaminatedOutput = outputTransfer - cleanOutput;
            if (cleanOutput > Epsilon)
            {
                _waterService.AddCleanWater(state.Coordinates, cleanOutput);
            }

            if (contaminatedOutput > Epsilon)
            {
                _waterService.AddContaminatedWater(state.Coordinates, contaminatedOutput);
            }

            remainingTransfer -= outputTransfer;
            remainingSpace -= state.AvailableSpace;
        }
    }

    private static void DistributeRemoval<TState>(
        IReadOnlyList<TState> states,
        float totalAmount,
        Func<TState, float> availableSelector,
        Action<Vector3Int, float> removalAction)
        where TState : IHydraulicPortState
    {
        if (totalAmount <= Epsilon)
        {
            return;
        }

        float remainingAmount = totalAmount;
        float remainingAvailable = states.Sum(availableSelector);
        for (int index = 0; index < states.Count; index++)
        {
            if (remainingAmount <= Epsilon || remainingAvailable <= Epsilon)
            {
                break;
            }

            TState state = states[index];
            float available = availableSelector(state);
            if (available <= Epsilon)
            {
                continue;
            }

            float removal = index == states.Count - 1
                ? remainingAmount
                : Mathf.Min(remainingAmount, totalAmount * (available / states.Sum(availableSelector)));

            removal = Mathf.Min(removal, available);
            if (removal <= Epsilon)
            {
                remainingAvailable -= available;
                continue;
            }

            removalAction(state.Coordinates, removal);
            remainingAmount -= removal;
            remainingAvailable -= available;
        }
    }

    private void ResetTransferStats()
    {
        CurrentTransferPerTick = 0f;
        CurrentTransferPerSecond = 0f;
        CurrentCleanTransferPerSecond = 0f;
        CurrentContaminatedTransferPerSecond = 0f;
    }

    private void MaybeLogTick(string state)
    {
        if (state == _lastDebugState && _ticksSinceLastDebugLog < DebugLogIntervalTicks)
        {
            return;
        }

        LogDebugSnapshot($"Tick:{state}");
    }

    private void LogDebugSnapshot(string trigger)
    {
        _lastDebugState = trigger.StartsWith("Tick:", StringComparison.Ordinal) ? trigger["Tick:".Length..] : trigger;
        _ticksSinceLastDebugLog = 0;

        string blockSummary = _blockObject == null
            ? "block=null"
            : $"coords={FormatCoordinates(_blockObject.Coordinates)} baseCoords={FormatCoordinates(_blockObject.CoordinatesAtBaseZ)} baseZ={_blockObject.BaseZ}";

        string componentLabel = Transform != null ? Transform.GetInstanceID().ToString() : "no-transform";

        Debug.Log(
            $"[FulgurFangs][HydraulicTransfer] instance={componentLabel} trigger={trigger} tick={_tickCounter} finished={_isFinished} " +
            $"throttle={_throttle:F2} limit={FlowLimitPerSecond:F3} max={_maxWaterPerSecond:F3} current={CurrentTransferPerSecond:F3} " +
            $"{blockSummary} intakes={DescribeIntakePorts()} outputs={DescribeOutputPorts()}");
    }

    private string DescribeIntakePorts()
    {
        if (_transformedIntakeCoordinates.IsDefaultOrEmpty)
        {
            return "[]";
        }

        List<string> descriptions = new(_transformedIntakeCoordinates.Length);
        foreach (Vector3Int transformedCoordinates in _transformedIntakeCoordinates)
        {
            string description = $"src={FormatCoordinates(transformedCoordinates)}";
            if (_blockObject == null)
            {
                descriptions.Add(description + " resolved=none reason=no-block");
                continue;
            }

            Vector3Int startCoordinates = transformedCoordinates + new Vector3Int(0, 0, _blockObject.BaseZ + 1);
            int resolvedZ = GetIntakeZCoordinateLimitedByDepth(startCoordinates);
            Vector3Int resolvedCoordinates = new(startCoordinates.x, startCoordinates.y, resolvedZ);
            int depth = startCoordinates.z - resolvedZ;
            bool hasColumn = HasWaterColumn(resolvedCoordinates);
            bool underwater = hasColumn && _threadSafeWaterMap.CellIsUnderwater(resolvedCoordinates);
            float waterHeight = hasColumn ? _threadSafeWaterMap.WaterHeightOrFloor(resolvedCoordinates) : -1f;
            float available = hasColumn ? GetAvailableWater(resolvedCoordinates) : 0f;

            descriptions.Add(
                $"{description} start={FormatCoordinates(startCoordinates)} resolved={FormatCoordinates(resolvedCoordinates)} depth={depth} " +
                $"hasColumn={hasColumn} underwater={underwater} waterHeight={waterHeight:F3} available={available:F3}");
        }

        return "[" + string.Join("; ", descriptions) + "]";
    }

    private string DescribeOutputPorts()
    {
        if (_transformedOutputCoordinates.IsDefaultOrEmpty)
        {
            return "[]";
        }

        List<string> descriptions = new(_transformedOutputCoordinates.Length);
        foreach (Vector3Int transformedCoordinates in _transformedOutputCoordinates)
        {
            List<string> candidates = new(_outputMaxDrop + 1);
            for (int offset = 0; offset <= _outputMaxDrop; offset++)
            {
                Vector3Int candidate = new(transformedCoordinates.x, transformedCoordinates.y, transformedCoordinates.z - offset);
                bool hasColumn = HasWaterColumn(candidate);
                float availableSpace = hasColumn ? GetAvailableSpace(candidate) : 0f;
                candidates.Add($"{FormatCoordinates(candidate)} col={hasColumn} space={availableSpace:F3}");
            }

            descriptions.Add($"src={FormatCoordinates(transformedCoordinates)} candidates=({string.Join(", ", candidates)})");
        }

        return "[" + string.Join("; ", descriptions) + "]";
    }

    private static string FormatCoordinates(Vector3Int coordinates)
    {
        return $"({coordinates.x},{coordinates.y},{coordinates.z})";
    }

    private interface IHydraulicPortState
    {
        Vector3Int Coordinates { get; }
    }

    private readonly record struct IntakePortState(
        Vector3Int Coordinates,
        float CleanAvailable,
        float ContaminatedAvailable) : IHydraulicPortState;

    private readonly record struct OutputPortState(
        Vector3Int Coordinates,
        float AvailableSpace) : IHydraulicPortState;
}
