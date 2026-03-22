using HarmonyLib;
using Timberborn.Workshops;

namespace FulgurFangs.Code.Electricity;

[HarmonyPatch(typeof(Manufactory), "ProductionEfficiency")]
public static class ManufactoryElectricityPatch
{
    [HarmonyPostfix]
    public static void Postfix(Manufactory __instance, ref float __result)
    {
        ElectricityConsumerComponent? consumer = __instance.GetComponent<ElectricityConsumerComponent>();
        if (consumer == null)
        {
            return;
        }

        __result *= consumer.SupplyFraction;
    }
}
