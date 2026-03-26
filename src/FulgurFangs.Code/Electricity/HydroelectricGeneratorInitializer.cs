using Timberborn.TemplateInstantiation;

namespace FulgurFangs.Code.Electricity;

public sealed class HydroelectricGeneratorInitializer : IDedicatedDecoratorInitializer<HydroelectricGeneratorSpec, HydroelectricGeneratorComponent>
{
    public void Initialize(HydroelectricGeneratorSpec subject, HydroelectricGeneratorComponent decorator)
    {
        decorator.SetGenerationParameters(subject.MaxOutput, subject.PowerPerFlowUnit);
    }
}
