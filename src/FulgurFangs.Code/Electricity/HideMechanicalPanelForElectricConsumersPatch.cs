using HarmonyLib;
using System.Reflection;
using Timberborn.BaseComponentSystem;

namespace FulgurFangs.Code.Electricity;

[HarmonyPatch]
public static class HideMechanicalPanelForElectricConsumersPatch
{
    public static MethodBase? TargetMethod()
    {
        return AccessTools.Method("Timberborn.MechanicalSystemUI.MechanicalNodeFragment:ShowFragment");
    }

    [HarmonyPrefix]
    public static bool Prefix(object __instance, BaseComponent entity)
    {
        if (entity.GetComponent<ElectricityConsumerComponent>() == null)
        {
            return true;
        }

        AccessTools.Method(__instance.GetType(), "ClearFragment")?.Invoke(__instance, null);
        return false;
    }
}
