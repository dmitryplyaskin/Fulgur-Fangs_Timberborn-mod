# Building Models And Slots

## Scope

This note captures the practical workflow that worked for replacing a building model
inside the active mod payload in `version-1.0`.

Current proven test case:

- `version-1.0/Buildings/Wood/ElectricLumberMill/ElectricLumberMill.FulgurFangs.blueprint.json`
- custom model based on the vanilla `LumberMill.Folktails`

## What We Learned

- For this mod, Unity is not required to swap a building model.
- Timberborn building models are consumed as `Timbermesh`.
- The mod can reference a local model directly from `version-1.0`.
- Blueprint model paths do not include the `.timbermesh` extension.
- Workshop buildings that use `TransformSlotInitializerSpec`
  still need matching slots inside the model.

## File Placement

The working convention is:

- put the model next to the blueprint
- name it like `Something.Model.timbermesh`
- reference it in blueprint as `Something.Model`

Example:

- file:
  `version-1.0/Buildings/Wood/ElectricLumberMill/ElectricLumberMill.FulgurFangs.Model.timbermesh`
- blueprint path:
  `Buildings/Wood/ElectricLumberMill/ElectricLumberMill.FulgurFangs.Model`

## Blueprint Changes Needed For A New Model

At minimum, update:

- `Children -> #Finished -> TimbermeshSpec -> Model`
- `Children -> #Unfinished -> ConstructionStage0 -> TimbermeshSpec -> Model`
  if you want a quick placeholder construction model
- `BlockObjectSpec.Size`
  if the footprint changed
- `BlockObjectSpec.Blocks`
  to match the new dimensions
- `BuildingAccessibleSpec.LocalAccess`
- `CollidersSpec`
- construction base nested blueprint
  for example `ConstructionBase2x3` -> `ConstructionBase3x3`

If the footprint changes, do not change only colliders.
The authoritative gameplay footprint is `BlockObjectSpec`.

## Workshop Slots

Important distinction:

- `BlockObjectSpec.Entrance`
  is the gameplay access tile
- `#Slot#Entrance`
  is the transform slot inside the model

For a normal enterable workshop,
both are needed.

The custom electric lumber mill crashed with:

- `InvalidOperationException: There are no "Entrance" slots`

because the blueprint still contained:

- `TransformSlotInitializerSpec`
- `WorkplaceSlotManagerSpec`
- `WorkshopAnimationControllerSpec`
- `WorkshopWorkerHiderSpec`

while the imported `Timbermesh` did not expose `#Slot#Entrance`.

Once the slot was enabled again in the model,
the workshop worked correctly.

## Slot Rule For Future Workshop Models

If a building is a normal workshop or other enterable workplace,
the model should include:

- `#Slot#Entrance`

If the slot is missing and the blueprint uses workshop slot initialization,
expect a crash during entity initialization.

Temporary workaround for visual-only testing:

- remove slot-dependent workshop specs from blueprint

But this should be treated only as a temporary debug step,
not the final setup for a production building.

## Custom Model Using Vanilla Textures

The tested custom lumber mill was built on top of the Folktails lumber mill textures.
This worked because the faction already includes:

- `Folktails`
- `IronTeeth`

in `FactionSpec.MaterialCollectionIds`.

If a custom model renders pink or crashes on material lookup,
check faction material collections first.

## Construction Stage Reality

For a first import test,
it is acceptable to reuse the finished model as `ConstructionStage0`.

Tradeoff:

- the building becomes placeable and testable quickly
- construction visuals are temporary
- animated or staged construction blocks will not match vanilla quality

Final polish should use a dedicated construction model.

## Wires And Towers

For the current electricity MVP:

- wire connection points are not read from slots
- poles and towers use `ZiplineTowerSpec.CableAnchorPoint`

So for electric poles and towers:

- cable anchor is configured in blueprint
- a special cable slot inside the model is not required

This is separate from `#Slot#Entrance`,
which is still needed for enterable workshops.

## Recommended Test Loop

When importing a new building model:

1. Copy the `Timbermesh` into `version-1.0`.
2. Point the building blueprint to the new model.
3. Adjust `BlockObjectSpec.Size`, `Blocks`, colliders, and construction base.
4. Confirm the model has required workshop slots, especially `#Slot#Entrance`.
5. Launch the game.
6. Place the building.
7. Start construction and check `Player.log` immediately on failure.

## First Log To Check

- `C:\Users\dima2\AppData\LocalLow\Mechanistry\Timberborn\Player.log`

If the building crashes during placement or initialization,
check it before changing code.
