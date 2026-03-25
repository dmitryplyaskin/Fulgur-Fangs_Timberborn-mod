using System.Collections.Generic;
using Timberborn.BaseComponentSystem;
using Timberborn.BlockSystem;
using Timberborn.EntitySystem;
using Timberborn.Localization;
using Timberborn.StatusSystem;
using Timberborn.TickSystem;
using UnityEngine;
using System.Linq;

namespace FulgurFangs.Code.Electricity;

public sealed class ElectricityConsumerComponent : TickableComponent, IPostInitializableEntity, IDeletableEntity, IFinishedStateListener
{
    private static readonly Color NetworkHighlightColor = new(0.055f, 0.26f, 0.275f, 1f);

    private readonly ElectricityService _electricityService;
    private readonly ILoc _loc;
    private BlockObject? _blockObject;
    private StatusSubject? _statusSubject;
    private StatusToggle? _noPowerStatusToggle;
    private List<ElectricityPoleComponent> _highlightedNodes = new();
    private bool _isFinished;
    private int _demand;

    public ElectricityConsumerComponent(ElectricityService electricityService, ILoc loc)
    {
        _electricityService = electricityService;
        _loc = loc;
    }

    public bool IsReady => Enabled && _isFinished;

    public int Demand => _demand;

    public bool Powered => SupplyFraction > 0f;

    public float SupplyFraction { get; private set; }

    public int InstanceId => Transform.GetInstanceID();

    public Vector3 WorldPosition => GetWorldPosition();

    public void PostInitializeEntity()
    {
        _blockObject = GetComponent<BlockObject>() ?? Transform.GetComponentInParent<BlockObject>();
        _statusSubject = GetComponent<StatusSubject>();
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
        UpdateStatus();
    }

    public void SetPowered(bool powered)
    {
        SetSupplyFraction(powered ? 1f : 0f);
    }

    public void SetSupplyFraction(float supplyFraction)
    {
        SupplyFraction = Mathf.Clamp01(supplyFraction);
        UpdateStatus();
    }

    public override void StartTickable()
    {
        InitializeStatus();
        UpdateStatus();
    }

    public override void Tick()
    {
        UpdateStatus();
    }

    public void OnEnterFinishedState()
    {
        _isFinished = true;
        _electricityService.RefreshStateWithoutAdvancingTime();
        UpdateStatus();
    }

    public void OnExitFinishedState()
    {
        _isFinished = false;
        SetSupplyFraction(0f);
        _electricityService.RefreshStateWithoutAdvancingTime();
        UpdateStatus();
    }

    public void HighlightNetworkSelection()
    {
        ClearNetworkSelection();

        _highlightedNodes = _electricityService
            .GetConsumerNetworkNodes(this)
            .Where(static node => node != null)
            .Distinct()
            .ToList();

        foreach (ElectricityPoleComponent node in _highlightedNodes)
        {
            node.HighlightSecondary(NetworkHighlightColor);
        }
    }

    public void ClearNetworkSelection()
    {
        foreach (ElectricityPoleComponent node in _highlightedNodes)
        {
            if (node != null)
            {
                node.UnhighlightSecondary();
            }
        }

        _highlightedNodes.Clear();
    }

    private void InitializeStatus()
    {
        if (_statusSubject == null || _noPowerStatusToggle != null)
        {
            return;
        }

        _noPowerStatusToggle = StatusToggle.CreateNormalStatusWithAlertAndFloatingIcon(
            "NoPower",
            _loc.T("Status.Electricity.NoPower"),
            _loc.T("Status.Electricity.NoPower.Short"),
            0f);
        _statusSubject.RegisterStatus(_noPowerStatusToggle);
    }

    private void UpdateStatus()
    {
        _noPowerStatusToggle?.Toggle(IsReady && _demand > 0 && SupplyFraction < 0.999f);
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
