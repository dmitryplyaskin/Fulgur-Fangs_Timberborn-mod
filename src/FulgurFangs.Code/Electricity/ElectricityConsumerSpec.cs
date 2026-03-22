using Timberborn.BlueprintSystem;

namespace FulgurFangs.Code.Electricity;

public record ElectricityConsumerSpec : ComponentSpec
{
    [Serialize]
    public int Demand { get; init; }
}
