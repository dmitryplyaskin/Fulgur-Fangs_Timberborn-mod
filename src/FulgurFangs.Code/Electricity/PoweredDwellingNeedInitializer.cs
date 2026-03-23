using Timberborn.TemplateInstantiation;

namespace FulgurFangs.Code.Electricity;

public sealed class PoweredDwellingNeedInitializer : IDedicatedDecoratorInitializer<PoweredDwellingNeedSpec, PoweredDwellingNeedComponent>
{
    public void Initialize(PoweredDwellingNeedSpec subject, PoweredDwellingNeedComponent decorator)
    {
        decorator.SetParameters(subject.NeedId, subject.PointsPerHour);
    }
}
