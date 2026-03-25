using System.Collections.Generic;
using System.Linq;
using Timberborn.BaseComponentSystem;
using Timberborn.MechanicalSystem;
using Timberborn.SingletonSystem;
using Timberborn.TimeSystem;
using Timberborn.TickSystem;
using Timberborn.ZiplineSystem;
using UnityEngine;

namespace FulgurFangs.Code.Electricity;

public sealed class ElectricityService : ITickableSingleton
{
    private readonly IDayNightCycle _dayNightCycle;
    private readonly HashSet<ElectricityPoleComponent> _nodes = new();
    private readonly HashSet<MechanicalToElectricConverterComponent> _converters = new();
    private readonly HashSet<ElectricityConsumerComponent> _consumers = new();
    private readonly HashSet<ElectricityAccumulatorComponent> _accumulators = new();
    private readonly Dictionary<int, ElectricitySubnetworkSnapshot> _nodeSnapshots = new();
    private readonly Dictionary<int, ElectricitySubnetworkSnapshot> _consumerSnapshots = new();
    private readonly Dictionary<int, IReadOnlyCollection<ElectricityPoleComponent>> _consumerNetworkNodes = new();
    private readonly Dictionary<int, ElectricitySubnetworkSnapshot> _accumulatorSnapshots = new();

    public static ElectricityService? Instance { get; private set; }

    public ElectricityNetworkState CurrentState { get; private set; }

    public ElectricityService(IDayNightCycle dayNightCycle)
    {
        _dayNightCycle = dayNightCycle;
        Instance = this;
    }

    public void RegisterPole(ElectricityPoleComponent pole)
    {
        _nodes.Add(pole);
        RefreshStateWithoutAdvancingTime();
    }

    public void UnregisterPole(ElectricityPoleComponent pole)
    {
        _nodes.Remove(pole);
        RefreshStateWithoutAdvancingTime();
    }

    public void RegisterConverter(MechanicalToElectricConverterComponent converter)
    {
        _converters.Add(converter);
        RefreshStateWithoutAdvancingTime();
    }

    public void UnregisterConverter(MechanicalToElectricConverterComponent converter)
    {
        _converters.Remove(converter);
        RefreshStateWithoutAdvancingTime();
    }

    public void RegisterConsumer(ElectricityConsumerComponent consumer)
    {
        _consumers.Add(consumer);
        RefreshStateWithoutAdvancingTime();
    }

    public void UnregisterConsumer(ElectricityConsumerComponent consumer)
    {
        _consumers.Remove(consumer);
        RefreshStateWithoutAdvancingTime();
    }

    public void RegisterAccumulator(ElectricityAccumulatorComponent accumulator)
    {
        _accumulators.Add(accumulator);
        RefreshStateWithoutAdvancingTime();
    }

    public void UnregisterAccumulator(ElectricityAccumulatorComponent accumulator)
    {
        _accumulators.Remove(accumulator);
        RefreshStateWithoutAdvancingTime();
    }

    public ElectricitySubnetworkSnapshot? GetNodeSnapshot(ElectricityPoleComponent? node)
    {
        if (node == null)
        {
            return null;
        }

        return _nodeSnapshots.TryGetValue(node.InstanceId, out ElectricitySubnetworkSnapshot snapshot)
            ? snapshot
            : null;
    }

    public ElectricitySubnetworkSnapshot? GetAccumulatorSnapshot(ElectricityAccumulatorComponent? accumulator)
    {
        if (accumulator == null)
        {
            return null;
        }

        return _accumulatorSnapshots.TryGetValue(accumulator.InstanceId, out ElectricitySubnetworkSnapshot snapshot)
            ? snapshot
            : null;
    }

    public ElectricitySubnetworkSnapshot? GetConsumerSnapshot(ElectricityConsumerComponent? consumer)
    {
        if (consumer == null)
        {
            return null;
        }

        return _consumerSnapshots.TryGetValue(consumer.InstanceId, out ElectricitySubnetworkSnapshot snapshot)
            ? snapshot
            : null;
    }

