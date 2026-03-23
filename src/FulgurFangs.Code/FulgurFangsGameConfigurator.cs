using Bindito.Core;
using FulgurFangs.Code.Electricity;
using FulgurFangs.Code.UI;
using Timberborn.EntityPanelSystem;
using Timberborn.TemplateInstantiation;

namespace FulgurFangs.Code;

[Context("Game")]
public sealed class FulgurFangsGameConfigurator : Configurator
{
    protected override void Configure()
    {
        Bind<ElectricityService>().AsSingleton();
        Bind<ElectricityPoleComponent>().AsTransient();
        Bind<MechanicalToElectricConverterComponent>().AsTransient();
        Bind<ElectricityConsumerComponent>().AsTransient();
        Bind<ElectricityAccumulatorComponent>().AsTransient();
        Bind<PoweredDwellingNeedComponent>().AsTransient();
        Bind<ElectricityBatteryFragment>().AsSingleton();
        Bind<ElectricityNetworkFragment>().AsSingleton();
        MultiBind<EntityPanelModule>().ToProvider<ElectricityEntityPanelModuleProvider>().AsSingleton();
        MultiBind<TemplateModule>().ToProvider(ProvideTemplateModule).AsSingleton();
    }

    private static TemplateModule ProvideTemplateModule()
    {
        var builder = new TemplateModule.Builder();
        builder.AddDedicatedDecorator<ElectricityRangeSpec, ElectricityPoleComponent>(new ElectricityPoleInitializer());
        builder.AddDedicatedDecorator<MechanicalToElectricConverterSpec, MechanicalToElectricConverterComponent>(new MechanicalToElectricConverterInitializer());
        builder.AddDedicatedDecorator<ElectricityConsumerSpec, ElectricityConsumerComponent>(new ElectricityConsumerInitializer());
        builder.AddDedicatedDecorator<ElectricityAccumulatorSpec, ElectricityAccumulatorComponent>(new ElectricityAccumulatorInitializer());
        builder.AddDedicatedDecorator<PoweredDwellingNeedSpec, PoweredDwellingNeedComponent>(new PoweredDwellingNeedInitializer());
        return builder.Build();
    }
}
