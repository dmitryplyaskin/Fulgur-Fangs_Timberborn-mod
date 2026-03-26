using Timberborn.BlueprintSystem;

namespace FulgurFangs.Code.Electricity;

public record HydroelectricGeneratorSpec : ComponentSpec
{
    [Serialize]
    public int MaxOutput { get; init; }

    [Serialize]
    public float PowerPerFlowUnit { get; init; }
}
