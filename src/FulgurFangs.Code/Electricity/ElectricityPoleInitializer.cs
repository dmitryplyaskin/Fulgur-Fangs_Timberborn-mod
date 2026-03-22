using Timberborn.TemplateInstantiation;

namespace FulgurFangs.Code.Electricity;

public sealed class ElectricityPoleInitializer : IDedicatedDecoratorInitializer<ElectricityRangeSpec, ElectricityPoleComponent>
{
    public void Initialize(ElectricityRangeSpec subject, ElectricityPoleComponent decorator)
    {
        decorator.SetRange(subject.Range);
        decorator.SetTransmissionLoss(subject.TransmissionLoss);
    }
}
