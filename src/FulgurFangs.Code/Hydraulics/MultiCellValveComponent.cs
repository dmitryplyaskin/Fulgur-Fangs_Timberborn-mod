using System.Collections.Immutable;
using System.Linq;
using Timberborn.BaseComponentSystem;
using Timberborn.BlockSystem;
using Timberborn.Coordinates;
using Timberborn.EntitySystem;
using Timberborn.Persistence;
using Timberborn.WorldPersistence;
using Timberborn.WaterSystem;
using UnityEngine;

namespace FulgurFangs.Code.Hydraulics;

public sealed class MultiCellValveComponent : BaseComponent, IAwakableComponent, IInitializableEntity, IPostPlacementChangeListener, IFinishedStateListener, IPersistentEntity
{
    private const float Epsilon = 0.0001f;
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
    }

    public void SetOutflowLimit(float outflowLimit)
    {
        _outflowLimit = Mathf.Clamp(outflowLimit, 0f, _maxOutflowLimit);
        if (_isFinished)
        {
            ApplyValveState();
        }
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
    }

    public void OnEnterFinishedState()
    {
        _isFinished = true;
        ApplyValveState();
    }

    public void OnExitFinishedState()
    {
        _isFinished = false;
        RemoveWaterControls(_transformedFlowCoordinates);
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
}
