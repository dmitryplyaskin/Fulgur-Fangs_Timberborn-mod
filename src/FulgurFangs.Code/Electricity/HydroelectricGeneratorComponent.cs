using Timberborn.BaseComponentSystem;
using Timberborn.BlockSystem;
using Timberborn.EntitySystem;
using Timberborn.WaterSystem;
using UnityEngine;
using FulgurFangs.Code.Hydraulics;

namespace FulgurFangs.Code.Electricity;

public sealed class HydroelectricGeneratorComponent : BaseComponent, IPostInitializableEntity, IDeletableEntity, IFinishedStateListener
{
    private readonly ElectricityService _electricityService;
    private readonly IThreadSafeWaterMap _threadSafeWaterMap;
    private BlockObject? _blockObject;
    private bool _isFinished;
    private HydraulicTransferComponent? _hydraulicTransferComponent;
    private MultiCellValveComponent? _multiCellValveComponent;
    private ValveSectionArrayComponent? _valveSectionArrayComponent;
    private int _maxOutput;
    private float _powerPerFlowUnit;

    public HydroelectricGeneratorComponent(
        ElectricityService electricityService,
        IThreadSafeWaterMap threadSafeWaterMap)
    {
        _electricityService = electricityService;
        _threadSafeWaterMap = threadSafeWaterMap;
    }

    public bool IsReady => Enabled && _isFinished;

    public int InstanceId => Transform != null ? Transform.GetInstanceID() : 0;

    public Vector3 WorldPosition => GetWorldPosition();

    public int MaxOutput => _maxOutput;

    public float CurrentFlow
    {
        get
        {
            if (_multiCellValveComponent != null)
            {
                return Mathf.Max(0f, _multiCellValveComponent.CurrentFlow);
            }

            if (_valveSectionArrayComponent != null)
            {
                return Mathf.Max(0f, _valveSectionArrayComponent.CurrentFlow);
            }

            if (_hydraulicTransferComponent != null)
            {
                return Mathf.Max(0f, _hydraulicTransferComponent.CurrentTransferPerSecond);
            }

            if (!IsReady || _blockObject == null)
            {
                return 0f;
            }

            Vector3Int coordinates = _blockObject.Coordinates;
            if (!_threadSafeWaterMap.CellIsUnderwater(coordinates))
            {
                return 0f;
            }

            return _threadSafeWaterMap.WaterFlowDirection(coordinates).magnitude;
        }
    }

    public float CurrentOutput => Mathf.Min(Mathf.Max(0f, CurrentFlow * _powerPerFlowUnit), _maxOutput);

    public void PostInitializeEntity()
    {
        _blockObject = GetComponent<BlockObject>() ?? Transform.GetComponentInParent<BlockObject>();
        _isFinished = _blockObject != null && _blockObject.IsFinished;
        _hydraulicTransferComponent = GetComponent<HydraulicTransferComponent>();
        _multiCellValveComponent = GetComponent<MultiCellValveComponent>();
        _valveSectionArrayComponent = GetComponent<ValveSectionArrayComponent>();
        _electricityService.RegisterHydroelectricGenerator(this);
    }

    public void DeleteEntity()
    {
        _electricityService.UnregisterHydroelectricGenerator(this);
    }

    public void SetGenerationParameters(int maxOutput, float powerPerFlowUnit)
    {
        _maxOutput = maxOutput;
        _powerPerFlowUnit = Mathf.Max(0f, powerPerFlowUnit);
    }

    public void OnEnterFinishedState()
    {
        _isFinished = true;
        _electricityService.RefreshStateWithoutAdvancingTime();
    }

    public void OnExitFinishedState()
    {
        _isFinished = false;
        _electricityService.RefreshStateWithoutAdvancingTime();
    }

    private Vector3 GetWorldPosition()
    {
        if (!ReferenceEquals(_blockObject, null))
        {
            Vector3Int coordinates = _blockObject.CoordinatesAtBaseZ;
            return new Vector3(coordinates.x + 0.5f, coordinates.z, coordinates.y + 0.5f);
        }

        return Transform != null ? Transform.position : Vector3.zero;
    }
}
