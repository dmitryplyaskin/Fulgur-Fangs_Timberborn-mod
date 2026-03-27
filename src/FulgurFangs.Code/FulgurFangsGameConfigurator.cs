using Bindito.Core;
using FulgurFangs.Code.Electricity;
using FulgurFangs.Code.Hydraulics;
using FulgurFangs.Code.UI;
using Timberborn.EntityPanelSystem;
using Timberborn.TemplateInstantiation;

namespace FulgurFangs.Code;

[Context("Game")]
public sealed class FulgurFangsGameConfigurator : Configurator
{
    protected override void Configure()
    {
        Bind<ElectricityConnectionService>().AsSingleton();
        Bind<ElectricityService>().AsSingleton();
        Bind<ElectricityCableRendererService>().AsSingleton();
        Bind<ElectricityConnectionCandidates>().AsSingleton();
        Bind<ElectricityPreviewCableRenderer>().AsSingleton();
        Bind<ElectricityConnectionAddingTool>().AsSingleton();
        Bind<ElectricityConnectionButtonFactory>().AsSingleton();
        Bind<ElectricityPoleComponent>().AsTransient();
        Bind<ElectricityTowerPreview>().AsTransient();
        Bind<MechanicalToElectricConverterComponent>().AsTransient();
        Bind<HydroelectricGeneratorComponent>().AsTransient();
        Bind<HydraulicTransferComponent>().AsTransient();
        Bind<MultiCellValveComponent>().AsTransient();
        Bind<ValveSectionArrayComponent>().AsTransient();
        Bind<ElectricityConsumerComponent>().AsTransient();
        Bind<ElectricityAccumulatorComponent>().AsTransient();
        Bind<PoweredDwellingNeedComponent>().AsTransient();
        Bind<ElectricityBatteryFragment>().AsSingleton();
        Bind<ElectricityNetworkFragment>().AsSingleton();
        Bind<HydraulicTransferFragment>().AsSingleton();
        Bind<ElectricityTowerFragment>().AsSingleton();
        MultiBind<EntityPanelModule>().ToProvider<ElectricityEntityPanelModuleProvider>().AsSingleton();
        MultiBind<TemplateModule>().ToProvider(ProvideTemplateModule).AsSingleton();
    }

    private static TemplateModule ProvideTemplateModule()
    {
        var builder = new TemplateModule.Builder();
        builder.AddDedicatedDecorator<ElectricityRangeSpec, ElectricityPoleComponent>(new ElectricityPoleInitializer());
        builder.AddDecorator<ElectricityPoleComponent, ElectricityTowerPreview>();
        builder.AddDedicatedDecorator<MechanicalToElectricConverterSpec, MechanicalToElectricConverterComponent>(new MechanicalToElectricConverterInitializer());
        builder.AddDedicatedDecorator<HydroelectricGeneratorSpec, HydroelectricGeneratorComponent>(new HydroelectricGeneratorInitializer());
        builder.AddDedicatedDecorator<HydraulicTransferSpec, HydraulicTransferComponent>(new HydraulicTransferInitializer());
        builder.AddDedicatedDecorator<MultiCellValveSpec, MultiCellValveComponent>(new MultiCellValveInitializer());
        builder.AddDedicatedDecorator<ValveSectionArraySpec, ValveSectionArrayComponent>(new ValveSectionArrayInitializer());
        builder.AddDedicatedDecorator<ElectricityConsumerSpec, ElectricityConsumerComponent>(new ElectricityConsumerInitializer());
        builder.AddDedicatedDecorator<ElectricityAccumulatorSpec, ElectricityAccumulatorComponent>(new ElectricityAccumulatorInitializer());
        builder.AddDedicatedDecorator<PoweredDwellingNeedSpec, PoweredDwellingNeedComponent>(new PoweredDwellingNeedInitializer());
        return builder.Build();
    }
}
