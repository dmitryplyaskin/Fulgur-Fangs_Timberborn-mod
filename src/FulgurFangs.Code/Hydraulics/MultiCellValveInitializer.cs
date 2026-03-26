using Timberborn.TemplateInstantiation;

namespace FulgurFangs.Code.Hydraulics;

public sealed class MultiCellValveInitializer : IDedicatedDecoratorInitializer<MultiCellValveSpec, MultiCellValveComponent>
{
    public void Initialize(MultiCellValveSpec subject, MultiCellValveComponent decorator)
    {
        decorator.SetParameters(subject);
    }
}
