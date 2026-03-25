using Timberborn.BaseComponentSystem;
using Timberborn.BlockSystem;
using Timberborn.EntitySystem;
using Timberborn.MechanicalSystem;
using UnityEngine;

namespace FulgurFangs.Code.Electricity;

public sealed class MechanicalToElectricConverterComponent : BaseComponent, IPostInitializableEntity, IDeletableEntity, IFinishedStateListener
{
    private readonly ElectricityService _electricityService;
    private BlockObject? _blockObject;
    private bool _isFinished;
    private MechanicalNode? _mechanicalNode;
    private int _maxOutput;

    public MechanicalToElectricConverterComponent(
        ElectricityService electricityService)
    {
        _electricityService = electricityService;
    }

    public bool IsReady => Enabled && _isFinished;

    public Vector3 WorldPosition => GetWorldPosition();

    public int MaxOutput => _maxOutput;

    public ElectricityPoleComponent? NetworkNode { get; private set; }

    public int InstanceId => Transform.GetInstanceID();

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

    public void PostInitializeEntity()
    {
        _blockObject = GetComponent<BlockObject>() ?? Transform.GetComponentInParent<BlockObject>();
        _isFinished = _blockObject != null && _blockObject.IsFinished;
        _mechanicalNode = GetComponent<MechanicalNode>() ?? GetComponentInChildren<MechanicalNode>(true);
        NetworkNode = GetComponent<ElectricityPoleComponent>();
        _electricityService.RegisterConverter(this);
    }

    public void DeleteEntity()
    {
        _electricityService.UnregisterConverter(this);
    }

    public void SetMaxOutput(int maxOutput)
    {
        _maxOutput = maxOutput;
    }

    public void OnEnterFinishedState()
    {
        _isFinished = true;
        _electricityService.RefreshStateWithoutAdvancingTime();
    }

    public void OnExitFinishedState()
    {
        _isFinished = false;
        _electricityService.RefreshStateWithoutAdvancingTime();
    }

    private Vector3 GetWorldPosition()
    {
        if (!ReferenceEquals(_blockObject, null))
        {
            Vector3Int coordinates = _blockObject.CoordinatesAtBaseZ;
            return new Vector3(coordinates.x + 0.5f, coordinates.z, coordinates.y + 0.5f);
        }

        return Transform != null ? Transform.position : Vector3.zero;
    }
}