    public IReadOnlyCollection<ElectricityPoleComponent> GetConsumerNetworkNodes(ElectricityConsumerComponent? consumer)
    {
        if (consumer == null || !consumer.IsReady)
        {
            return System.Array.Empty<ElectricityPoleComponent>();
        }

        if (_consumerNetworkNodes.TryGetValue(consumer.InstanceId, out IReadOnlyCollection<ElectricityPoleComponent>? nodes))
        {
            return nodes;
        }

        ElectricityPoleComponent[] activeNodes = _nodes
            .Where(static node => node.IsReady)
            .OrderBy(static node => node.InstanceId)
            .ToArray();
        if (activeNodes.Length == 0)
        {
            return System.Array.Empty<ElectricityPoleComponent>();
        }

        foreach (ElectricitySubnetwork subnetwork in BuildSubnetworks(activeNodes))
        {
            if (subnetwork.Distributors.Any(distributor => distributor.InRangeOf(consumer.WorldPosition)))
            {
                return subnetwork.Nodes;
            }
        }

        return System.Array.Empty<ElectricityPoleComponent>();
    }

    public IEnumerable<BaseComponent> GetElectricObjectsInRange(ElectricityPoleComponent node)
    {
        if (node == null || !node.HasDistributionRange)
        {
            yield break;
        }

        foreach (MechanicalToElectricConverterComponent converter in _converters)
        {
            if (converter != null && converter.GameObject && converter.IsReady && node.InRangeOf(converter.WorldPosition))
            {
                yield return converter;
            }
        }

        foreach (ElectricityAccumulatorComponent accumulator in _accumulators)
        {
            if (accumulator != null && accumulator.GameObject && accumulator.IsReady && node.InRangeOf(accumulator.WorldPosition))
            {
                yield return accumulator;
            }
        }

        foreach (ElectricityConsumerComponent consumer in _consumers)
        {
            if (consumer != null && consumer.GameObject && consumer.IsReady && node.InRangeOf(consumer.WorldPosition))
            {
                yield return consumer;
            }
        }
    }

    public IReadOnlyCollection<ElectricityPoleComponent> GetConnectedPoles(ElectricityPoleComponent rootPole)
    {
        if (rootPole == null)
        {
            return System.Array.Empty<ElectricityPoleComponent>();
        }

        Dictionary<ZiplineTower, ElectricityPoleComponent> nodesByTower = _nodes
            .Where(static node => node != null && node.GameObject && node.IsReady && node.Tower != null)
            .GroupBy(static node => node.Tower!)
            .ToDictionary(static group => group.Key, static group => group.First());

        HashSet<ElectricityPoleComponent> visitedNodes = new();
        Queue<ElectricityPoleComponent> queue = new();

        visitedNodes.Add(rootPole);
        queue.Enqueue(rootPole);

        while (queue.Count > 0)
        {
            ElectricityPoleComponent node = queue.Dequeue();
            foreach (ZiplineTower targetTower in node.GetConnectionTargetsSafe())
            {
                if (!nodesByTower.TryGetValue(targetTower, out ElectricityPoleComponent? connectedNode))
                {
                    continue;
                }

                if (visitedNodes.Add(connectedNode))
                {
                    queue.Enqueue(connectedNode);
                }
            }
        }

        return visitedNodes.ToArray();
    }

    public void Tick()
    {
        RecalculateState(advanceAccumulators: true);
    }

    public void RefreshStateWithoutAdvancingTime()
    {
        RecalculateState(advanceAccumulators: false);
    }

