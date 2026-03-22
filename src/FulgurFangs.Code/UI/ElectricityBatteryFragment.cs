using FulgurFangs.Code.Electricity;
using Timberborn.BaseComponentSystem;
using Timberborn.CoreUI;
using Timberborn.EntityPanelSystem;
using UnityEngine.UIElements;

namespace FulgurFangs.Code.UI;

public sealed class ElectricityBatteryFragment : IEntityPanelFragment
{
    private readonly VisualElementLoader _visualElementLoader;
    private VisualElement? _root;
    private Label? _chargeLabel;
    private Timberborn.CoreUI.ProgressBar? _progressBar;
    private Slider? _chargeSlider;
    private ElectricityAccumulatorComponent? _accumulator;

    public ElectricityBatteryFragment(VisualElementLoader visualElementLoader)
    {
        _visualElementLoader = visualElementLoader;
    }

    public VisualElement InitializeFragment()
    {
        _root = _visualElementLoader.LoadVisualElement("Game/EntityPanel/BatteryFragment");
        _chargeLabel = _root.Q<Label>("Charge");
        _progressBar = _root.Q<Timberborn.CoreUI.ProgressBar>("ProgressBar");
        _chargeSlider = _root.Q<Slider>("ChargeSlider");
        if (_chargeSlider != null)
        {
            _chargeSlider.style.display = DisplayStyle.None;
        }

        SetVisible(false);
        return _root;
    }

    public void ShowFragment(BaseComponent entity)
    {
        _accumulator = entity.GetComponent<ElectricityAccumulatorComponent>();
    }

    public void UpdateFragment()
    {
        if (_root == null || _chargeLabel == null || _progressBar == null || _accumulator == null || !_accumulator.Enabled)
        {
            SetVisible(false);
            return;
        }

        int currentCharge = _accumulator.RoundedCurrentCharge;
        int capacity = _accumulator.RoundedCapacity;
        _chargeLabel.text = $"{currentCharge} / {capacity} kWh";
        _progressBar.SetProgress(_accumulator.ChargeLevel);
        SetVisible(true);
    }

    public void ClearFragment()
    {
        _accumulator = null;
        SetVisible(false);
    }

    private void SetVisible(bool visible)
    {
        if (_root != null)
        {
            _root.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}
