using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Timberborn.BaseComponentSystem;
using Timberborn.BlockSystem;
using Timberborn.Coordinates;
using Timberborn.EntitySystem;
using Timberborn.Persistence;
using Timberborn.TickSystem;
using Timberborn.WorldPersistence;
using Timberborn.WaterSystem;
using UnityEngine;

namespace FulgurFangs.Code.Hydraulics;

public sealed class MultiCellValveComponent : TickableComponent, IAwakableComponent, IInitializableEntity, IPostPlacementChangeListener, IFinishedStateListener, IPersistentEntity
{
    private const float Epsilon = 0.0001f;
    private const int DebugLogIntervalTicks = 60;
    private static readonly ComponentKey SaveKey = new("FulgurFangs.MultiCellValve");
    private static readonly PropertyKey<float> OutflowLimitKey = new("OutflowLimit");

    private readonly IWaterService _waterService;
    private readonly IThreadSafeWaterMap _threadSafeWaterMap;
    private BlockObject? _blockObject;
    private bool _isFinished;
    private bool _loadedFromSave;
    private bool _fullObstacleApplied;
    private ImmutableArray<Vector3Int> _flowCoordinates = ImmutableArray<Vector3Int>.Empty;
    private ImmutableArray<Vector3Int> _transformedFlowCoordinates = ImmutableArray<Vector3Int>.Empty;
    private float _maxOutflowLimit = 2f;
    private float _outflowLimitStep = 0.01f;
    private float _outflowLimit = 2f;
    private int _tickCounter;

    public MultiCellValveComponent(IWaterService waterService, IThreadSafeWaterMap threadSafeWaterMap)
    {
        _waterService = waterService;
        _threadSafeWaterMap = threadSafeWaterMap;
    }

    public float MaxOutflowLimit => _maxOutflowLimit;

    public float OutflowLimitStep => _outflowLimitStep;

    public float OutflowLimit => _outflowLimit;

    public ImmutableArray<Vector3Int> FlowCoordinates => _transformedFlowCoordinates;

    public float CurrentFlow
    {
        get
        {
            if (!_isFinished || _transformedFlowCoordinates.IsDefaultOrEmpty)
            {
                return 0f;
            }

            float flow = 0f;
            foreach (Vector3Int coordinates in _transformedFlowCoordinates)
            {
                if (!_threadSafeWaterMap.CellIsUnderwater(coordinates))
                {
                    continue;
                }

                flow += _threadSafeWaterMap.WaterFlowDirection(coordinates).magnitude;
            }

            return flow;
        }
    }

    public void Awake()
    {
        _blockObject = GetComponent<BlockObject>() ?? Transform.GetComponentInParent<BlockObject>();
        _isFinished = _blockObject != null && _blockObject.IsFinished;
    }

    public void InitializeEntity()
    {
        UpdateTransformedCoordinates();
        if (!_loadedFromSave)
        {
            _outflowLimit = Mathf.Clamp(_outflowLimit, 0f, _maxOutflowLimit);
        }

        if (_isFinished)
        {
            ApplyValveState();
        }

        LogDebugSnapshot("InitializeEntity");
    }

    public void Save(IEntitySaver entitySaver)
    {
        entitySaver.GetComponent(SaveKey).Set(OutflowLimitKey, _outflowLimit);
    }

    public void Load(IEntityLoader entityLoader)
    {
        if (entityLoader.TryGetComponent(SaveKey, out IObjectLoader componentLoader))
        {
            _loadedFromSave = true;
            _outflowLimit = componentLoader.Get(OutflowLimitKey);
        }
    }

    public void SetParameters(MultiCellValveSpec spec)
    {
        _flowCoordinates = spec.FlowCoordinates;
        _maxOutflowLimit = Mathf.Max(Epsilon, spec.MaxOutflowLimit);
        _outflowLimitStep = Mathf.Max(0.001f, spec.OutflowLimitStep);
        if (!_loadedFromSave)
        {
            _outflowLimit = Mathf.Clamp(spec.DefaultOutflowLimit, 0f, _maxOutflowLimit);
        }
        else
        {
            _outflowLimit = Mathf.Clamp(_outflowLimit, 0f, _maxOutflowLimit);
        }

        ImmutableArray<Vector3Int> previousCoordinates = _transformedFlowCoordinates;
        UpdateTransformedCoordinates();
        if (_isFinished)
        {
            RemoveWaterControls(previousCoordinates);
            ApplyValveState();
        }

        LogDebugSnapshot("SetParameters");
    }

    public void SetOutflowLimit(float outflowLimit)
    {
        _outflowLimit = Mathf.Clamp(outflowLimit, 0f, _maxOutflowLimit);
        if (_isFinished)
        {
            ApplyValveState();
        }

        LogDebugSnapshot("SetOutflowLimit");
    }

    public void OnPostPlacementChanged()
    {
        ImmutableArray<Vector3Int> previousCoordinates = _transformedFlowCoordinates;
        if (_isFinished)
        {
            RemoveWaterControls(previousCoordinates);
        }

        UpdateTransformedCoordinates();
        if (_isFinished)
        {
            ApplyValveState();
        }

        LogDebugSnapshot("OnPostPlacementChanged");
    }

