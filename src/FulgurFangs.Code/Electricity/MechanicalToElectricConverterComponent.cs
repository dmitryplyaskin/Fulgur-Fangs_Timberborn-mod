using Timberborn.BaseComponentSystem;
using Timberborn.EntitySystem;
using Timberborn.MechanicalSystem;
using UnityEngine;

namespace FulgurFangs.Code.Electricity;

public sealed class MechanicalToElectricConverterComponent : BaseComponent, IPostInitializableEntity, IDeletableEntity
{
    private readonly ElectricityService _electricityService;
    private MechanicalNode? _mechanicalNode;
    private int _maxOutput;

    public MechanicalToElectricConverterComponent(
        ElectricityService electricityService)
    {
        _electricityService = electricityService;
    }

    public bool IsReady => Enabled;

    public Vector3 WorldPosition => Transform.position;

    public int MaxOutput => _maxOutput;

    public MechanicalGraph? MechanicalGraph
    {
        get
        {
            if (ReferenceEquals(_mechanicalNode, null))
            {
                return null;
            }

            try
            {
                MechanicalGraph? graph = _mechanicalNode.Graph;
                return ReferenceEquals(graph, null) || !graph.Valid ? null : graph;
            }
            catch (NullReferenceException)
            {
                return null;
            }
        }
    }

    public int PreferredMechanicalInput
    {
        get
        {
            if (ReferenceEquals(_mechanicalNode, null))
            {
                return 0;
            }

            try
            {
                int directInput = Mathf.Clamp(_mechanicalNode.Actuals.PowerInput, 0, _maxOutput);
                if (directInput > 0)
                {
                    return directInput;
                }

                MechanicalGraph? graph = MechanicalGraph;
                if (graph == null)
                {
                    return 0;
                }

                return Mathf.Clamp(graph.PowerSupply, 0, _maxOutput);
            }
            catch (NullReferenceException)
            {
                return 0;
            }
        }
    }

    public string DebugMechanicalState
    {
        get
        {
            if (ReferenceEquals(_mechanicalNode, null))
            {
                return "node=null";
            }

            try
            {
                MechanicalGraph? graph = MechanicalGraph;
                if (graph == null)
                {
                    return
                        $"graph=null powered={_mechanicalNode.Powered} active={_mechanicalNode.Active} activePowered={_mechanicalNode.ActiveAndPowered} " +
                        $"actualIn={_mechanicalNode.Actuals.PowerInput} actualOut={_mechanicalNode.Actuals.PowerOutput}";
                }

                return
                    $"graphValid={graph.Valid} powered={_mechanicalNode.Powered} active={_mechanicalNode.Active} activePowered={_mechanicalNode.ActiveAndPowered} " +
                    $"actualIn={_mechanicalNode.Actuals.PowerInput} actualOut={_mechanicalNode.Actuals.PowerOutput} " +
                    $"graphSupply={graph.PowerSupply} graphDemand={graph.PowerDemand} graphSurplus={graph.PowerSurplus} graphPowered={graph.Powered}";
            }
            catch (NullReferenceException)
            {
                return "state=NullReferenceException";
            }
        }
    }

    public void PostInitializeEntity()
    {
        _mechanicalNode = GetComponent<MechanicalNode>() ?? GetComponentInChildren<MechanicalNode>(true);
        _electricityService.RegisterConverter(this);
        Debug.Log($"[FulgurFangs] Converter registered at {WorldPosition} maxOutput={_maxOutput} nodeFound={!ReferenceEquals(_mechanicalNode, null)}");
    }

    public void DeleteEntity()
    {
        _electricityService.UnregisterConverter(this);
    }

    public void SetMaxOutput(int maxOutput)
    {
        _maxOutput = maxOutput;
    }
}
