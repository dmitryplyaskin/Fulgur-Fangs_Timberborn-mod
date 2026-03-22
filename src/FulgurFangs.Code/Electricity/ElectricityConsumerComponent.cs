using Timberborn.BaseComponentSystem;
using Timberborn.BlockSystem;
using Timberborn.EntitySystem;
using UnityEngine;

namespace FulgurFangs.Code.Electricity;

public sealed class ElectricityConsumerComponent : BaseComponent, IPostInitializableEntity, IDeletableEntity, IFinishedStateListener
{
    private readonly ElectricityService _electricityService;
    private BlockObject? _blockObject;
    private bool _isFinished;
    private int _demand;

    public ElectricityConsumerComponent(ElectricityService electricityService)
    {
        _electricityService = electricityService;
    }

    public bool IsReady => Enabled && _isFinished;

    public int Demand => _demand;

    public bool Powered => SupplyFraction > 0f;

    public float SupplyFraction { get; private set; }

    public int InstanceId => Transform.GetInstanceID();

    public Vector3 WorldPosition => Transform.position;

    public void PostInitializeEntity()
    {
        _blockObject = GetComponent<BlockObject>() ?? Transform.GetComponentInParent<BlockObject>();
        _isFinished = _blockObject != null && _blockObject.IsFinished;
        _electricityService.RegisterConsumer(this);
    }

    public void DeleteEntity()
    {
        _electricityService.UnregisterConsumer(this);
    }

    public void SetDemand(int demand)
    {
        _demand = demand;
    }

    public void SetPowered(bool powered)
    {
        SetSupplyFraction(powered ? 1f : 0f);
    }

    public void SetSupplyFraction(float supplyFraction)
    {
        SupplyFraction = Mathf.Clamp01(supplyFraction);
    }

    public void OnEnterFinishedState()
    {
        _isFinished = true;
    }

    public void OnExitFinishedState()
    {
        _isFinished = false;
        SetSupplyFraction(0f);
    }
}
