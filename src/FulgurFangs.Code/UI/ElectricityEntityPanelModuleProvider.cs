using Bindito.Core;
using Timberborn.EntityPanelSystem;

namespace FulgurFangs.Code.UI;

public sealed class ElectricityEntityPanelModuleProvider : IProvider<EntityPanelModule>
{
    private readonly ElectricityBatteryFragment _electricityBatteryFragment;
    private readonly ElectricityNetworkFragment _electricityNetworkFragment;
    private readonly HydraulicTransferFragment _hydraulicTransferFragment;
    private readonly ElectricityTowerFragment _electricityTowerFragment;

    public ElectricityEntityPanelModuleProvider(
        ElectricityBatteryFragment electricityBatteryFragment,
        ElectricityNetworkFragment electricityNetworkFragment,
        HydraulicTransferFragment hydraulicTransferFragment,
        ElectricityTowerFragment electricityTowerFragment)
    {
        _electricityBatteryFragment = electricityBatteryFragment;
        _electricityNetworkFragment = electricityNetworkFragment;
        _hydraulicTransferFragment = hydraulicTransferFragment;
        _electricityTowerFragment = electricityTowerFragment;
    }

    public EntityPanelModule Get()
    {
        var builder = new EntityPanelModule.Builder();
        builder.AddMiddleFragment(_electricityBatteryFragment, 0);
        builder.AddMiddleFragment(_electricityTowerFragment, 1);
        builder.AddMiddleFragment(_hydraulicTransferFragment, 2);
        builder.AddTopFragment(_electricityNetworkFragment, 10);
        return builder.Build();
    }
}
