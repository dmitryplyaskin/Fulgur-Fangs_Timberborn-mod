using Timberborn.BaseComponentSystem;
using Timberborn.EntitySystem;
using UnityEngine;

namespace FulgurFangs.Code.Electricity;

public sealed class ElectricityConsumerComponent : BaseComponent, IPostInitializableEntity, IDeletableEntity
{
    private readonly ElectricityService _electricityService;
    private int _demand;

    public ElectricityConsumerComponent(ElectricityService electricityService)
    {
        _electricityService = electricityService;
    }

    public bool IsReady => Enabled;

    public int Demand => _demand;

    public bool Powered { get; private set; }

    public Vector3 WorldPosition => Transform.position;

    public void PostInitializeEntity()
    {
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
        Powered = powered;
    }
}
