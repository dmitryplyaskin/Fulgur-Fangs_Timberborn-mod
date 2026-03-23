using FulgurFangs.Code.Electricity;
using Timberborn.BaseComponentSystem;
using Timberborn.CoreUI;
using Timberborn.EntityPanelSystem;
using Timberborn.Localization;
using UnityEngine;
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
    private ElectricityConsumerComponent? _consumer;

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
        _consumer = entity.GetComponent<ElectricityConsumerComponent>();
    }

    public void UpdateFragment()
    {
        if (_generatorLabel == null || _consumerLabel == null || _networkLabel == null)
        {
            SetVisible(false);
            return;
        }

        ElectricityService? service = ElectricityService.Instance;
        if (_consumer != null)
        {
            RenderConsumer(service, _consumer);
            return;
        }

        ElectricitySubnetworkSnapshot? snapshot = _node != null
            ? service?.GetNodeSnapshot(_node)
            : service?.GetAccumulatorSnapshot(_accumulator);

        if (!snapshot.HasValue)
        {
            SetVisible(false);
            return;
        }

        ElectricitySubnetworkSnapshot value = snapshot.Value;
        _generatorLabel.style.display = DisplayStyle.None;
        _consumerLabel.style.display = DisplayStyle.Flex;
        _networkLabel.style.display = DisplayStyle.Flex;
        _consumerLabel.text = $"{_loc.T("Electricity.Panel.Active")}: {value.Supply} kW";
        _networkLabel.text = $"{_loc.T("Electricity.Panel.NetworkCharge")}: {value.StoredCharge} / {value.StorageCapacity} kWh";
        SetVisible(true);
    }

    public void ClearFragment()
    {
        _node = null;
        _accumulator = null;
        _consumer = null;
        SetVisible(false);
    }

    private void RenderConsumer(ElectricityService? service, ElectricityConsumerComponent consumer)
    {
        if (_generatorLabel == null || _consumerLabel == null || _networkLabel == null)
        {
            SetVisible(false);
            return;
        }

        ElectricitySubnetworkSnapshot? snapshot = service?.GetConsumerSnapshot(consumer);
        int demand = Mathf.Max(0, consumer.Demand);
        int deliveredPower = Mathf.RoundToInt(demand * Mathf.Clamp01(consumer.SupplyFraction));
        int efficiency = demand > 0 ? Mathf.RoundToInt(consumer.SupplyFraction * 100f) : 0;
        int networkSupply = snapshot?.Supply ?? 0;
        int networkConsumption = snapshot?.Consumption ?? 0;

        _generatorLabel.style.display = DisplayStyle.Flex;
        _consumerLabel.style.display = DisplayStyle.Flex;
        _networkLabel.style.display = DisplayStyle.None;
        _generatorLabel.text = $"{_loc.T("Electricity.Panel.InputAndMax")}: {deliveredPower} / {demand} kW ({efficiency}%)";
        _consumerLabel.text = $"{_loc.T("Electricity.Panel.GenerationAndConsumption")}: {networkSupply} / {networkConsumption} kW";
        SetVisible(true);
    }

    private void SetVisible(bool visible)
    {
        if (_root != null)
        {
            _root.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}
