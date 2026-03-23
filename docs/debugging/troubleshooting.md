# Troubleshooting

## First Checks

- Confirm the issue comes from `version-1.0`.
- Confirm no stray files exist in the active mod root.
- Check `C:\Users\dima2\AppData\LocalLow\Mechanistry\Timberborn\Player.log`.
- Check `[FulgurFangs]` lines in `Player.log` before changing code.
- Crash report archives live in:
  `C:\Users\dima2\OneDrive\Документы\Timberborn\Error reports`

## Known Working Fixes

- New faction crash on game start:
  fixed by removing `StartingFactionSpec`.
- Folktails material crash on electric pole:
  fixed by adding `Folktails` to `MaterialCollectionIds`.
- Localization crash:
  caused by malformed CSV and stray localization folders.
- Component instantiation crash:
  fixed by binding decorator components in Bindito
  and using dedicated decorator initializers for custom specs.
- Early lifecycle crashes:
  fixed by registering components in `PostInitializeEntity`, not `Awake`.
- Converter power:
  fixed by resolving `MechanicalNode` from the entity after initialization.
- Workshop building crash with
  `There are no "Entrance" slots`:
  caused by missing `#Slot#Entrance` in the `Timbermesh`
  while the blueprint still used workshop slot specs.

## Hard Rules For Future Changes

- Keep the active mod folder clean.
- Do not store spare mods, spare manifests, or extra localization folders inside `Fulgur Fangs`.
- Keep only active localization CSV files in `version-1.0/Localizations`.
- When editing localization CSV, keep valid CSV escaping.
- For custom component specs, use:
  `ComponentSpec` + dedicated decorator initializer.
- For entity decorators, prefer:
  `PostInitializeEntity` and `DeleteEntity`.
- If a decorator needs a sibling game component, resolve it from the entity after initialization.
- For new ranged buildings, reuse the existing ranged-building pattern first.
- For new electric workshops, reuse the `ElectricLumberMill` pattern first.
- For custom workshop models, keep `#Slot#Entrance` inside the model
  if the blueprint uses `TransformSlotInitializerSpec`.
- When swapping a model footprint, update both:
  `BlockObjectSpec` and `CollidersSpec`.
- For quick construction-model testing, reusing the final `Timbermesh`
  as `ConstructionStage0` is acceptable,
  but treat it as temporary.

## Build Command

```powershell
dotnet build src/FulgurFangs.Code/FulgurFangs.Code.csproj
```
