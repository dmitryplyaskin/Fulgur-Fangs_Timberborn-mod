using Timberborn.TemplateInstantiation;

namespace FulgurFangs.Code.Electricity;

public sealed class ElectricityConsumerInitializer : IDedicatedDecoratorInitializer<ElectricityConsumerSpec, ElectricityConsumerComponent>
{
    public void Initialize(ElectricityConsumerSpec subject, ElectricityConsumerComponent decorator)
    {
        decorator.SetDemand(subject.Demand);
    }
}
