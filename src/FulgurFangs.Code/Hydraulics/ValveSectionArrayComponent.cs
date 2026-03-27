using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Timberborn.BaseComponentSystem;
using Timberborn.BlockSystem;
using Timberborn.Buildings;
using Timberborn.ConstructionSites;
using Timberborn.Coordinates;
using Timberborn.EntitySystem;
using Timberborn.Persistence;
using Timberborn.TemplateSystem;
using Timberborn.WaterBuildings;
using Timberborn.WaterSystem;
using Timberborn.WorldPersistence;
using UnityEngine;

namespace FulgurFangs.Code.Hydraulics;

public sealed class ValveSectionArrayComponent : BaseComponent, IAwakableComponent, IInitializableEntity, IPostPlacementChangeListener, IDeletableEntity, IFinishedStateListener, IPersistentEntity
{
    private const float Epsilon = 0.0001f;
    private static readonly ComponentKey SaveKey = new("FulgurFangs.ValveSectionArray");
    private static readonly PropertyKey<float> OutflowLimitKey = new("OutflowLimit");

    private readonly ConstructionFactory _constructionFactory;
    private readonly TemplateNameMapper _templateNameMapper;
    private readonly IBlockService _blockService;
    private readonly EntityService _entityService;
    private readonly IThreadSafeWaterMap _threadSafeWaterMap;
    private readonly List<Valve> _sectionValves = new();
    private BlockObject? _blockObject;
    private bool _isFinished;
    private bool _loadedFromSave;
    private string _sectionTemplateName = string.Empty;
    private ImmutableArray<Vector3Int> _sectionCoordinates = ImmutableArray<Vector3Int>.Empty;
    private float _maxOutflowLimit = 2f;
    private float _outflowLimitStep = 0.01f;
    private float _outflowLimit = 1f;

    public ValveSectionArrayComponent(
        ConstructionFactory constructionFactory,
        TemplateNameMapper templateNameMapper,
        IBlockService blockService,
        EntityService entityService,
        IThreadSafeWaterMap threadSafeWaterMap)
    {
        _constructionFactory = constructionFactory;
        _templateNameMapper = templateNameMapper;
        _blockService = blockService;
        _entityService = entityService;
        _threadSafeWaterMap = threadSafeWaterMap;
    }

    public float MaxOutflowLimit => _maxOutflowLimit;

    public float OutflowLimitStep => _outflowLimitStep;

    public float OutflowLimit => _outflowLimit;

    public float CurrentFlow
    {
        get
        {
            if (!_isFinished || _blockObject == null || _sectionCoordinates.IsDefaultOrEmpty)
            {
                return 0f;
            }

            float totalFlow = 0f;
            foreach (Vector3Int sectionCoordinates in _sectionCoordinates)
            {
                Vector3Int lowerCoordinates = _blockObject.TransformCoordinates(sectionCoordinates);
                Vector3Int upperCoordinates = _blockObject.TransformCoordinates(sectionCoordinates + Vector3Int.forward);
                totalFlow += GetCellFlow(lowerCoordinates);
                totalFlow += GetCellFlow(upperCoordinates);
            }

            return totalFlow;
        }
    }

    public void Awake()
    {
        _blockObject = GetComponent<BlockObject>() ?? Transform.GetComponentInParent<BlockObject>();
        _isFinished = _blockObject != null && _blockObject.IsFinished;
    }

    public void InitializeEntity()
    {
        if (!_loadedFromSave)
        {
            _outflowLimit = Mathf.Clamp(_outflowLimit, 0f, _maxOutflowLimit);
        }

        if (_isFinished)
        {
            EnsureSections();
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

    public void SetParameters(ValveSectionArraySpec spec)
    {
        _sectionTemplateName = spec.SectionTemplateName;
        _sectionCoordinates = spec.SectionCoordinates;
        _maxOutflowLimit = Mathf.Max(Epsilon, spec.MaxOutflowLimit);
        _outflowLimitStep = Mathf.Max(0.001f, spec.OutflowLimitStep);
        _outflowLimit = _loadedFromSave
            ? Mathf.Clamp(_outflowLimit, 0f, _maxOutflowLimit)
            : Mathf.Clamp(spec.DefaultOutflowLimit, 0f, _maxOutflowLimit);

        if (_isFinished)
        {
            EnsureSections();
        }
    }

    public void SetOutflowLimit(float outflowLimit)
    {
        _outflowLimit = Mathf.Clamp(outflowLimit, 0f, _maxOutflowLimit);
        ApplyOutflowLimitToSections();
    }

    public void OnPostPlacementChanged()
    {
        if (!_isFinished)
        {
            return;
        }

        DeleteSections();
        EnsureSections();
    }

    public void OnEnterFinishedState()
    {
        _isFinished = true;
        EnsureSections();
    }

    public void OnExitFinishedState()
    {
        _isFinished = false;
        DeleteSections();
    }

    public void DeleteEntity()
    {
        DeleteSections();
    }

    private void EnsureSections()
    {
        if (_blockObject == null || _sectionCoordinates.IsDefaultOrEmpty || string.IsNullOrWhiteSpace(_sectionTemplateName))
        {
            return;
        }

        _sectionValves.Clear();
        BuildingSpec sectionBuildingSpec = _templateNameMapper.GetTemplate(_sectionTemplateName).Blueprint.GetSpec<BuildingSpec>();
        foreach (Vector3Int sectionCoordinates in _sectionCoordinates)
        {
            Vector3Int worldCoordinates = _blockObject.TransformCoordinates(sectionCoordinates);
            Valve? valve = ResolveExistingSection(worldCoordinates);
            if (valve == null)
            {
                Placement placement = new(worldCoordinates, _blockObject.Orientation, _blockObject.FlipMode);
                BaseComponent section = _constructionFactory.CreateAsFinished(sectionBuildingSpec, placement);
                valve = section.GetComponent<Valve>();
            }

            if (valve == null)
            {
                continue;
            }

            valve.SetOutflowLimit(_outflowLimit);
            _sectionValves.Add(valve);
        }
    }

    private Valve? ResolveExistingSection(Vector3Int worldCoordinates)
    {
        foreach (Valve valve in _blockService.GetObjectsWithComponentAt<Valve>(worldCoordinates))
        {
            TemplateSpec? templateSpec = valve.GetComponent<TemplateSpec>();
            if (templateSpec != null && templateSpec.TemplateName == _sectionTemplateName)
            {
                return valve;
            }
        }

        return null;
    }

    private void ApplyOutflowLimitToSections()
    {
        foreach (Valve valve in _sectionValves.Where(section => section != null))
        {
            valve.SetOutflowLimit(_outflowLimit);
        }
    }

    private void DeleteSections()
    {
        foreach (Valve valve in _sectionValves.Where(section => section != null).Distinct())
        {
            TemplateSpec? templateSpec = valve.GetComponent<TemplateSpec>();
            if (templateSpec != null && templateSpec.TemplateName == _sectionTemplateName)
            {
                _entityService.Delete(valve);
            }
        }

        _sectionValves.Clear();
    }

    private float GetCellFlow(Vector3Int coordinates)
    {
        if (!_threadSafeWaterMap.CellIsUnderwater(coordinates))
        {
            return 0f;
        }

        return _threadSafeWaterMap.WaterFlowDirection(coordinates).magnitude;
    }
}
