using Timberborn.BlueprintSystem;

namespace FulgurFangs.Code.Electricity;

public record ElectricityAccumulatorSpec : ComponentSpec
{
    [Serialize]
    public float Capacity { get; init; }

    [Serialize]
    public float LeakagePerHour { get; init; }

    [Serialize]
    public float MaxDischargePerHour { get; init; }
}
