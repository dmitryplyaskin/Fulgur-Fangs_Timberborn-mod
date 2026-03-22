using System.Collections.Generic;
using Timberborn.EntityPanelSystem;
using Timberborn.Localization;
using Timberborn.UIFormatters;
using UnityEngine.UIElements;

namespace FulgurFangs.Code.Electricity;

internal static class ElectricityEntityDescriptions
{
    public static IEnumerable<EntityDescription> CreateNetworkDescriptions(
        ILoc loc,
        DescribedAmountFactory describedAmountFactory,
        ElectricitySubnetworkSnapshot snapshot,
        int baseOrder)
    {
        yield return CreateRow(
            describedAmountFactory.CreatePlain(loc.T("Electricity.Panel.Active"), $"{snapshot.Supply} / {snapshot.Consumption} EL"),
            baseOrder);

        yield return CreateRow(
            describedAmountFactory.CreatePlain(loc.T("Electricity.Panel.NetworkCharge"), $"{snapshot.StoredCharge} / {snapshot.StorageCapacity} EL"),
            baseOrder + 1);
    }

    public static IEnumerable<EntityDescription> CreateAccumulatorDescriptions(
        ILoc loc,
        DescribedAmountFactory describedAmountFactory,
        ElectricitySubnetworkSnapshot snapshot,
        int currentCharge,
        int capacity,
        int baseOrder)
    {
        foreach (EntityDescription description in CreateNetworkDescriptions(loc, describedAmountFactory, snapshot, baseOrder))
        {
            yield return description;
        }

        yield return CreateRow(
            describedAmountFactory.CreatePlain(loc.T("Electricity.Panel.AccumulatorCharge"), $"{currentCharge} / {capacity} EL"),
            baseOrder + 2);
    }

    private static EntityDescription CreateRow(VisualElement row, int order)
    {
        return EntityDescription.CreateMiddleSection(row, order);
    }
}
