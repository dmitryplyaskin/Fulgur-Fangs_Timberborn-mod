using Bindito.Core;
using Timberborn.EntityPanelSystem;

namespace FulgurFangs.Code.UI;

public sealed class ElectricityEntityPanelModuleProvider : IProvider<EntityPanelModule>
{
    private readonly ElectricityBatteryFragment _electricityBatteryFragment;
    private readonly ElectricityNetworkFragment _electricityNetworkFragment;
    private readonly ElectricityTowerFragment _electricityTowerFragment;

    public ElectricityEntityPanelModuleProvider(
        ElectricityBatteryFragment electricityBatteryFragment,
        ElectricityNetworkFragment electricityNetworkFragment,
        ElectricityTowerFragment electricityTowerFragment)
    {
        _electricityBatteryFragment = electricityBatteryFragment;
        _electricityNetworkFragment = electricityNetworkFragment;
        _electricityTowerFragment = electricityTowerFragment;
    }

    public EntityPanelModule Get()
    {
        var builder = new EntityPanelModule.Builder();
        builder.AddMiddleFragment(_electricityBatteryFragment, 0);
        builder.AddMiddleFragment(_electricityTowerFragment, 1);
        builder.AddTopFragment(_electricityNetworkFragment, 10);
        return builder.Build();
    }
}
