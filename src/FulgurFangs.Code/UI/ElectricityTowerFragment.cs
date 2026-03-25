using FulgurFangs.Code.Electricity;
using Timberborn.BaseComponentSystem;
using Timberborn.CoreUI;
using Timberborn.EntityPanelSystem;
using UnityEngine.UIElements;

namespace FulgurFangs.Code.UI;

public sealed class ElectricityTowerFragment : IEntityPanelFragment
{
    private readonly VisualElementLoader _visualElementLoader;
    private readonly ElectricityConnectionButtonFactory _electricityConnectionButtonFactory;
    private VisualElement? _root;
    private VisualElement? _buttons;
    private ElectricityPoleComponent? _pole;

    public ElectricityTowerFragment(
        VisualElementLoader visualElementLoader,
        ElectricityConnectionButtonFactory electricityConnectionButtonFactory)
    {
        _visualElementLoader = visualElementLoader;
        _electricityConnectionButtonFactory = electricityConnectionButtonFactory;
    }

    public VisualElement InitializeFragment()
    {
        _root = _visualElementLoader.LoadVisualElement("Game/EntityPanel/ZiplineTowerFragment");
        _buttons = _root.Q<VisualElement>("Buttons");
        _root.ToggleDisplayStyle(false);
        return _root;
    }

    public void ShowFragment(BaseComponent entity)
    {
        _pole = entity.GetComponent<ElectricityPoleComponent>();
        CreateButtons();
    }

    public void UpdateFragment()
    {
    }

    public void ClearFragment()
    {
        _pole = null;
        _buttons?.Clear();
        _root?.ToggleDisplayStyle(false);
    }

    private void CreateButtons()
    {
        if (_root == null || _buttons == null || _pole == null || !_pole.IsReady || _pole.MaxConnections <= 0 || _pole.MaxDistance <= 0f)
        {
            _buttons?.Clear();
            _root?.ToggleDisplayStyle(false);
            return;
        }

        _buttons.Clear();
        foreach (ElectricityPoleComponent target in _electricityConnectionButtonFactory.GetOrderedTargets(_pole))
        {
            _electricityConnectionButtonFactory.CreateConnection(_buttons, _pole, target);
        }

        if (_electricityConnectionButtonFactory.HasFreeSlots(_pole))
        {
            _electricityConnectionButtonFactory.CreateAddConnection(_buttons, _pole);
        }

        _root.ToggleDisplayStyle(true);
    }
}
