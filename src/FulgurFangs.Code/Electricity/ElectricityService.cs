using System.Collections.Generic;
using System.Linq;
using Timberborn.MechanicalSystem;
using Timberborn.SingletonSystem;
using UnityEngine;

namespace FulgurFangs.Code.Electricity;

public sealed class ElectricityService : IUpdatableSingleton
{
    private readonly HashSet<ElectricityPoleComponent> _poles = new();
    private readonly HashSet<MechanicalToElectricConverterComponent> _converters = new();
    private readonly HashSet<ElectricityConsumerComponent> _consumers = new();
    private ElectricityNetworkState _lastLoggedState;
    private int _lastLoggedPoleCount = -1;
    private int _lastLoggedConverterCount = -1;
    private int _lastLoggedConsumerCount = -1;
    private int _lastLoggedNetworkConverterCount = -1;
    private int _lastLoggedNetworkConsumerCount = -1;

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
        Dictionary<MechanicalGraph, int> initialGraphPower = new();
        List<string> converterDebugStates = new();

        foreach (MechanicalToElectricConverterComponent converter in networkConverters)
        {
            MechanicalGraph? graph = converter.MechanicalGraph;
            converterDebugStates.Add(converter.DebugMechanicalState);
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
            initialGraphPower.Add(graph, graphPowerBudget);
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

        if (_lastLoggedPoleCount != poles.Length ||
            _lastLoggedConverterCount != converters.Length ||
            _lastLoggedConsumerCount != consumers.Length ||
            _lastLoggedNetworkConverterCount != networkConverters.Length ||
            _lastLoggedNetworkConsumerCount != networkConsumers.Length ||
            !_lastLoggedState.Equals(CurrentState))
        {
            Debug.Log(
                $"[FulgurFangs] Electricity state poles={poles.Length} converters={converters.Length} consumers={consumers.Length} " +
                $"inRangeConverters={networkConverters.Length} inRangeConsumers={networkConsumers.Length} " +
                $"mechanicalSurplus={initialGraphPower.Values.Sum()} supply={CurrentState.Supply} demand={CurrentState.Demand} consumption={CurrentState.Consumption} " +
                $"converterStates=[{string.Join(" | ", converterDebugStates)}]");

            _lastLoggedPoleCount = poles.Length;
            _lastLoggedConverterCount = converters.Length;
            _lastLoggedConsumerCount = consumers.Length;
            _lastLoggedNetworkConverterCount = networkConverters.Length;
            _lastLoggedNetworkConsumerCount = networkConsumers.Length;
            _lastLoggedState = CurrentState;
        }
    }
}
