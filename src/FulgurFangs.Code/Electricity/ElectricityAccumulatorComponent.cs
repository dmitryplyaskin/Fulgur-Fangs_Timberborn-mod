using Timberborn.BaseComponentSystem;
using Timberborn.EntityPanelSystem;
using Timberborn.EntitySystem;
using Timberborn.Localization;
using Timberborn.Persistence;
using Timberborn.UIFormatters;
using Timberborn.WorldPersistence;
using UnityEngine;

namespace FulgurFangs.Code.Electricity;

public sealed class ElectricityAccumulatorComponent : BaseComponent, IPostInitializableEntity, IDeletableEntity, IPersistentEntity, IEntityDescriber
{
    private static readonly ComponentKey SaveKey = new("FulgurFangs.ElectricityAccumulator");
    private static readonly PropertyKey<float> ChargeKey = new("Charge");

    private readonly ElectricityService _electricityService;
    private readonly ILoc _loc;
    private readonly DescribedAmountFactory _describedAmountFactory;
    private float _capacity;
    private float _currentCharge;
    private float _leakagePerHour;
    private float _maxDischargePerHour;
    private bool _loadedFromSave;

    public ElectricityAccumulatorComponent(
        ElectricityService electricityService,
        ILoc loc,
        DescribedAmountFactory describedAmountFactory)
    {
        _electricityService = electricityService;
        _loc = loc;
        _describedAmountFactory = describedAmountFactory;
    }

    public bool IsReady => Enabled;

    public float Capacity => _capacity;

    public float CurrentCharge => _currentCharge;

    public float MaxDischargePerHour => _maxDischargePerHour;

    public int InstanceId => Transform.GetInstanceID();

    public Vector3 WorldPosition => Transform.position;

    public void PostInitializeEntity()
    {
        _electricityService.RegisterAccumulator(this);
    }

    public void DeleteEntity()
    {
        _electricityService.UnregisterAccumulator(this);
    }

    public void Save(IEntitySaver entitySaver)
    {
        entitySaver.GetComponent(SaveKey).Set(ChargeKey, _currentCharge);
    }

    public void Load(IEntityLoader entityLoader)
    {
        if (entityLoader.TryGetComponent(SaveKey, out IObjectLoader componentLoader))
        {
            _loadedFromSave = true;
            _currentCharge = Mathf.Clamp(componentLoader.Get(ChargeKey), 0f, _capacity);
        }
    }

    public void SetParameters(float capacity, float leakagePerHour, float maxDischargePerHour)
    {
        _capacity = Mathf.Max(0f, capacity);
        _leakagePerHour = Mathf.Max(0f, leakagePerHour);
        _maxDischargePerHour = Mathf.Max(0f, maxDischargePerHour);
        _currentCharge = _loadedFromSave
            ? Mathf.Clamp(_currentCharge, 0f, _capacity)
            : 0f;
    }

    public void ApplyLeakage(float deltaHours)
    {
        if (_currentCharge <= 0f || _leakagePerHour <= 0f || deltaHours <= 0f)
        {
            return;
        }

        _currentCharge = Mathf.Max(0f, _currentCharge - _leakagePerHour * deltaHours);
    }

    public float GetAvailableDischargePower(float deltaHours)
    {
        if (deltaHours <= 0f)
        {
            return 0f;
        }

        return Mathf.Min(_maxDischargePerHour, _currentCharge / deltaHours);
    }

    public float DischargePower(float requestedPower, float deltaHours)
    {
        if (deltaHours <= 0f)
        {
            return 0f;
        }

        float power = Mathf.Min(Mathf.Max(0f, requestedPower), GetAvailableDischargePower(deltaHours));
        _currentCharge = Mathf.Max(0f, _currentCharge - power * deltaHours);
        return power;
    }

    public float GetAvailableChargePower(float deltaHours)
    {
        if (deltaHours <= 0f)
        {
            return 0f;
        }

        return Mathf.Min(_maxDischargePerHour, (_capacity - _currentCharge) / deltaHours);
    }

    public float ChargePower(float availablePower, float deltaHours)
    {
        if (deltaHours <= 0f)
        {
            return 0f;
        }

        float acceptedPower = Mathf.Min(Mathf.Max(0f, availablePower), GetAvailableChargePower(deltaHours));
        _currentCharge = Mathf.Min(_capacity, _currentCharge + acceptedPower * deltaHours);
        return acceptedPower;
    }

    public IEnumerable<EntityDescription> DescribeEntity()
    {
        ElectricitySubnetworkSnapshot snapshot = _electricityService.GetAccumulatorSnapshot(this) ?? default;
        return ElectricityEntityDescriptions.CreateAccumulatorDescriptions(
            _loc,
            _describedAmountFactory,
            snapshot,
            Mathf.RoundToInt(_currentCharge),
            Mathf.RoundToInt(_capacity),
            40);
    }
}
