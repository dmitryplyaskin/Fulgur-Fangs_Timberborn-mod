using System.Collections.Generic;
using System.Linq;
using Timberborn.BaseComponentSystem;
using Timberborn.BlockSystem;
using Timberborn.BuildingRange;
using Timberborn.EntitySystem;
using Timberborn.Persistence;
using Timberborn.RangedEffectBuildingUI;
using Timberborn.SelectionSystem;
using Timberborn.TerrainSystem;
using Timberborn.WorldPersistence;
using UnityEngine;

namespace FulgurFangs.Code.Electricity;

public sealed class ElectricityPoleComponent : BuildingWithTerrainRange, IPostInitializableEntity, IDeletableEntity, IBuildingWithRange, ISelectionListener, IFinishedStateListener, IPersistentEntity
{
    private static readonly Color NetworkHighlightColor = new(0.055f, 0.26f, 0.275f, 1f);
    private static readonly ComponentKey SaveKey = new("FulgurFangs.ElectricityPole");
    private static readonly ListKey<ElectricityPoleComponent> ConnectionsKey = new("Connections");

    private readonly ElectricityConnectionService _electricityConnectionService;
    private readonly ElectricityService _electricityService;
    private readonly Highlighter _highlighter;
    private readonly ReferenceSerializer _referenceSerializer;
    private readonly RangeObjectHighlighterService _rangeObjectHighlighterService;
    private readonly RangeTileMarkerService _rangeTileMarkerService;
    private readonly ITerrainService _terrainService;
    private BlockObject? _blockObject;
    private bool _isFinished;
    private Vector3 _cableAnchorPoint;
    private int _maxConnections;
    private float _maxDistance;
    private int _range;
    private int _transmissionLoss;
    private bool _rangeServicesRegistered;
    private List<ElectricityPoleComponent> _highlightedPoles = new();
    private List<ElectricityPoleComponent> _loadedConnections = new();
    private string _rangeName = "ElectricityPole.Uninitialized";

    public ElectricityPoleComponent(
        ElectricityConnectionService electricityConnectionService,
        ElectricityService electricityService,
        Highlighter highlighter,
        ReferenceSerializer referenceSerializer,
        RangeObjectHighlighterService rangeObjectHighlighterService,
        RangeTileMarkerService rangeTileMarkerService,
        ITerrainService terrainService)
    {
        _electricityConnectionService = electricityConnectionService;
        _electricityService = electricityService;
        _highlighter = highlighter;
        _referenceSerializer = referenceSerializer;
        _rangeObjectHighlighterService = rangeObjectHighlighterService;
        _rangeTileMarkerService = rangeTileMarkerService;
        _terrainService = terrainService;
    }

    public bool IsReady => Enabled && _isFinished;

    public string RangeName => _rangeName;

    public int TransmissionLoss => Mathf.Max(0, _transmissionLoss);

    public bool HasDistributionRange => _range > 0;

    public int MaxConnections => Mathf.Max(0, _maxConnections);

    public float MaxDistance => Mathf.Max(0f, _maxDistance);

    public int InstanceId => GameObject != null ? GameObject.GetInstanceID() : 0;

    public Vector3 WorldPosition => GetWorldPosition();

    public Vector3 CableAnchorWorldPosition => GetCableAnchorWorldPosition();

    public Vector3Int BlockCoordinates => !ReferenceEquals(_blockObject, null)
        ? _blockObject.CoordinatesAtBaseZ
        : GetFallbackBlockCoordinates();

    public void PostInitializeEntity()
    {
        _blockObject = GetComponent<BlockObject>() ?? Transform.GetComponentInParent<BlockObject>();
        _isFinished = _blockObject != null && _blockObject.IsFinished;
        _rangeName = $"ElectricityPole.{Transform.GetInstanceID()}";
        _electricityConnectionService.RegisterPole(this);
        if (_loadedConnections.Count > 0)
        {
            _electricityConnectionService.AddLoadedConnections(this, _loadedConnections);
            _loadedConnections.Clear();
        }

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
        OnUnselect();
        _highlighter.UnhighlightPrimary(this);
        _highlighter.UnhighlightSecondary(this);
        if (_rangeServicesRegistered)
        {
            _rangeTileMarkerService.RemoveBuildingWithRange(this);
            _rangeObjectHighlighterService.RemoveBuildingWithObjectRange(this);
            _rangeServicesRegistered = false;
        }

        _electricityConnectionService.UnregisterPole(this);
        _electricityService.UnregisterPole(this);
    }

    public void Save(IEntitySaver entitySaver)
    {
        entitySaver.GetComponent(SaveKey).Set(
            ConnectionsKey,
            _electricityConnectionService.GetExplicitConnectionTargets(this),
            _referenceSerializer.Of<ElectricityPoleComponent>());
    }

    public void Load(IEntityLoader entityLoader)
    {
        if (entityLoader.TryGetComponent(SaveKey, out IObjectLoader componentLoader))
        {
            _loadedConnections = componentLoader.Get(
                ConnectionsKey,
                _referenceSerializer.Of<ElectricityPoleComponent>());
        }
    }

    public void SetRange(int range)
    {
        _range = range;
    }

    public void SetTransmissionLoss(int transmissionLoss)
    {
        _transmissionLoss = transmissionLoss;
    }

    public void SetTowerParameters(Vector3 cableAnchorPoint, int maxConnections, float maxDistance)
    {
        _cableAnchorPoint = cableAnchorPoint;
        _maxConnections = maxConnections;
        _maxDistance = maxDistance;
    }

    public bool InRangeOf(Vector3 position)
    {
        if (!HasDistributionRange)
        {
            return false;
        }

        Vector3Int coordinates = BlockCoordinates;
        return Mathf.Abs(position.x - coordinates.x) <= _range &&
               Mathf.Abs(position.z - coordinates.y) <= _range;
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
            pole.HighlightSecondary(NetworkHighlightColor);
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
                pole.UnhighlightSecondary();
            }
        }

        _highlightedPoles.Clear();
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

    public void HighlightSecondary(Color color)
    {
        _highlighter.HighlightSecondary(this, color);
    }

    public void UnhighlightSecondary()
    {
        _highlighter.UnhighlightSecondary(this);
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

    private Vector3 GetWorldPosition()
    {
        if (!ReferenceEquals(_blockObject, null))
        {
            Vector3Int coordinates = _blockObject.CoordinatesAtBaseZ;
            return new Vector3(coordinates.x + 0.5f, coordinates.z, coordinates.y + 0.5f);
        }

        return Transform != null ? Transform.position : Vector3.zero;
    }

    private Vector3 GetCableAnchorWorldPosition()
    {
        if (Transform != null)
        {
            return Transform.TransformPoint(_cableAnchorPoint);
        }

        return GetWorldPosition() + new Vector3(_cableAnchorPoint.x - 0.5f, _cableAnchorPoint.y, _cableAnchorPoint.z - 0.5f);
    }

    private Vector3Int GetFallbackBlockCoordinates()
    {
        if (Transform == null)
        {
            return Vector3Int.zero;
        }

        Vector3 position = Transform.position;
        return new Vector3Int(
            Mathf.RoundToInt(position.x),
            Mathf.RoundToInt(position.z),
            Mathf.RoundToInt(position.y));
    }
}
