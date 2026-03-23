# Range Overlays

## Working Pattern

For buildings that should behave like ranged service buildings, use:

- `IBuildingWithRange`
- `RangeTileMarkerService`
- `RangeObjectHighlighterService`

The current stable reference in this mod is the pole/distributor implementation.

## Registration Rules

- Register the building in `RangeTileMarkerService.AddBuildingWithRange(this)`.
- Register the building in `RangeObjectHighlighterService.AddBuildingWithObjectRange(this)`.
- On select, call:
  `RecalculateArea(RangeName)`,
  `DrawArea()`,
  `ShowArea()`,
  and object highlight recalculation.
- Each building instance must have its own unique `RangeName`.

## Coordinate Rules

- Do not build range tiles from `Transform.position`.
- Use `BlockObject.CoordinatesAtBaseZ`.
- For terrain-following overlays:
  iterate `x/y`,
  query `ITerrainService.GetAllHeightsInCell(...)`,
  take the top terrain `z`,
  and emit that exact `Vector3Int`.

## Pitfalls Already Seen

- `DrawRangeBoundsOnIt` in blueprint is not enough on its own.
- `AreaHighlightingService.DrawTile(...)` alone is not enough to mimic vanilla ranged overlays.
- Wrong highlight service calls can color the building red instead of drawing the intended area.
- Building ranges from `Transform.position` causes wrong axes and floating strips.
- Projecting to arbitrary top objects in a column produced unstable overlays.

## Current Limitation

- Current distributor-style range projection is stable on terrain and slopes.
- Behavior on roofs and some artificial surfaces may still differ from vanilla dispatcher coverage.
