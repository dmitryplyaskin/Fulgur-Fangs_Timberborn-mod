# Implemented Mechanics

## Faction

- `Fulgur Fangs` loads as a separate faction.
- The current implementation reuses `Iron Teeth` assets and many vanilla shells.

## Electricity

- Separate electricity simulation exists on top of Timberborn mechanics.
- Electricity is shown locally in the selected building card.
- No global top-bar counter is used because multiple isolated subnetworks can exist.

## Electricity Buildings

- `Basic Dynamo Converter`
- `Advanced Dynamo Converter`
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
- `Accumulator Cell`
- `Salicornia`
- `Pickled Salicornia`
- `Mine`
- `Electrified Mine`