    private void RecalculateState(bool advanceAccumulators)
    {
        CleanupRegistries();
        _nodeSnapshots.Clear();
        _consumerSnapshots.Clear();
        _consumerNetworkNodes.Clear();
        _accumulatorSnapshots.Clear();

        ElectricityConsumerComponent[] consumers = _consumers
            .Where(static consumer => consumer.IsReady)
            .ToArray();
        foreach (ElectricityConsumerComponent consumer in consumers)
        {
            consumer.SetSupplyFraction(0f);
        }

        float deltaHours = Mathf.Max(0f, _dayNightCycle.FixedDeltaTimeInHours);
        ElectricityAccumulatorComponent[] readyAccumulators = _accumulators
            .Where(static accumulator => accumulator.IsReady)
            .OrderBy(static accumulator => accumulator.InstanceId)
            .ToArray();
        if (advanceAccumulators)
        {
            foreach (ElectricityAccumulatorComponent accumulator in readyAccumulators)
            {
                accumulator.ApplyLeakage(deltaHours);
            }
        }

        Dictionary<ElectricityAccumulatorComponent, float> simulatedAccumulatorCharge = readyAccumulators
            .ToDictionary(static accumulator => accumulator, static accumulator => accumulator.CurrentCharge);

        ElectricityPoleComponent[] nodes = _nodes
            .Where(static node => node.IsReady)
            .OrderBy(static node => node.InstanceId)
            .ToArray();

        if (nodes.Length == 0)
        {
            CurrentState = default;
            return;
        }

        List<ElectricitySubnetwork> subnetworks = BuildSubnetworks(nodes);
        HashSet<ElectricityConsumerComponent> assignedConsumers = new();
        HashSet<ElectricityAccumulatorComponent> assignedAccumulators = new();

        float totalSupply = 0f;
        float totalDemand = 0f;
        float totalConsumption = 0f;

        foreach (ElectricitySubnetwork subnetwork in subnetworks)
        {
            ElectricityConsumerComponent[] networkConsumers = subnetwork.Distributors.Count == 0
                ? System.Array.Empty<ElectricityConsumerComponent>()
                : consumers
                    .Where(consumer => !assignedConsumers.Contains(consumer) && subnetwork.Distributors.Any(distributor => distributor.InRangeOf(consumer.WorldPosition)))
                    .OrderBy(static consumer => consumer.InstanceId)
                    .ToArray();
            ElectricityAccumulatorComponent[] networkAccumulators = subnetwork.Distributors.Count == 0
                ? System.Array.Empty<ElectricityAccumulatorComponent>()
                : readyAccumulators
                    .Where(accumulator => !assignedAccumulators.Contains(accumulator) && subnetwork.Distributors.Any(distributor => distributor.InRangeOf(accumulator.WorldPosition)))
                    .OrderBy(static accumulator => accumulator.InstanceId)
                    .ToArray();

            foreach (ElectricityConsumerComponent consumer in networkConsumers)
            {
                assignedConsumers.Add(consumer);
            }

            foreach (ElectricityAccumulatorComponent accumulator in networkAccumulators)
            {
                assignedAccumulators.Add(accumulator);
            }

            float generation = ComputeGeneration(subnetwork.Converters);
            float consumerDemand = networkConsumers.Sum(static consumer => Mathf.Max(0, consumer.Demand));
            float transmissionLoss = networkConsumers.Length > 0
                ? subnetwork.Nodes.Sum(static node => node.TransmissionLoss)
                : 0f;
            float networkLoad = consumerDemand + transmissionLoss;

            float discharged = 0f;
            if (networkLoad > generation)
            {
                discharged = DischargeAccumulators(networkAccumulators, simulatedAccumulatorCharge, networkLoad - generation, deltaHours);
            }

            float availablePower = generation + discharged;
            float lossesConsumed = Mathf.Min(availablePower, transmissionLoss);
            float consumerPower = Mathf.Min(Mathf.Max(0f, availablePower - transmissionLoss), consumerDemand);
            float supplyFraction = consumerDemand > 0f ? Mathf.Clamp01(consumerPower / consumerDemand) : 0f;

            foreach (ElectricityConsumerComponent consumer in networkConsumers)
            {
                consumer.SetSupplyFraction(supplyFraction);
            }

            float charged = 0f;
            if (generation > networkLoad)
            {
                charged = ChargeAccumulators(networkAccumulators, simulatedAccumulatorCharge, generation - networkLoad, deltaHours);
            }

            int storedCharge = Mathf.RoundToInt(networkAccumulators.Sum(accumulator => simulatedAccumulatorCharge[accumulator]));
            int storageCapacity = Mathf.RoundToInt(networkAccumulators.Sum(static accumulator => accumulator.Capacity));
            ElectricitySubnetworkSnapshot snapshot = new(
                Mathf.RoundToInt(availablePower),
                Mathf.RoundToInt(networkLoad + charged),
                Mathf.RoundToInt(lossesConsumed + consumerPower + charged),
                storedCharge,
                storageCapacity);

            foreach (ElectricityPoleComponent node in subnetwork.Nodes)
            {
                _nodeSnapshots[node.InstanceId] = snapshot;
            }

            foreach (ElectricityConsumerComponent consumer in networkConsumers)
            {
                _consumerSnapshots[consumer.InstanceId] = snapshot;
                _consumerNetworkNodes[consumer.InstanceId] = subnetwork.Nodes;
            }

            foreach (ElectricityAccumulatorComponent accumulator in networkAccumulators)
            {
                _accumulatorSnapshots[accumulator.InstanceId] = snapshot;
            }

            totalSupply += generation + discharged;
            totalDemand += networkLoad + charged;
            totalConsumption += lossesConsumed + consumerPower + charged;
        }

        CurrentState = new ElectricityNetworkState(
            Mathf.RoundToInt(totalSupply),
            Mathf.RoundToInt(totalDemand),
            Mathf.RoundToInt(totalConsumption));

        if (!advanceAccumulators)
        {
            return;
        }

        foreach (KeyValuePair<ElectricityAccumulatorComponent, float> accumulatorState in simulatedAccumulatorCharge)
        {
            accumulatorState.Key.SetCurrentCharge(accumulatorState.Value);
        }
    }

