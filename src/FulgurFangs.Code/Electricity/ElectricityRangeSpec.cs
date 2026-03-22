using Timberborn.BlueprintSystem;

namespace FulgurFangs.Code.Electricity;

public record ElectricityRangeSpec : ComponentSpec
{
    [Serialize]
    public int Range { get; init; }

    [Serialize]
    public int TransmissionLoss { get; init; }
}
