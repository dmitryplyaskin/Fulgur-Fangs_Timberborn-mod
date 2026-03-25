using System;
using System.Collections.Generic;
using Timberborn.BlueprintSystem;
using Timberborn.CoreUI;
using Timberborn.EntitySystem;
using Timberborn.Localization;
using Timberborn.SelectionSystem;
using Timberborn.SingletonSystem;
using Timberborn.ToolSystem;
using Timberborn.TooltipSystem;
using UnityEngine;
using UnityEngine.UIElements;

namespace FulgurFangs.Code.Electricity;

public sealed class ElectricityConnectionButtonFactory : ILoadableSingleton
{
    private const string PlusIconClass = "icon--plus";
    private static readonly Color ConnectableColor = new(0.12f, 0.71f, 0.63f, 1f);
    private readonly VisualElementLoader _visualElementLoader;
    private readonly ElectricityConnectionAddingTool _electricityConnectionAddingTool;
    private readonly ToolService _toolService;
    private readonly EntitySelectionService _entitySelectionService;
    private readonly ElectricityConnectionService _electricityConnectionService;
    private readonly ElectricityCableRendererService _electricityCableRendererService;
    private readonly ElectricityService _electricityService;
    private readonly Highlighter _highlighter;
    private readonly ITooltipRegistrar _tooltipRegistrar;
    private readonly ILoc _loc;

    public ElectricityConnectionButtonFactory(
        VisualElementLoader visualElementLoader,
        ElectricityConnectionAddingTool electricityConnectionAddingTool,
        ToolService toolService,
        EntitySelectionService entitySelectionService,
        ElectricityConnectionService electricityConnectionService,
        ElectricityCableRendererService electricityCableRendererService,
        ElectricityService electricityService,
        Highlighter highlighter,
        ITooltipRegistrar tooltipRegistrar,
        ILoc loc)
    {
        _visualElementLoader = visualElementLoader;
        _electricityConnectionAddingTool = electricityConnectionAddingTool;
        _toolService = toolService;
        _entitySelectionService = entitySelectionService;
        _electricityConnectionService = electricityConnectionService;
        _electricityCableRendererService = electricityCableRendererService;
        _electricityService = electricityService;
        _highlighter = highlighter;
        _tooltipRegistrar = tooltipRegistrar;
        _loc = loc;
    }

    public void Load()
    {
    }

    public IReadOnlyList<ElectricityPoleComponent> GetOrderedTargets(ElectricityPoleComponent owner)
    {
        return _electricityConnectionService.GetExplicitConnectionTargets(owner);
    }

    public bool HasFreeSlots(ElectricityPoleComponent owner)
    {
        return _electricityConnectionService.GetExplicitConnectionCount(owner) < owner.MaxConnections;
    }

    public void CreateConnection(VisualElement root, ElectricityPoleComponent owner, ElectricityPoleComponent target)
    {
        SetForConnection(owner, target, Create(root));
    }

    public void CreateAddConnection(VisualElement root, ElectricityPoleComponent owner)
    {
        Button button = Create(root);
        button.RegisterCallback<ClickEvent>(_ =>
        {
            _electricityConnectionAddingTool.SwitchTo(owner);
            _toolService.SwitchTool(_electricityConnectionAddingTool);
        });
        SetName(button, _loc.T("Electricity.Connection.Button.Add"));
        SetIcon(button, null, PlusIconClass);
        SetRemoveConnectionButton(button);
        _tooltipRegistrar.RegisterLocalizable(button, "Electricity.Connection.Tooltip.Add");
    }

    private Button Create(VisualElement root)
    {
        Button button = _visualElementLoader.LoadVisualElement("Game/EntityPanel/ZiplineConnectionButton").Q<Button>();
        root.Add(button);
        return button;
    }

    private void SetForConnection(ElectricityPoleComponent owner, ElectricityPoleComponent target, Button button)
    {
        button.RegisterCallback<MouseEnterEvent>(_ => Highlight(owner, target));
        button.RegisterCallback<MouseLeaveEvent>(_ => Unhighlight(owner, target));
        button.RegisterCallback<DetachFromPanelEvent>(_ => Unhighlight(owner, target));
        button.RegisterCallback<ClickEvent>(_ => _entitySelectionService.Select(target));

        LabeledEntity? labeledEntity = target.GetComponent<LabeledEntity>();
        SetName(button, labeledEntity?.DisplayName ?? GetFallbackLabel(target));
        SetIcon(button, labeledEntity?.Image);
        _tooltipRegistrar.RegisterLocalizable(button, "Electricity.Connection.Tooltip.SelectTarget");
        SetRemoveConnectionButton(button, () => RemoveConnection(owner, target));
    }

    private void Highlight(ElectricityPoleComponent owner, ElectricityPoleComponent target)
    {
        _highlighter.HighlightPrimary(target, ConnectableColor);
        _electricityCableRendererService.HighlightConnection(owner, target, ConnectableColor);
    }

    private void Unhighlight(ElectricityPoleComponent owner, ElectricityPoleComponent target)
    {
        _electricityCableRendererService.UnhighlightConnection(owner, target);
        _highlighter.UnhighlightPrimary(target);
    }

    private void RemoveConnection(ElectricityPoleComponent owner, ElectricityPoleComponent target)
    {
        Unhighlight(owner, target);
        if (_electricityConnectionService.Disconnect(owner, target))
        {
            _electricityService.RefreshStateWithoutAdvancingTime();
        }

        _entitySelectionService.Unselect();
        _entitySelectionService.Select(owner);
    }

    private static void SetName(VisualElement root, string? text = null)
    {
        Label? label = root.Q<Label>("Name");
        if (label == null)
        {
            return;
        }

        label.text = text;
        label.ToggleDisplayStyle(text != null);
    }

    private static void SetIcon(VisualElement root, Sprite? sprite, string? className = null)
    {
        Image? image = root.Q<Image>("Icon");
        if (image == null)
        {
            return;
        }

        if (sprite != null)
        {
            image.sprite = sprite;
        }

        if (!string.IsNullOrEmpty(className))
        {
            image.AddToClassList(className);
        }
    }

    private void SetRemoveConnectionButton(VisualElement root, Action? actionCallback = null)
    {
        Button? button = root.Q<Button>("RemoveConnection");
        if (button == null)
        {
            return;
        }

        if (actionCallback != null)
        {
            button.RegisterCallback<ClickEvent>(_ => actionCallback());
            _tooltipRegistrar.RegisterLocalizable(button, "Electricity.Connection.Tooltip.Remove");
        }
        else
        {
            button.ToggleDisplayStyle(false);
        }
    }

    private string GetFallbackLabel(ElectricityPoleComponent target)
    {
        Vector3Int coordinates = target.BlockCoordinates;
        return $"{_loc.T("Electricity.Connection.Target")} [{coordinates.x},{coordinates.y},{coordinates.z}]";
    }
}