    private void CleanupRegistries()
    {
        _nodes.RemoveWhere(static node => node == null || !node.GameObject);
        _converters.RemoveWhere(static converter => converter == null || !converter.GameObject);
        _consumers.RemoveWhere(static consumer => consumer == null || !consumer.GameObject);
        _accumulators.RemoveWhere(static accumulator => accumulator == null || !accumulator.GameObject);
    }

    private static float ComputeGeneration(IReadOnlyCollection<MechanicalToElectricConverterComponent> converters)
    {
        Dictionary<MechanicalGraph, int> availableGraphPower = new();
        float totalSupply = 0f;

        foreach (MechanicalToElectricConverterComponent converter in converters)
        {
            MechanicalGraph? graph = converter.MechanicalGraph;
            if (graph == null)
            {
                totalSupply += converter.PreferredMechanicalInput;
                continue;
            }

            if (!availableGraphPower.ContainsKey(graph))
            {
                availableGraphPower.Add(graph, Mathf.Max(0, graph.PowerSupply));
            }
        }

        foreach (MechanicalToElectricConverterComponent converter in converters)
        {
            MechanicalGraph? graph = converter.MechanicalGraph;
            if (graph == null)
            {
                continue;
            }

            int remainingGraphPower = availableGraphPower[graph];
            int convertedPower = Mathf.Min(remainingGraphPower, converter.PreferredMechanicalInput);
            availableGraphPower[graph] = remainingGraphPower - convertedPower;
            totalSupply += convertedPower;
        }

        return totalSupply;
    }

    private static float DischargeAccumulators(
        IReadOnlyList<ElectricityAccumulatorComponent> accumulators,
        IDictionary<ElectricityAccumulatorComponent, float> simulatedAccumulatorCharge,
        float requestedPower,
        float deltaHours)
    {
        float remainingPower = Mathf.Max(0f, requestedPower);
        if (remainingPower <= 0f || deltaHours <= 0f)
        {
            return 0f;
        }

        float dischargedPower = 0f;
        for (int index = 0; index < accumulators.Count; index++)
        {
            if (remainingPower <= 0f)
            {
                break;
            }

            ElectricityAccumulatorComponent accumulator = accumulators[index];
            float currentCharge = simulatedAccumulatorCharge[accumulator];
            float availableDischargePower = Mathf.Min(accumulator.MaxDischargePerHour, currentCharge / deltaHours);
            float requestedShare = remainingPower / (accumulators.Count - index);
            float releasedPower = Mathf.Min(Mathf.Max(0f, requestedShare), availableDischargePower);
            simulatedAccumulatorCharge[accumulator] = Mathf.Max(0f, currentCharge - releasedPower * deltaHours);
            dischargedPower += releasedPower;
            remainingPower -= releasedPower;
        }

        return dischargedPower;
    }

