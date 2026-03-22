using Timberborn.TemplateInstantiation;

namespace FulgurFangs.Code.Electricity;

public sealed class ElectricityAccumulatorInitializer : IDedicatedDecoratorInitializer<ElectricityAccumulatorSpec, ElectricityAccumulatorComponent>
{
    public void Initialize(ElectricityAccumulatorSpec subject, ElectricityAccumulatorComponent decorator)
    {
        decorator.SetParameters(subject.Capacity, subject.LeakagePerHour, subject.MaxDischargePerHour);
    }
}
