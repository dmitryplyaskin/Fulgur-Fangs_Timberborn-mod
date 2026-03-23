# Electric Consumers And Workshops

## Electric Consumer Pattern

The current reusable electric-consumer stack already exists in code:

- `ElectricityConsumerSpec`
- `ElectricityConsumerComponent`
- `ElectricityNetworkFragment`
- `ElectricityEntityPanelModuleProvider`
- `HideMechanicalPanelForElectricConsumersPatch`
- `ManufactoryElectricityPatch`
- `ElectricityConsumerSelectionPatch`

If a building is a normal production building that should simply consume electricity,
you usually do not need new C# code.

## Proven Electric Workshop Pattern

The first proven pattern is:
`version-1.0/Buildings/Wood/ElectricLumberMill/ElectricLumberMill.FulgurFangs.blueprint.json`

It reuses a vanilla manufactory shell,
but replaces mechanical participation with the electric consumer decorator.

### Include In Blueprint

- `WorkplaceSpec`
- `ManufactorySpec`
- `WorkshopSpec`
- `ElectricityConsumerSpec`
- Normal building model, access, label, workplace helper, construction, and animation specs

### Remove From Blueprint

- `MechanicalNodeSpec`
- `MechanicalBuildingSpec`
- `MechanicalConnectorTargetSpec`

### Keep With Care

- `TransputProviderSpec`
  Keep it only if the building still needs its vanilla transput behavior.

## Electric Workshop Checklist

For the next simple electric workshop, the intended workflow is:

1. Copy a suitable vanilla `Manufactory` or `Workshop` blueprint shell.
2. Rename `TemplateSpec.TemplateName` and place the blueprint under `version-1.0/Buildings/...`.
3. Remove all mechanical power specs from the copied blueprint.
4. Add `ElectricityConsumerSpec` with the desired `Demand`.
5. Adjust `WorkplaceSpec`.
6. Add or swap `ProductionRecipeIds`.
7. Add building localizations.
8. Register the building in `version-1.0/TemplateCollections/TemplateCollection.Buildings.FulgurFangs.blueprint.json`.
9. Build `src/FulgurFangs.Code/FulgurFangs.Code.csproj`.
10. Verify in-game:
   no mechanical UI,
   no mechanical connection behavior,
   missing-power warning works,
   electric network highlight works on selection.

## Recipe Limits

- For standard `RecipeSpec`,
  ingredient amounts should be treated as integer-based.
- Decimal-looking workshop consumption in Timberborn UI can come from other systems.
- If a future workshop needs fractional helper input,
  treat that as a separate mechanic unless runtime behavior proves otherwise.

## Example Blueprint Skeleton

```json
{
  "WorkplaceSpec": {
    "MaxWorkers": 1,
    "DefaultWorkers": 1,
    "DefaultWorkerType": "Beaver",
    "DisallowOtherWorkerTypes": false,
    "WorkerTypeUnlockCosts": []
  },
  "ManufactorySpec": {
    "ProductionRecipeIds": [
      "Your.Recipe.Id"
    ]
  },
  "WorkshopSpec": {},
  "ElectricityConsumerSpec": {
    "Demand": 100
  },
  "TemplateSpec": {
    "TemplateName": "YourBuilding.FulgurFangs",
    "BackwardCompatibleTemplateNames": [],
    "RequiredFeatureToggle": "",
    "DisablingFeatureToggle": ""
  }
}
```

## Reuse Goal

For the next simple building, creation should be close to:

- copy blueprint shell
- remove mechanical specs
- add `ElectricityConsumerSpec`
- point to recipes
- add localization
- register in template collection

## Optional Electricity Pattern

Optional electricity is now also supported conceptually:

- the building keeps its base function without power
- `ElectricityConsumerComponent` is used only for bonus logic
- a separate component reads `SupplyFraction`
  and grants extra effects only while the building is powered

The first test case of this pattern is the electric dwelling:

- base function:
  normal housing
- optional electric bonus:
  extra resident need satisfaction while powered
