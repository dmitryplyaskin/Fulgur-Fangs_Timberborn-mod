using FulgurFangs.Code.Electricity;
using HarmonyLib;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine.UIElements;

namespace FulgurFangs.Code.UI;

[HarmonyPatch]
public static class TopBarElectricityPatch
{
    private const string TopBarPanelTypeName = "Timberborn.TopBarSystem.TopBarPanel";
    private const string CounterName = "FulgurFangsElectricityCounter";
    private const string FallbackLabelName = "FulgurFangsElectricityLabel";

    [HarmonyPostfix]
    [HarmonyPatch]
    public static void Postfix(object __instance)
    {
        EnsureCounter(__instance);
        UpdateLabel(__instance);
    }

    [HarmonyTargetMethods]
    public static IEnumerable<MethodBase> TargetMethods()
    {
        Type? panelType = AccessTools.TypeByName(TopBarPanelTypeName);
        if (panelType == null)
        {
            yield break;
        }

        MethodInfo? postLoad = AccessTools.Method(panelType, "PostLoad");
        if (postLoad != null)
        {
            yield return postLoad;
        }

        MethodInfo? updateSingleton = AccessTools.Method(panelType, "UpdateSingleton");
        if (updateSingleton != null)
        {
            yield return updateSingleton;
        }
    }

    private static void UpdateLabel(object panel)
    {
        Label? label = EnsureCounter(panel) ?? EnsureFallbackLabel(panel);
        if (label == null)
        {
            return;
        }

        ElectricityNetworkState state = ElectricityService.Instance?.CurrentState ?? default;
        label.text = $"EL {state.Supply} / {state.Consumption}";
    }

    private static Label? EnsureCounter(object panel)
    {
        Type? panelType = panel.GetType();
        VisualElement? root = AccessTools.Field(panelType, "_root").GetValue(panel) as VisualElement;
        if (root == null)
        {
            return null;
        }

        VisualElement? existingCounterRoot = root.Q<VisualElement>(CounterName);
        if (existingCounterRoot != null)
        {
            return existingCounterRoot.Q<Label>("Count");
        }

        object? goodsGroupSpecService = AccessTools.Field(panelType, "_goodsGroupSpecService").GetValue(panel);
        object? topBarCounterFactory = AccessTools.Field(panelType, "_topBarCounterFactory").GetValue(panel);
        object? counters = AccessTools.Field(panelType, "_counters").GetValue(panel);
        if (goodsGroupSpecService == null || topBarCounterFactory == null || counters == null)
        {
            return null;
        }

        Type groupSpecServiceType = goodsGroupSpecService.GetType();
        object? materialsGroup = AccessTools.Method(groupSpecServiceType, "GetSpec")?.Invoke(goodsGroupSpecService, new object[] { "Materials" });
        if (materialsGroup == null)
        {
            return null;
        }

        Type topBarCounterFactoryType = topBarCounterFactory.GetType();
        object? counter = AccessTools.Method(topBarCounterFactoryType, "CreateSimpleCounter")?.Invoke(
            topBarCounterFactory,
            new object[] { materialsGroup, "Log", root });
        if (counter == null)
        {
            return null;
        }

        Type counterType = counter.GetType();
        VisualElement? counterRoot = AccessTools.Field(counterType, "_root").GetValue(counter) as VisualElement;
        Label? counterLabel = AccessTools.Field(counterType, "_counter").GetValue(counter) as Label;
        if (counterRoot == null || counterLabel == null)
        {
            return null;
        }

        counterRoot.name = CounterName;

        if (counters is IList list)
        {
            list.Add(counter);
        }

        return counterLabel;
    }

    private static Label? EnsureFallbackLabel(object panel)
    {
        Type panelType = panel.GetType();
        VisualElement? root = AccessTools.Field(panelType, "_root").GetValue(panel) as VisualElement;
        if (root == null)
        {
            return null;
        }

        Label? label = root.Q<Label>(FallbackLabelName);
        if (label != null)
        {
            return label;
        }

        label = new Label
        {
            name = FallbackLabelName,
            text = "EL 0 / 0"
        };
        label.style.minWidth = 90;
        label.style.marginLeft = 8;
        root.Add(label);
        return label;
    }
}
