using System.Collections.Generic;
using System.Linq;
using Timberborn.BaseComponentSystem;
using Timberborn.MechanicalSystem;
using Timberborn.SingletonSystem;
using Timberborn.ZiplineSystem;
using UnityEngine;

namespace FulgurFangs.Code.Electricity;

public sealed class ElectricityService : IUpdatableSingleton
{
    private readonly HashSet<ElectricityPoleComponent> _poles = new();
    private readonly HashSet<MechanicalToElectricConverterComponent> _converters = new();
    private readonly HashSet<ElectricityConsumerComponent> _consumers = new();

    public static ElectricityService? Instance { get; private set; }

    public ElectricityNetworkState CurrentState { get; private set; }

    public ElectricityService()
    {
        Instance = this;
    }

    public void RegisterPole(ElectricityPoleComponent pole) => _poles.Add(pole);

    public void UnregisterPole(ElectricityPoleComponent pole) => _poles.Remove(pole);

    public void RegisterConverter(MechanicalToElectricConverterComponent converter) => _converters.Add(converter);

    public void UnregisterConverter(MechanicalToElectricConverterComponent converter) => _converters.Remove(converter);

    public void RegisterConsumer(ElectricityConsumerComponent consumer) => _consumers.Add(consumer);

    public void UnregisterConsumer(ElectricityConsumerComponent consumer) => _consumers.Remove(consumer);

    public IEnumerable<BaseComponent> GetElectricObjectsInRange(ElectricityPoleComponent pole)
    {
        foreach (MechanicalToElectricConverterComponent converter in _converters)
        {
            if (converter != null && converter.GameObject && converter.IsReady && pole.InRangeOf(converter.WorldPosition))
            {
                yield return converter;
            }
        }

        foreach (ElectricityConsumerComponent consumer in _consumers)
        {
            if (consumer != null && consumer.GameObject && consumer.IsReady && pole.InRangeOf(consumer.WorldPosition))
            {
                yield return consumer;
            }
        }
    }

    public IReadOnlyCollection<ElectricityPoleComponent> GetConnectedPoles(ElectricityPoleComponent rootPole)
    {
        if (rootPole == null)
        {
            return Array.Empty<ElectricityPoleComponent>();
        }

        Dictionary<ZiplineTower, ElectricityPoleComponent> polesByTower = _poles
            .Where(static pole => pole != null && pole.GameObject && pole.IsReady && pole.Tower != null)
            .GroupBy(static pole => pole.Tower!)
            .ToDictionary(static group => group.Key, static group => group.First());

        HashSet<ElectricityPoleComponent> visitedPoles = new();
        Queue<ElectricityPoleComponent> queue = new();

        visitedPoles.Add(rootPole);
        queue.Enqueue(rootPole);

        while (queue.Count > 0)
        {
            ElectricityPoleComponent pole = queue.Dequeue();
            foreach (ZiplineTower targetTower in pole.GetConnectionTargetsSafe())
            {
                if (!polesByTower.TryGetValue(targetTower, out ElectricityPoleComponent? connectedPole))
                {
                    continue;
                }

                if (visitedPoles.Add(connectedPole))
                {
                    queue.Enqueue(connectedPole);
                }
            }
        }

        return visitedPoles.ToArray();
    }

    public void UpdateSingleton()
    {
        _poles.RemoveWhere(static pole => pole == null || !pole.GameObject || !pole.IsReady);
        _converters.RemoveWhere(static converter => converter == null || !converter.GameObject || !converter.IsReady);
        _consumers.RemoveWhere(static consumer => consumer == null || !consumer.GameObject || !consumer.IsReady);

        ElectricityPoleComponent[] poles = _poles.ToArray();
        MechanicalToElectricConverterComponent[] converters = _converters.ToArray();
        ElectricityConsumerComponent[] consumers = _consumers.ToArray();

        if (poles.Length == 0)
        {
            foreach (ElectricityConsumerComponent consumer in consumers)
            {
                consumer.SetPowered(false);
            }

            CurrentState = default;
            return;
        }

        MechanicalToElectricConverterComponent[] networkConverters = converters
            .Where(converter => poles.Any(pole => pole.InRangeOf(converter.WorldPosition)))
            .ToArray();

        ElectricityConsumerComponent[] networkConsumers = consumers
            .Where(consumer => poles.Any(pole => pole.InRangeOf(consumer.WorldPosition)))
            .ToArray();

        Dictionary<MechanicalGraph, int> availableGraphPower = new();

        foreach (MechanicalToElectricConverterComponent converter in networkConverters)
        {
            MechanicalGraph? graph = converter.MechanicalGraph;
            if (graph == null)
            {
                continue;
            }

            if (availableGraphPower.ContainsKey(graph))
            {
                continue;
            }

            int graphPowerBudget = Mathf.Max(0, graph.PowerSupply);
            availableGraphPower.Add(graph, graphPowerBudget);
        }

        int totalSupply = 0;
        foreach (MechanicalToElectricConverterComponent converter in networkConverters)
        {
            MechanicalGraph? graph = converter.MechanicalGraph;
            if (graph == null)
            {
                totalSupply += converter.PreferredMechanicalInput;
                continue;
            }

            int remainingGraphPower = availableGraphPower[graph];
            int preferredInput = converter.PreferredMechanicalInput;
            int convertedPower = Mathf.Min(remainingGraphPower, preferredInput);
            availableGraphPower[graph] = remainingGraphPower - convertedPower;
            totalSupply += convertedPower;
        }

        int totalDemand = networkConsumers.Sum(static consumer => consumer.Demand);
        int totalConsumption = Mathf.Min(totalSupply, totalDemand);
        bool powered = totalSupply >= totalDemand;

        foreach (ElectricityConsumerComponent consumer in networkConsumers)
        {
            consumer.SetPowered(powered);
        }

        foreach (ElectricityConsumerComponent consumer in consumers.Except(networkConsumers))
        {
            consumer.SetPowered(false);
        }

        CurrentState = new ElectricityNetworkState(totalSupply, totalDemand, totalConsumption);
    }
}
