namespace FulgurFangs.Code.Electricity;

public readonly record struct ElectricitySubnetworkSnapshot(
    int Supply,
    int Demand,
    int Consumption,
    int StoredCharge,
    int StorageCapacity);
