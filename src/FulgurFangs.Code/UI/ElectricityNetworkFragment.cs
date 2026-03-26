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
    private HydroelectricGeneratorComponent? _hydroelectricGenerator;

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
        _hydroelectricGenerator = entity.GetComponent<HydroelectricGeneratorComponent>();
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

        if (_hydroelectricGenerator != null)
        {
            RenderHydroelectricGenerator(service, _hydroelectricGenerator);
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
        _generatorLabel.style.display = DisplayStyle.Flex;
        _consumerLabel.style.display = DisplayStyle.Flex;
        _networkLabel.style.display = DisplayStyle.None;
        _generatorLabel.text = $"{_loc.T("Electricity.Panel.GenerationAndConsumption")}: {value.Supply} / {value.Consumption} kW";
        _consumerLabel.text = $"{_loc.T("Electricity.Panel.NetworkCharge")}: {value.StoredCharge} / {value.StorageCapacity} kWh";
        SetVisible(true);
    }

    public void ClearFragment()
    {
        _node = null;
        _accumulator = null;
        _consumer = null;
        _hydroelectricGenerator = null;
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
        int storedCharge = snapshot?.StoredCharge ?? 0;
        int storageCapacity = snapshot?.StorageCapacity ?? 0;

        _generatorLabel.style.display = DisplayStyle.Flex;
        _consumerLabel.style.display = DisplayStyle.Flex;
        _networkLabel.style.display = DisplayStyle.Flex;
        _generatorLabel.text = $"{_loc.T("Electricity.Panel.InputAndMax")}: {deliveredPower} / {demand} kW ({efficiency}%)";
        _consumerLabel.text = $"{_loc.T("Electricity.Panel.GenerationAndConsumption")}: {networkSupply} / {networkConsumption} kW";
        _networkLabel.text = $"{_loc.T("Electricity.Panel.NetworkCharge")}: {storedCharge} / {storageCapacity} kWh";
        SetVisible(true);
    }

    private void RenderHydroelectricGenerator(ElectricityService? service, HydroelectricGeneratorComponent hydroelectricGenerator)
    {
        if (_generatorLabel == null || _consumerLabel == null || _networkLabel == null)
        {
            SetVisible(false);
            return;
        }

        int currentOutput = Mathf.RoundToInt(hydroelectricGenerator.CurrentOutput);
        int maxOutput = Mathf.Max(0, hydroelectricGenerator.MaxOutput);
        float currentFlow = hydroelectricGenerator.CurrentFlow;
        ElectricitySubnetworkSnapshot? snapshot = service?.GetHydroelectricSnapshot(hydroelectricGenerator);

        _generatorLabel.style.display = DisplayStyle.Flex;
        _consumerLabel.style.display = DisplayStyle.Flex;
        _networkLabel.style.display = DisplayStyle.Flex;
        _generatorLabel.text = $"{_loc.T("Electricity.Panel.Output")}: {currentOutput} / {maxOutput} kW";
        _consumerLabel.text = $"{_loc.T("Electricity.Panel.Flow")}: {currentFlow:0.00}";
        _networkLabel.text = snapshot.HasValue
            ? $"{_loc.T("Electricity.Panel.GenerationAndConsumption")}: {snapshot.Value.Supply} / {snapshot.Value.Consumption} kW"
            : _loc.T("Electricity.Panel.NoDistributorCoverage");
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
