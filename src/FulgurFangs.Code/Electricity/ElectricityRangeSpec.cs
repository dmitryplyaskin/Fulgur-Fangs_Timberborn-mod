using Timberborn.BlueprintSystem;
using UnityEngine;

namespace FulgurFangs.Code.Electricity;

public record ElectricityRangeSpec : ComponentSpec
{
    [Serialize]
    public int Range { get; init; }

    [Serialize]
    public int TransmissionLoss { get; init; }

    [Serialize]
    public Vector3 CableAnchorPoint { get; init; }

    [Serialize]
    public int MaxConnections { get; init; }

    [Serialize]
    public float MaxDistance { get; init; }
}