    public void OnEnterFinishedState()
    {
        _isFinished = true;
        ApplyValveState();
        LogDebugSnapshot("OnEnterFinishedState");
    }

    public void OnExitFinishedState()
    {
        _isFinished = false;
        RemoveWaterControls(_transformedFlowCoordinates);
        LogDebugSnapshot("OnExitFinishedState");
    }

    public override void Tick()
    {
        if (!ShouldLogDebugSnapshots())
        {
            return;
        }

        _tickCounter++;
        if (_tickCounter % DebugLogIntervalTicks != 0)
        {
            return;
        }

        LogDebugSnapshot("Tick");
    }

    private void UpdateTransformedCoordinates()
    {
        if (_blockObject == null)
        {
            _transformedFlowCoordinates = ImmutableArray<Vector3Int>.Empty;
            return;
        }

        _transformedFlowCoordinates = _flowCoordinates
            .Select(_blockObject.TransformCoordinates)
            .ToImmutableArray();
    }

    private void ApplyValveState()
    {
        if (_transformedFlowCoordinates.IsDefaultOrEmpty || _blockObject == null)
        {
            return;
        }

        FlowDirection flowDirection = _blockObject.Orientation.ToFlowDirection();
        foreach (Vector3Int coordinates in _transformedFlowCoordinates)
        {
            _waterService.AddDirectionLimiter(coordinates, flowDirection);
            ApplyOutflowLimit(coordinates);
        }

        UpdateClosedObstacles();
    }

    private void ApplyOutflowLimit(Vector3Int coordinates)
    {
        if (_outflowLimit >= _maxOutflowLimit - Epsilon)
        {
            _waterService.RemoveOutflowLimit(coordinates);
        }
        else
        {
            _waterService.SetOutflowLimit(coordinates, _outflowLimit);
        }
    }

    private void UpdateClosedObstacles()
    {
        bool shouldApplyFullObstacle = _outflowLimit <= Epsilon;
        if (shouldApplyFullObstacle == _fullObstacleApplied)
        {
            return;
        }

        foreach (Vector3Int coordinates in _transformedFlowCoordinates)
        {
            if (shouldApplyFullObstacle)
            {
                _waterService.AddFullObstacle(coordinates);
            }
            else
            {
                _waterService.RemoveFullObstacle(coordinates);
            }
        }

        _fullObstacleApplied = shouldApplyFullObstacle;
    }

    private void RemoveWaterControls(ImmutableArray<Vector3Int> coordinates)
    {
        if (coordinates.IsDefaultOrEmpty)
        {
            _fullObstacleApplied = false;
            return;
        }

        foreach (Vector3Int flowCoordinates in coordinates)
        {
            _waterService.RemoveOutflowLimit(flowCoordinates);
            _waterService.RemoveDirectionLimiter(flowCoordinates);
            if (_fullObstacleApplied)
            {
                _waterService.RemoveFullObstacle(flowCoordinates);
            }
        }

        _fullObstacleApplied = false;
    }

    private bool ShouldLogDebugSnapshots()
    {
        return Transform != null &&
               (Transform.name.Contains("ThrottlingHydroPlant", System.StringComparison.OrdinalIgnoreCase) ||
                Transform.name.Contains("UpperFlowHydroPlant", System.StringComparison.OrdinalIgnoreCase));
    }

    private void LogDebugSnapshot(string trigger)
    {
        if (!ShouldLogDebugSnapshots())
        {
            return;
        }

        StringBuilder builder = new();
        builder.Append("[FulgurFangs][MultiCellValve] ");
        builder.Append("trigger=").Append(trigger);
        builder.Append(" tick=").Append(_tickCounter);
        builder.Append(" finished=").Append(_isFinished);
        builder.Append(" outflow=").Append(_outflowLimit.ToString("F3"));
        builder.Append('/').Append(_maxOutflowLimit.ToString("F3"));
        builder.Append(" step=").Append(_outflowLimitStep.ToString("F3"));
        builder.Append(" currentFlow=").Append(CurrentFlow.ToString("F3"));
        builder.Append(" fullObstacle=").Append(_fullObstacleApplied);

        if (_blockObject == null)
        {
            builder.Append(" block=null");
            Debug.Log(builder.ToString());
            return;
        }

        builder.Append(" coords=").Append(FormatCoordinates(_blockObject.Coordinates));
        builder.Append(" baseCoords=").Append(FormatCoordinates(_blockObject.CoordinatesAtBaseZ));
        builder.Append(" baseZ=").Append(_blockObject.BaseZ);
        builder.Append(" orientation=").Append(_blockObject.Orientation);
        builder.Append(" size=").Append(FormatCoordinates(_blockObject.Blocks.Size));
        builder.Append(" occupied=").Append(FormatCoordinatesList(_blockObject.PositionedBlocks.GetOccupiedCoordinates()));
        builder.Append(" foundation=").Append(FormatCoordinatesList(_blockObject.PositionedBlocks.GetFoundationCoordinates()));
        builder.Append(" allBlocks=").Append(FormatCoordinatesList(_blockObject.PositionedBlocks.GetAllCoordinates()));
        builder.Append(" localFlow=").Append(FormatCoordinatesList(_flowCoordinates));
        builder.Append(" worldFlow=").Append(FormatCoordinatesList(_transformedFlowCoordinates));
        builder.Append(" cells=").Append(DescribeLocalCells());
        builder.Append(" waterSlices=").Append(DescribeWaterSlices());

        Debug.Log(builder.ToString());
    }

