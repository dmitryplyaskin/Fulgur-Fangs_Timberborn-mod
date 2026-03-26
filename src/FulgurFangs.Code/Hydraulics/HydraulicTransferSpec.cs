using System.Collections.Immutable;
using Timberborn.BlueprintSystem;
using UnityEngine;

namespace FulgurFangs.Code.Hydraulics;

public record HydraulicTransferSpec : ComponentSpec
{
    [Serialize]
    public ImmutableArray<Vector3Int> IntakeCoordinates { get; init; }

    [Serialize]
    public ImmutableArray<Vector3Int> OutputCoordinates { get; init; }

    [Serialize]
    public float MaxWaterPerSecond { get; init; }

    [Serialize]
    public float DefaultThrottle { get; init; } = 1f;

    [Serialize]
    public float ThrottleStep { get; init; } = 0.05f;

    [Serialize]
    public int IntakeMaxDepth { get; init; } = 4;

    [Serialize]
    public int OutputMaxDrop { get; init; } = 4;

    [Serialize]
    public bool MoveCleanWater { get; init; } = true;

    [Serialize]
    public bool MoveContaminatedWater { get; init; } = true;
}
