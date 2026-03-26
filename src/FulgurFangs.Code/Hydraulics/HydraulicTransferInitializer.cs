using Timberborn.TemplateInstantiation;

namespace FulgurFangs.Code.Hydraulics;

public sealed class HydraulicTransferInitializer : IDedicatedDecoratorInitializer<HydraulicTransferSpec, HydraulicTransferComponent>
{
    public void Initialize(HydraulicTransferSpec subject, HydraulicTransferComponent decorator)
    {
        decorator.SetParameters(subject);
    }
}
