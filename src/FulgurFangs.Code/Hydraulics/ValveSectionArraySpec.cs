using System.Collections.Immutable;
using Timberborn.BlueprintSystem;
using UnityEngine;

namespace FulgurFangs.Code.Hydraulics;

public record ValveSectionArraySpec : ComponentSpec
{
    [Serialize]
    public string SectionTemplateName { get; init; } = string.Empty;

    [Serialize]
    public ImmutableArray<Vector3Int> SectionCoordinates { get; init; }

    [Serialize]
    public float MaxOutflowLimit { get; init; } = 2f;

    [Serialize]
    public float OutflowLimitStep { get; init; } = 0.01f;

    [Serialize]
    public float DefaultOutflowLimit { get; init; } = 1f;
}
