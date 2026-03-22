using Timberborn.TemplateInstantiation;

namespace FulgurFangs.Code.Electricity;

public sealed class MechanicalToElectricConverterInitializer : IDedicatedDecoratorInitializer<MechanicalToElectricConverterSpec, MechanicalToElectricConverterComponent>
{
    public void Initialize(MechanicalToElectricConverterSpec subject, MechanicalToElectricConverterComponent decorator)
    {
        decorator.SetMaxOutput(subject.MaxOutput);
    }
}
