using System.Collections.Generic;
using System.Linq;
using Timberborn.BaseComponentSystem;
using Timberborn.BlockSystem;
using Timberborn.BuildingRange;
using Timberborn.EntitySystem;
using Timberborn.RangedEffectBuildingUI;
using Timberborn.SelectionSystem;
using Timberborn.TerrainSystem;
using Timberborn.ZiplineSystem;
using UnityEngine;

namespace FulgurFangs.Code.Electricity;

public sealed class ElectricityPoleComponent : BuildingWithTerrainRange, IPostInitializableEntity, IDeletableEntity, IBuildingWithRange, ISelectionListener, IFinishedStateListener
{
    private static readonly Color NetworkHighlightColor = new(0.055f, 0.26f, 0.275f, 1f);

    private readonly ElectricityService _electricityService;
    private readonly Highlighter _highlighter;
    private readonly RangeObjectHighlighterService _rangeObjectHighlighterService;
    private readonly RangeTileMarkerService _rangeTileMarkerService;
    private readonly ITerrainService _terrainService;
    private BlockObject? _blockObject;
    private bool _isFinished;
    private ZiplineTower? _tower;
    private int _range;
    private int _transmissionLoss;
    private bool _rangeServicesRegistered;
    private List<ElectricityPoleComponent> _highlightedPoles = new();
    private string _rangeName = "ElectricityPole.Uninitialized";

    public ElectricityPoleComponent(
        ElectricityService electricityService,
        Highlighter highlighter,
        RangeObjectHighlighterService rangeObjectHighlighterService,
        RangeTileMarkerService rangeTileMarkerService,
        ITerrainService terrainService)
    {
        _electricityService = electricityService;
        _highlighter = highlighter;
        _rangeObjectHighlighterService = rangeObjectHighlighterService;
        _rangeTileMarkerService = rangeTileMarkerService;
        _terrainService = terrainService;
    }

    public bool IsReady => Enabled && _isFinished;

    public string RangeName => _rangeName;

    public ZiplineTower? Tower => _tower;

    public int TransmissionLoss => Mathf.Max(0, _transmissionLoss);

    public bool HasDistributionRange => _range > 0;

    public int InstanceId => Transform.GetInstanceID();

    public Vector3 WorldPosition => Transform.position;

    public Vector3Int BlockCoordinates => !ReferenceEquals(_blockObject, null)
        ? _blockObject.CoordinatesAtBaseZ
        : new Vector3Int(
            Mathf.RoundToInt(WorldPosition.x),
            Mathf.RoundToInt(WorldPosition.z),
            Mathf.RoundToInt(WorldPosition.y));

    public void PostInitializeEntity()
    {
        _blockObject = GetComponent<BlockObject>() ?? Transform.GetComponentInParent<BlockObject>();
        _isFinished = _blockObject != null && _blockObject.IsFinished;
        _tower = GetComponent<ZiplineTower>() ?? GetComponentInChildren<ZiplineTower>(true);
        _rangeName = $"ElectricityPole.{Transform.GetInstanceID()}";
        if (HasDistributionRange)
        {
            _rangeTileMarkerService.AddBuildingWithRange(this);
            _rangeObjectHighlighterService.AddBuildingWithObjectRange(this);
            _rangeServicesRegistered = true;
        }

        _electricityService.RegisterPole(this);
    }

    public void DeleteEntity()
    {
        if (_rangeServicesRegistered)
        {
            _rangeTileMarkerService.RemoveBuildingWithRange(this);
            _rangeObjectHighlighterService.RemoveBuildingWithObjectRange(this);
            _rangeServicesRegistered = false;
        }

        _electricityService.UnregisterPole(this);
    }

    public void SetRange(int range)
    {
        _range = range;
    }

    public void SetTransmissionLoss(int transmissionLoss)
    {
        _transmissionLoss = transmissionLoss;
    }

    public bool InRangeOf(Vector3 position)
    {
        if (!HasDistributionRange)
        {
            return false;
        }

        return Mathf.Abs(position.x - WorldPosition.x) <= _range &&
               Mathf.Abs(position.z - WorldPosition.z) <= _range;
    }

    public IEnumerable<Vector3Int> GetBlocksInRange()
    {
        if (!HasDistributionRange)
        {
            yield break;
        }

        Vector3Int center = BlockCoordinates;
        for (int x = center.x - _range; x <= center.x + _range; x++)
        {
            for (int y = center.y - _range; y <= center.y + _range; y++)
            {
                if (x < 0 || y < 0 || x >= _terrainService.Size.x || y >= _terrainService.Size.y)
                {
                    continue;
                }

                yield return GetProjectedTerrainCoordinates(x, y, center.z);
            }
        }
    }

    public IEnumerable<BaseComponent> GetObjectsInRange()
    {
        return HasDistributionRange
            ? _electricityService.GetElectricObjectsInRange(this)
            : System.Array.Empty<BaseComponent>();
    }

    public void OnSelect()
    {
        if (_rangeServicesRegistered)
        {
            _rangeTileMarkerService.RecalculateArea(RangeName);
            _rangeTileMarkerService.DrawArea();
            _rangeTileMarkerService.ShowArea();
            _rangeObjectHighlighterService.RecalculateAreaAndHighlightObjects(RangeName);
        }

        _highlightedPoles = _electricityService.GetConnectedPoles(this).Where(static pole => pole != null).ToList();
        foreach (ElectricityPoleComponent pole in _highlightedPoles)
        {
            _highlighter.HighlightSecondary(pole, NetworkHighlightColor);
        }
    }

    public void OnUnselect()
    {
        if (_rangeServicesRegistered)
        {
            _rangeTileMarkerService.HideArea();
            _rangeObjectHighlighterService.ClearHighlights();
        }

        foreach (ElectricityPoleComponent pole in _highlightedPoles)
        {
            if (pole != null)
            {
                _highlighter.UnhighlightSecondary(pole);
            }
        }

        _highlightedPoles.Clear();
    }

    public void OnEnterFinishedState()
    {
        _isFinished = true;
    }

    public void OnExitFinishedState()
    {
        _isFinished = false;
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

    private Vector3Int GetProjectedTerrainCoordinates(int x, int y, int fallbackZ)
    {
        int terrainZ = _terrainService
            .GetAllHeightsInCell(new Vector2Int(x, y))
            .Select(static coordinates => coordinates.z)
            .DefaultIfEmpty(fallbackZ)
            .Max();

        return new Vector3Int(x, y, terrainZ);
    }
}
