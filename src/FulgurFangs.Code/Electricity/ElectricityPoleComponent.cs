using System.Collections.Generic;
using Timberborn.BaseComponentSystem;
using Timberborn.EntitySystem;
using Timberborn.ZiplineSystem;
using UnityEngine;

namespace FulgurFangs.Code.Electricity;

public sealed class ElectricityPoleComponent : BaseComponent, IPostInitializableEntity, IDeletableEntity
{
    private readonly ElectricityService _electricityService;
    private readonly ZiplineTower _tower;
    private int _range;

    public ElectricityPoleComponent(ElectricityService electricityService, ZiplineTower tower)
    {
        _electricityService = electricityService;
        _tower = tower;
    }

    public bool IsReady => Enabled;

    public ZiplineTower Tower => _tower;

    public Vector3 WorldPosition => Transform.position;

    public void PostInitializeEntity()
    {
        _electricityService.RegisterPole(this);
        Debug.Log($"[FulgurFangs] Pole registered at {WorldPosition} range={_range}");
    }

    public void DeleteEntity()
    {
        _electricityService.UnregisterPole(this);
    }

    public void SetRange(int range)
    {
        _range = range;
    }

    public bool InRangeOf(Vector3 position)
    {
        return Mathf.Abs(position.x - WorldPosition.x) <= _range &&
               Mathf.Abs(position.z - WorldPosition.z) <= _range;
    }

    public IEnumerable<ZiplineTower> GetConnectionTargetsSafe()
    {
        if (ReferenceEquals(_tower, null) || !_tower.Enabled || !_tower.IsActive)
        {
            yield break;
        }

        IEnumerator<ZiplineTower>? enumerator = null;
        try
        {
            enumerator = _tower.ConnectionTargets.GetEnumerator();
        }
        catch (NullReferenceException)
        {
            yield break;
        }

        if (enumerator == null)
        {
            yield break;
        }

        while (true)
        {
            bool movedNext;
            try
            {
                movedNext = enumerator.MoveNext();
            }
            catch (NullReferenceException)
            {
                yield break;
            }

            if (!movedNext)
            {
                yield break;
            }

            ZiplineTower? current = enumerator.Current;
            if (!ReferenceEquals(current, null))
            {
                yield return current;
            }
        }
    }
}
