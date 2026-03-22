using Timberborn.BlueprintSystem;

namespace FulgurFangs.Code.Electricity;

public record MechanicalToElectricConverterSpec : ComponentSpec
{
    [Serialize]
    public int MaxOutput { get; init; }
}
