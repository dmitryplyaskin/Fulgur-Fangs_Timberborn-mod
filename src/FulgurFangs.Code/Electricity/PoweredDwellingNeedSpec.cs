using Timberborn.BlueprintSystem;

namespace FulgurFangs.Code.Electricity;

public record PoweredDwellingNeedSpec : ComponentSpec
{
    [Serialize]
    public string NeedId { get; init; } = "";

    [Serialize]
    public float PointsPerHour { get; init; }
}
