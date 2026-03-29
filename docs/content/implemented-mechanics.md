# Implemented Mechanics

## Faction

- `Fulgur Fangs` loads as a separate faction.
- The current implementation reuses `Iron Teeth` assets and many vanilla shells.

## Electricity

- Separate electricity simulation exists on top of Timberborn mechanics.
- Electricity is shown locally in the selected building card.
- No global top-bar counter is used because multiple isolated subnetworks can exist.
- A reusable multi-port hydraulic transfer runtime now exists for future hydro plants.
- A native multi-cell valve runtime exists for large hydro plants and wide spillways.

## Electricity Buildings

- `Basic Dynamo Converter`
- `Advanced Dynamo Converter`
- `Hydroelectric Valve`
- `Throttling Hydro Plant`
- `Upper-Tier Hydro Plant`
- `Electric Pole`
- `Splitter`
- `Transmission Tower`
- `Distributor`
- `Accumulator`
- `Grid Tester`
- `Electric Lumber Mill`

## Electric Lumber Mill

- First real electric production building.
- Now supports a custom `Timbermesh` test model.
- The current test model is based on the vanilla `LumberMill.Folktails`.
- Works from the mod electric network, not the vanilla mechanical network.
- `1` worker.
- `100 kW` demand.

### Recipes

- `1 Log -> 1 Plank` in `0.5 h`
- `3 Log + 1 RareEarthConcentrate -> 3 Plank` in `1 h`

## Other Implemented Content

- `Rubber Pine`
- `Rubber`
- `Rare Earth Concentrate`
- `Copper`
- `Conductive Components`
- `Electric Motor`
- `Accumulator Cell`
- `Charcoal`
- `Synthetic Fuel`
- `Salicornia`
- `Pickled Salicornia`
- `Mine`
- `Electrified Mine`

## Placeholder Production Integration

- `Smelter` now also provides a temporary `Charcoal` recipe.
- `Bot Part Factory` now also provides a temporary `Electric Motor` recipe.
- `Centrifuge` now also provides a temporary `Synthetic Fuel` recipe.
- These are integration placeholders so the full resource chain exists in-game
  before the dedicated power buildings are implemented.

## Hydro State

- `Hydroelectric Valve` is the small stable valve-based hydro generator.
- `Upper-Tier Hydro Plant` is the active hydro test bed for validating flow rows,
  spill shaping, hitbox alignment, and roof stacking.
- `Throttling Hydro Plant` now uses the proven large-hydro pattern discovered on the test bed.
- Practical rules and pitfalls for these buildings are documented in
  `docs/content/hydro-plants.md`.
