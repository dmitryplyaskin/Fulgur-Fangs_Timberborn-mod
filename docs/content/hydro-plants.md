# Hydro Plants

## Scope

This note captures the working patterns and failed patterns discovered while building
the large `3x2x2` hydro plants for Fulgur Fangs.

Relevant blueprints:

- `version-1.0/Buildings/Power/ThrottlingHydroPlant/ThrottlingHydroPlant.FulgurFangs.blueprint.json`
- `version-1.0/Buildings/Power/UpperFlowHydroPlant/UpperFlowHydroPlant.FulgurFangs.blueprint.json`
- `version-1.0/Buildings/Power/HydroelectricValve/HydroelectricValve.FulgurFangs.blueprint.json`

## What Actually Worked

### 1. Native Valve Logic, Not Water Teleport

The large hydro plant only became stable after switching to the native valve-style flow logic.

Working rule:

- use `MultiCellValveComponent`
- drive water through `DirectionLimiter` and `OutflowLimit`
- let Timberborn's water solver move the water

Failed rule:

- do not move water manually between intake and output cells for this kind of structure
- the old hydraulic transfer approach produced teleported water, visual artifacts,
  and `Column ... not found` crashes

## 2. Active Water Cells Must Match The Intended Hydraulic Row

For the large custom hydro body, the hydraulic row and the visible shell must be aligned deliberately.

### Proven test bed

The test building `UpperFlowHydroPlant` became the stable reference.

Its working setup is:

- `BlockObjectSpec.Size = 3x2x2`
- active row:
  `MultiCellValveSpec.FlowCoordinates = (X:0..2, Y:1, Z:1)`
- matching horizontal obstacles on the same row:
  `FinishableHorizontalWaterObstacleSpec.Obstacles = (X:0..2, Y:1, Z:1)`
- lower front half-wall:
  `WaterObstacleSpec.Coordinates = (X:0..2, Y:0)`
- `FinishableWaterObstacleSpec.Height = 1.0`

This version proved two things:

- the large body can behave correctly with a native wide valve
- the local water level and effective head matter a lot for how the spill behaves

### Proven final large hydro plant

The final working `ThrottlingHydroPlant` uses the same overall footprint and the same front half-wall pattern,
but its final active row was moved down after the hydraulic test was complete:

- active row:
  `MultiCellValveSpec.FlowCoordinates = (X:0..2, Y:1, Z:0)`
- matching horizontal obstacles:
  `FinishableHorizontalWaterObstacleSpec.Obstacles = (X:0..2, Y:1, Z:0)`
- front lower half-wall:
  `WaterObstacleSpec.Coordinates = (X:0..2, Y:0)`

This is the currently accepted production configuration.

## 3. The Front Half-Wall Pattern Matters

The hidden half-wall was the practical key to shaping the spill correctly.

Working rule:

- keep a `WaterObstacleSpec` line on the front row
- keep `FinishableWaterObstacleSpec.Height = 1.0`

This blocks the lower half of the front face and lets the intended row remain the active spill line.

Failed patterns:

- putting the half-wall on the wrong row causes the water to appear behind or inside the shell
- adding extra outer obstacle rows can produce phantom partitions or even water-solver crashes

## 4. Do Not Push The Hydraulic Row Outside The Footprint

One failed experiment moved the flow row to `Y = -1`, outside the body.

This caused:

- phantom water partitions behind the building
- water appearing to pass through solid walls
- generally broken spill visuals

Hard rule:

- keep hydro flow rows inside the real building footprint

## 5. Test Variants First, Then Port To Production

The final fix was found faster after creating a separate test hydro blueprint
instead of repeatedly mutating the production building.

Recommended workflow:

1. Clone the large hydro blueprint under a temporary test name.
2. Change only one hydraulic variable at a time.
3. Verify spill behavior in-game.
4. Only after the test version is stable, copy the proven pattern back into the main building.

This is how `UpperFlowHydroPlant` was used.

## Hitbox And Selection

The click area problem was not caused by the water solver.

The practical fix was:

- keep the visible mesh on `ModelRoot`
- keep `ModelRoot.TransformSpec.Position.Z = 1.0`
- set the box collider center back to the local body origin:
  `CollidersSpec.BoxColliders[].Center.Z = 0.0`

Important rule:

- do not offset both `ModelRoot` and the collider center in the same axis

The earlier double offset made the building selectable from empty space in front of it.

## Buildable Roof

For the hydro roof to accept buildings on top, the top layer must be explicitly stackable.

Working pattern:

- top cells:
  `Occupations = "Floor, Bottom, Corners, Path, Middle"`
- top cells:
  `Stackable = "BlockObject"`
- `BlockObjectNavMeshSettingsSpec.GenerateFloorsOnStackable = true`

Do not rely on the visible deck mesh alone.

## Observed Engine Constraints

The large hydro experiments confirmed some important Timberborn constraints:

- water behavior is strongly tied to solver-controlled rows and columns, not to the visible mesh
- collider fixes do not replace correct `BlockObjectSpec`
- changing only `Z` inside one visual shell is not enough when the intended hydraulic separation is really about another row

## Debugging Workflow That Helped

Useful tools:

- `C:\Users\dima2\AppData\LocalLow\Mechanistry\Timberborn\Player.log`
- `[FulgurFangs][MultiCellValve]` runtime logs from
  `src/FulgurFangs.Code/Hydraulics/MultiCellValveComponent.cs`

The debug dump was used to inspect:

- transformed flow coordinates
- occupied block cells
- nearby water columns
- actual flow vectors

This made it possible to separate:

- wrong hydraulic rows
- wrong obstacle rows
- wrong collider offsets

## Anti-Patterns To Avoid

- hydraulic transfer by removing water in one cell and adding it in another
- external flow rows outside the footprint
- guessing with collider offsets before checking the active row and obstacle row
- trying to fix the hydro spill only through the visible mesh
- changing the production hydro building before proving the pattern on a disposable test blueprint
