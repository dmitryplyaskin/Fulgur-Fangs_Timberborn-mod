using FulgurFangs.Code.Electricity;
using Timberborn.BaseComponentSystem;
using Timberborn.CoreUI;
using Timberborn.EntityPanelSystem;
using Timberborn.Localization;
using UnityEngine.UIElements;

namespace FulgurFangs.Code.UI;

public sealed class ElectricityNetworkFragment : IEntityPanelFragment
{
    private readonly VisualElementLoader _visualElementLoader;
    private readonly ILoc _loc;
    private VisualElement? _root;
    private Label? _generatorLabel;
    private Label? _consumerLabel;
    private Label? _networkLabel;
    private ElectricityPoleComponent? _node;
    private ElectricityAccumulatorComponent? _accumulator;

    public ElectricityNetworkFragment(VisualElementLoader visualElementLoader, ILoc loc)
    {
        _visualElementLoader = visualElementLoader;
        _loc = loc;
    }

    public VisualElement InitializeFragment()
    {
        _root = _visualElementLoader.LoadVisualElement("Game/EntityPanel/MechanicalNodeFragment");
        _generatorLabel = _root.Q<Label>("Generator");
        _consumerLabel = _root.Q<Label>("Consumer");
        _networkLabel = _root.Q<Label>("Network");
        if (_generatorLabel != null)
        {
            _generatorLabel.style.display = DisplayStyle.None;
        }

        if (_consumerLabel != null)
        {
            _consumerLabel.style.display = DisplayStyle.None;
        }

        SetVisible(false);
        return _root;
    }

    public void ShowFragment(BaseComponent entity)
    {
        _node = entity.GetComponent<ElectricityPoleComponent>();
        _accumulator = entity.GetComponent<ElectricityAccumulatorComponent>();
    }

    public void UpdateFragment()
    {
        if (_networkLabel == null)
        {
            SetVisible(false);
            return;
        }

        ElectricityService? service = ElectricityService.Instance;
        ElectricitySubnetworkSnapshot? snapshot = _node != null
            ? service?.GetNodeSnapshot(_node)
            : service?.GetAccumulatorSnapshot(_accumulator);

        if (!snapshot.HasValue)
        {
            SetVisible(false);
            return;
        }

        ElectricitySubnetworkSnapshot value = snapshot.Value;
        _networkLabel.text =
            $"{_loc.T("Electricity.Panel.Active")}: {value.Supply} kW\n" +
            $"{_loc.T("Electricity.Panel.NetworkCharge")}: {value.StoredCharge} / {value.StorageCapacity} kWh";
        SetVisible(true);
    }

    public void ClearFragment()
    {
        _node = null;
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
