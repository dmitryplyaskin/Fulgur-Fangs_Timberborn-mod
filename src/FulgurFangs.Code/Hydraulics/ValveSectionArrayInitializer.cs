using Timberborn.TemplateInstantiation;

namespace FulgurFangs.Code.Hydraulics;

public sealed class ValveSectionArrayInitializer : IDedicatedDecoratorInitializer<ValveSectionArraySpec, ValveSectionArrayComponent>
{
    public void Initialize(ValveSectionArraySpec subject, ValveSectionArrayComponent decorator)
    {
        decorator.SetParameters(subject);
    }
}
