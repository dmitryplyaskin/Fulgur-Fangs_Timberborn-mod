using HarmonyLib;
using Timberborn.BaseComponentSystem;
using Timberborn.SelectionSystem;

namespace FulgurFangs.Code.Electricity;

[HarmonyPatch(typeof(EntitySelectionService), nameof(EntitySelectionService.Select))]
public static class ElectricityConsumerSelectPatch
{
    [HarmonyPostfix]
    public static void Postfix(BaseComponent __0, EntitySelectionService __instance)
    {
        ElectricityConsumerSelectionTracker.Select(
            ElectricityConsumerSelectionHelper.ResolveConsumer(__0)
            ?? ElectricityConsumerSelectionHelper.ResolveConsumer(__instance.SelectedObject));
    }
}

[HarmonyPatch(typeof(EntitySelectionService), nameof(EntitySelectionService.SelectAndFollow))]
public static class ElectricityConsumerSelectAndFollowPatch
{
    [HarmonyPostfix]
    public static void Postfix(BaseComponent __0, EntitySelectionService __instance)
    {
        ElectricityConsumerSelectionTracker.Select(
            ElectricityConsumerSelectionHelper.ResolveConsumer(__0)
            ?? ElectricityConsumerSelectionHelper.ResolveConsumer(__instance.SelectedObject));
    }
}

[HarmonyPatch(typeof(EntitySelectionService), nameof(EntitySelectionService.SelectAndFocusOn))]
public static class ElectricityConsumerSelectAndFocusPatch
{
    [HarmonyPostfix]
    public static void Postfix(BaseComponent __0, EntitySelectionService __instance)
    {
        ElectricityConsumerSelectionTracker.Select(
            ElectricityConsumerSelectionHelper.ResolveConsumer(__0)
            ?? ElectricityConsumerSelectionHelper.ResolveConsumer(__instance.SelectedObject));
    }
}

[HarmonyPatch(typeof(EntitySelectionService), nameof(EntitySelectionService.Replace))]
public static class ElectricityConsumerReplaceSelectionPatch
{
    [HarmonyPostfix]
    public static void Postfix(SelectableObject __1)
    {
        ElectricityConsumerSelectionTracker.Select(
            ElectricityConsumerSelectionHelper.ResolveConsumer(__1));
    }
}

[HarmonyPatch(typeof(EntitySelectionService), nameof(EntitySelectionService.Unselect), new[] { typeof(SelectableObject) })]
public static class ElectricityConsumerUnselectSpecificPatch
{
    [HarmonyPrefix]
    public static void Prefix(SelectableObject __0)
    {
        ElectricityConsumerSelectionTracker.Clear(
            ElectricityConsumerSelectionHelper.ResolveConsumer(__0));
    }
}

[HarmonyPatch(typeof(EntitySelectionService), nameof(EntitySelectionService.Unselect), new System.Type[] { })]
public static class ElectricityConsumerUnselectPatch
{
    [HarmonyPrefix]
    public static void Prefix()
    {
        ElectricityConsumerSelectionTracker.Clear();
    }
}

internal static class ElectricityConsumerSelectionHelper
{
    public static ElectricityConsumerComponent? ResolveConsumer(BaseComponent? component)
    {
        if (component == null)
        {
            return null;
        }

        return component.GetComponent<ElectricityConsumerComponent>()
               ?? component.Transform.GetComponentInParent<ElectricityConsumerComponent>()
               ?? component.GetComponentInChildren<ElectricityConsumerComponent>(true)
               ?? component.Transform.root.GetComponentInChildren<ElectricityConsumerComponent>(true);
    }

    public static ElectricityConsumerComponent? ResolveConsumer(SelectableObject? selectableObject)
    {
        if (selectableObject == null)
        {
            return null;
        }

        return selectableObject.GetComponent<ElectricityConsumerComponent>()
               ?? selectableObject.Transform.GetComponentInParent<ElectricityConsumerComponent>()
               ?? selectableObject.GetComponentInChildren<ElectricityConsumerComponent>(true)
               ?? selectableObject.Transform.root.GetComponentInChildren<ElectricityConsumerComponent>(true);
    }
}

internal static class ElectricityConsumerSelectionTracker
{
    private static ElectricityConsumerComponent? _selectedConsumer;

    public static void Select(ElectricityConsumerComponent? consumer)
    {
        if (ReferenceEquals(_selectedConsumer, consumer))
        {
            return;
        }

        _selectedConsumer?.ClearNetworkSelection();
        _selectedConsumer = consumer;
        _selectedConsumer?.HighlightNetworkSelection();
    }

    public static void Clear(ElectricityConsumerComponent? consumer = null)
    {
        if (consumer != null && !ReferenceEquals(_selectedConsumer, consumer))
        {
            return;
        }

        _selectedConsumer?.ClearNetworkSelection();
        _selectedConsumer = null;
    }
}
