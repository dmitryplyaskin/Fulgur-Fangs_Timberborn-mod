using Timberborn.BaseComponentSystem;
using Timberborn.BlockSystem;
using Timberborn.DwellingSystem;
using Timberborn.Effects;
using Timberborn.EntitySystem;
using Timberborn.NeedSystem;
using Timberborn.TickSystem;
using Timberborn.TimeSystem;
using UnityEngine;

namespace FulgurFangs.Code.Electricity;

public sealed class PoweredDwellingNeedComponent : TickableComponent, IPostInitializableEntity, IFinishedStateListener
{
    private readonly IDayNightCycle _dayNightCycle;
    private BlockObject? _blockObject;
    private Dwelling? _dwelling;
    private ElectricityConsumerComponent? _electricityConsumer;
    private bool _isFinished;
    private string _needId = "";
    private float _pointsPerHour;

    public PoweredDwellingNeedComponent(IDayNightCycle dayNightCycle)
    {
        _dayNightCycle = dayNightCycle;
    }

    public void PostInitializeEntity()
    {
        _blockObject = GetComponent<BlockObject>() ?? Transform.GetComponentInParent<BlockObject>();
        _dwelling = GetComponent<Dwelling>() ?? Transform.GetComponentInParent<Dwelling>();
        _electricityConsumer = GetComponent<ElectricityConsumerComponent>() ?? Transform.GetComponentInParent<ElectricityConsumerComponent>();
        _isFinished = _blockObject != null && _blockObject.IsFinished;
    }

    public void SetParameters(string needId, float pointsPerHour)
    {
        _needId = needId ?? "";
        _pointsPerHour = Mathf.Max(0f, pointsPerHour);
    }

    public override void Tick()
    {
        if (!_isFinished ||
            !Enabled ||
            string.IsNullOrWhiteSpace(_needId) ||
            _pointsPerHour <= 0f ||
            _dwelling == null ||
            _electricityConsumer == null ||
            _electricityConsumer.SupplyFraction <= 0f)
        {
            return;
        }

        float deltaHours = Mathf.Max(0f, _dayNightCycle.FixedDeltaTimeInHours) * _electricityConsumer.SupplyFraction;
        if (deltaHours <= 0f)
        {
            return;
        }

        ContinuousEffect effect = new(_needId, _pointsPerHour);
        foreach (Dweller dweller in _dwelling.AdultDwellers)
        {
            ApplyEffect(dweller, in effect, deltaHours);
        }

        foreach (Dweller dweller in _dwelling.ChildDwellers)
        {
            ApplyEffect(dweller, in effect, deltaHours);
        }
    }

    public void OnEnterFinishedState()
    {
        _isFinished = true;
    }

    public void OnExitFinishedState()
    {
        _isFinished = false;
    }

    private static void ApplyEffect(Dweller dweller, in ContinuousEffect effect, float deltaHours)
    {
        NeedManager? needManager = dweller.GetComponent<NeedManager>() ?? dweller.Transform.GetComponentInParent<NeedManager>();
        needManager?.ApplyEffect(in effect, deltaHours);
    }
}
