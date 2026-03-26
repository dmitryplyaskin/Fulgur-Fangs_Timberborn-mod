using System.Collections.Immutable;
using Timberborn.BlueprintSystem;
using UnityEngine;

namespace FulgurFangs.Code.Hydraulics;

public record MultiCellValveSpec : ComponentSpec
{
    [Serialize]
    public ImmutableArray<Vector3Int> FlowCoordinates { get; init; }

    [Serialize]
    public float MaxOutflowLimit { get; init; } = 2f;

    [Serialize]
    public float OutflowLimitStep { get; init; } = 0.01f;

    [Serialize]
    public float DefaultOutflowLimit { get; init; } = 2f;
}