    private static float ChargeAccumulators(
        IReadOnlyList<ElectricityAccumulatorComponent> accumulators,
        IDictionary<ElectricityAccumulatorComponent, float> simulatedAccumulatorCharge,
        float availablePower,
        float deltaHours)
    {
        float remainingPower = Mathf.Max(0f, availablePower);
        if (remainingPower <= 0f || deltaHours <= 0f)
        {
            return 0f;
        }

        float chargedPower = 0f;
        for (int index = 0; index < accumulators.Count; index++)
        {
            if (remainingPower <= 0f)
            {
                break;
            }

            ElectricityAccumulatorComponent accumulator = accumulators[index];
            float currentCharge = simulatedAccumulatorCharge[accumulator];
            float availableChargePower = Mathf.Max(0f, (accumulator.Capacity - currentCharge) / deltaHours);
            float requestedShare = remainingPower / (accumulators.Count - index);
            float acceptedPower = Mathf.Min(Mathf.Max(0f, requestedShare), availableChargePower);
            simulatedAccumulatorCharge[accumulator] = Mathf.Min(accumulator.Capacity, currentCharge + acceptedPower * deltaHours);
            chargedPower += acceptedPower;
            remainingPower -= acceptedPower;
        }

        return chargedPower;
    }

    private List<ElectricitySubnetwork> BuildSubnetworks(IReadOnlyList<ElectricityPoleComponent> nodes)
    {
        Dictionary<ZiplineTower, ElectricityPoleComponent> nodesByTower = nodes
            .Where(static node => node.IsReady && node.Tower != null)
            .GroupBy(static node => node.Tower!)
            .ToDictionary(static group => group.Key, static group => group.First());

        HashSet<ElectricityPoleComponent> visitedNodes = new();
        List<ElectricitySubnetwork> subnetworks = new();

        foreach (ElectricityPoleComponent rootNode in nodes)
        {
            if (!visitedNodes.Add(rootNode))
            {
                continue;
            }

            List<ElectricityPoleComponent> subnetworkNodes = new();
            Queue<ElectricityPoleComponent> queue = new();
            queue.Enqueue(rootNode);

            while (queue.Count > 0)
            {
                ElectricityPoleComponent currentNode = queue.Dequeue();
                subnetworkNodes.Add(currentNode);

                foreach (ZiplineTower targetTower in currentNode.GetConnectionTargetsSafe())
                {
                    if (!nodesByTower.TryGetValue(targetTower, out ElectricityPoleComponent? connectedNode))
                    {
                        continue;
                    }

                    if (visitedNodes.Add(connectedNode))
                    {
                        queue.Enqueue(connectedNode);
                    }
                }
            }

            HashSet<ElectricityPoleComponent> nodeSet = subnetworkNodes.ToHashSet();
            List<ElectricityPoleComponent> distributors = subnetworkNodes
                .Where(static node => node.IsReady && node.HasDistributionRange)
                .OrderBy(static node => node.InstanceId)
                .ToList();
            List<MechanicalToElectricConverterComponent> converters = _converters
                .Where(converter => converter.IsReady && converter.NetworkNode != null && nodeSet.Contains(converter.NetworkNode))
                .OrderBy(static converter => converter.InstanceId)
                .ToList();
            subnetworks.Add(new ElectricitySubnetwork(subnetworkNodes, distributors, converters));
        }

        return subnetworks;
    }

    private sealed class ElectricitySubnetwork
    {
        public ElectricitySubnetwork(
            IReadOnlyList<ElectricityPoleComponent> nodes,
            IReadOnlyList<ElectricityPoleComponent> distributors,
            IReadOnlyList<MechanicalToElectricConverterComponent> converters)
        {
            Nodes = nodes;
            Distributors = distributors;
            Converters = converters;
        }

        public IReadOnlyList<ElectricityPoleComponent> Nodes { get; }

        public IReadOnlyList<ElectricityPoleComponent> Distributors { get; }

        public IReadOnlyList<MechanicalToElectricConverterComponent> Converters { get; }
    }
}
