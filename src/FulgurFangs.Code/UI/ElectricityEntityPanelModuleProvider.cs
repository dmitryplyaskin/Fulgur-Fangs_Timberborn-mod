using Bindito.Core;
using Timberborn.EntityPanelSystem;

namespace FulgurFangs.Code.UI;

public sealed class ElectricityEntityPanelModuleProvider : IProvider<EntityPanelModule>
{
    private readonly ElectricityBatteryFragment _electricityBatteryFragment;
    private readonly ElectricityNetworkFragment _electricityNetworkFragment;

    public ElectricityEntityPanelModuleProvider(
        ElectricityBatteryFragment electricityBatteryFragment,
        ElectricityNetworkFragment electricityNetworkFragment)
    {
        _electricityBatteryFragment = electricityBatteryFragment;
        _electricityNetworkFragment = electricityNetworkFragment;
    }

    public EntityPanelModule Get()
    {
        var builder = new EntityPanelModule.Builder();
        builder.AddMiddleFragment(_electricityBatteryFragment, 0);
        builder.AddTopFragment(_electricityNetworkFragment, 10);
        return builder.Build();
    }
}
