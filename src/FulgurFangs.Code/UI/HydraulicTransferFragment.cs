using FulgurFangs.Code.Hydraulics;
using Timberborn.BaseComponentSystem;
using Timberborn.CoreUI;
using Timberborn.EntityPanelSystem;
using Timberborn.Localization;
using Timberborn.UIFormatters;
using UnityEngine.UIElements;

namespace FulgurFangs.Code.UI;

public sealed class HydraulicTransferFragment : IEntityPanelFragment
{
    private readonly VisualElementLoader _visualElementLoader;
    private readonly ILoc _loc;
    private readonly Phrase _flowLimitPhrase = Phrase.New("Hydraulics.Panel.FlowLimit").Format<float>(UnitFormatter.FormatFlow);
    private readonly Phrase _currentFlowPhrase = Phrase.New("Hydraulics.Panel.CurrentFlow").Format<float>(UnitFormatter.FormatFlow);
    private VisualElement? _root;
    private Label? _flowLimitLabel;
    private PreciseSlider? _flowLimitSlider;
    private Label? _flowLimitStateLabel;
    private HydraulicTransferComponent? _hydraulicTransferComponent;
    private MultiCellValveComponent? _multiCellValveComponent;

    public HydraulicTransferFragment(VisualElementLoader visualElementLoader, ILoc loc)
    {
        _visualElementLoader = visualElementLoader;
        _loc = loc;
    }

    public VisualElement InitializeFragment()
    {
        _root = _visualElementLoader.LoadVisualElement("Game/EntityPanel/ValveFragment");
        _flowLimitLabel = _root.Q<Label>("OutflowLimitLabel");
        _flowLimitStateLabel = _root.Q<Label>("OutflowLimitStateLabel");
        _flowLimitSlider = _root.Q<PreciseSlider>("OutflowLimitSlider");
        _flowLimitSlider.SetValueChangedCallback(SetFlowLimit);

        _root.Q<Label>("ValveState")?.ToggleDisplayStyle(false);
        _root.Q<VisualElement>("AutomationOutflowLimitWrapper")?.ToggleDisplayStyle(false);
        _root.Q<VisualElement>("ReactionSpeedWrapper")?.ToggleDisplayStyle(false);
        _root.Q<Toggle>("Synchronize")?.ToggleDisplayStyle(false);
        if (_flowLimitStateLabel != null)
        {
            _flowLimitStateLabel.ToggleDisplayStyle(true);
        }

        SetVisible(false);
        return _root;
    }

    public void ShowFragment(BaseComponent entity)
    {
        _hydraulicTransferComponent = entity.GetComponent<HydraulicTransferComponent>();
        _multiCellValveComponent = entity.GetComponent<MultiCellValveComponent>();
        if (_flowLimitSlider == null)
        {
            return;
        }

        if (_multiCellValveComponent != null)
        {
            _flowLimitSlider.SetStepWithoutNotify(_multiCellValveComponent.OutflowLimitStep);
            return;
        }

        if (_hydraulicTransferComponent != null)
        {
            _flowLimitSlider.SetStepWithoutNotify(_hydraulicTransferComponent.ThrottleStep * _hydraulicTransferComponent.MaxWaterPerSecond);
        }
    }

    public void UpdateFragment()
    {
        if (_flowLimitSlider == null || _flowLimitLabel == null || _flowLimitStateLabel == null)
        {
            SetVisible(false);
            return;
        }

        if (_multiCellValveComponent != null)
        {
            float maxFlow = _multiCellValveComponent.MaxOutflowLimit;
            float currentLimit = _multiCellValveComponent.OutflowLimit;
            _flowLimitSlider.UpdateValuesWithoutNotify(currentLimit, maxFlow);
            _flowLimitSlider.SetMarker(_multiCellValveComponent.CurrentFlow);
            _flowLimitLabel.text = _loc.T(_flowLimitPhrase, currentLimit);
            _flowLimitStateLabel.text = _loc.T(_currentFlowPhrase, _multiCellValveComponent.CurrentFlow);
            SetVisible(true);
            return;
        }

        if (_hydraulicTransferComponent != null)
        {
            float maxFlow = _hydraulicTransferComponent.MaxWaterPerSecond;
            float currentLimit = _hydraulicTransferComponent.FlowLimitPerSecond;
            _flowLimitSlider.UpdateValuesWithoutNotify(currentLimit, maxFlow);
            _flowLimitSlider.SetMarker(_hydraulicTransferComponent.CurrentTransferPerSecond);
            _flowLimitLabel.text = _loc.T(_flowLimitPhrase, currentLimit);
            _flowLimitStateLabel.text = _loc.T(_currentFlowPhrase, _hydraulicTransferComponent.CurrentTransferPerSecond);
            SetVisible(true);
            return;
        }

        SetVisible(false);
    }

    public void ClearFragment()
    {
        _hydraulicTransferComponent = null;
        _multiCellValveComponent = null;
        SetVisible(false);
    }

    private void SetFlowLimit(float flowLimit)
    {
        if (_multiCellValveComponent != null)
        {
            _multiCellValveComponent.SetOutflowLimit(flowLimit);
            return;
        }

        _hydraulicTransferComponent?.SetFlowLimitPerSecond(flowLimit);
    }

    private void SetVisible(bool visible)
    {
        if (_root != null)
        {
            _root.ToggleDisplayStyle(visible);
        }
    }
}