    private string DescribeLocalCells()
    {
        if (_blockObject == null)
        {
            return "[]";
        }

        Vector3Int size = _blockObject.Blocks.Size;
        StringBuilder builder = new("[");
        bool first = true;
        for (int z = 0; z < size.z; z++)
        {
            for (int y = 0; y < size.y; y++)
            {
                for (int x = 0; x < size.x; x++)
                {
                    Vector3Int local = new(x, y, z);
                    Vector3Int world = _blockObject.TransformCoordinates(local);
                    bool isFlow = _flowCoordinates.Contains(local);
                    bool isOccupied = _blockObject.PositionedBlocks.HasBlockAt(world);
                    bool hasColumn = _threadSafeWaterMap.TryGetColumnFloor(world, out int floor);
                    bool underwater = hasColumn && _threadSafeWaterMap.CellIsUnderwater(world);
                    float waterHeight = hasColumn ? _threadSafeWaterMap.WaterHeightOrFloor(world) : -1f;
                    Vector3 flowDirection = hasColumn ? _threadSafeWaterMap.WaterFlowDirection(world) : Vector3.zero;

                    if (!first)
                    {
                        builder.Append("; ");
                    }

                    builder.Append("local=").Append(FormatCoordinates(local));
                    builder.Append("->world=").Append(FormatCoordinates(world));
                    builder.Append(" flow=").Append(isFlow);
                    builder.Append(" block=").Append(isOccupied);
                    builder.Append(" col=").Append(hasColumn);
                    builder.Append(" floor=").Append(floor);
                    builder.Append(" underwater=").Append(underwater);
                    builder.Append(" waterHeight=").Append(waterHeight.ToString("F3"));
                    builder.Append(" waterFlow=").Append(FormatVector(flowDirection));
                    first = false;
                }
            }
        }

        builder.Append(']');
        return builder.ToString();
    }

    private string DescribeWaterSlices()
    {
        if (_blockObject == null)
        {
            return "[]";
        }

        Vector3Int baseCoordinates = _blockObject.CoordinatesAtBaseZ;
        Vector3Int size = _blockObject.Blocks.Size;
        int minX = baseCoordinates.x - 1;
        int maxX = baseCoordinates.x + size.x;
        int minY = baseCoordinates.y - 1;
        int maxY = baseCoordinates.y + size.y;
        int minZ = Mathf.Max(0, baseCoordinates.z - 1);
        int maxZ = baseCoordinates.z + size.z;

        StringBuilder builder = new("[");
        bool firstCell = true;
        for (int z = minZ; z <= maxZ; z++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    Vector3Int coordinates = new(x, y, z);
                    bool hasColumn = _threadSafeWaterMap.TryGetColumnFloor(coordinates, out int floor);
                    bool underwater = hasColumn && _threadSafeWaterMap.CellIsUnderwater(coordinates);
                    float waterHeight = hasColumn ? _threadSafeWaterMap.WaterHeightOrFloor(coordinates) : -1f;
                    Vector3 flowDirection = hasColumn ? _threadSafeWaterMap.WaterFlowDirection(coordinates) : Vector3.zero;
                    bool isFlow = _transformedFlowCoordinates.Contains(coordinates);
                    bool hasBlock = _blockObject.PositionedBlocks.HasBlockAt(coordinates);

                    if (!firstCell)
                    {
                        builder.Append("; ");
                    }

                    builder.Append("cell=").Append(FormatCoordinates(coordinates));
                    builder.Append(" flowCell=").Append(isFlow);
                    builder.Append(" block=").Append(hasBlock);
                    builder.Append(" col=").Append(hasColumn);
                    builder.Append(" floor=").Append(floor);
                    builder.Append(" underwater=").Append(underwater);
                    builder.Append(" waterHeight=").Append(waterHeight.ToString("F3"));
                    builder.Append(" waterFlow=").Append(FormatVector(flowDirection));
                    firstCell = false;
                }
            }
        }

        builder.Append(']');
        return builder.ToString();
    }

    private static string FormatCoordinatesList(System.Collections.Generic.IEnumerable<Vector3Int> coordinates)
    {
        return "[" + string.Join(", ", coordinates.Select(FormatCoordinates)) + "]";
    }

    private static string FormatVector(Vector3 vector)
    {
        return $"({vector.x:F3},{vector.y:F3},{vector.z:F3})|m={vector.magnitude:F3}";
    }

    private static string FormatCoordinates(Vector3Int coordinates)
    {
        return $"({coordinates.x},{coordinates.y},{coordinates.z})";
    }
}
