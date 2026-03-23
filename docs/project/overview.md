# Project Overview

## Project State

- Game target: Timberborn `v1.0`.
- Active mod payload lives in `version-1.0`.
- The faction `Fulgur Fangs` already loads as a separate faction.
- Current base faction data is reused from `Iron Teeth`.
- Electricity is currently an MVP layer implemented in `Code.dll`.

## Source Of Truth

- Official blueprints:
  `F:\SteamLibrary\steamapps\common\Timberborn\Timberborn_Data\StreamingAssets\Modding\Blueprints`
- Reference faction mod:
  `F:\SteamLibrary\steamapps\workshop\content\1062090\3346318229`
- Local modding wiki:
  `timberborn-modding.wiki`

## Active Mod Layout

- `version-1.0/manifest.json`
  Timberborn 1.0 mod entry.
- `version-1.0/Buildings`
  Custom buildings and copied vanilla shells.
- `version-1.0/Factions`
  Faction setup and faction modifiers.
- `version-1.0/Recipes`
  Custom production recipes.
- `version-1.0/TemplateCollections`
  Registration of custom buildings.
- `version-1.0/Localizations`
  Active localization CSV files only.
- `version-1.0/Code.dll`
  Runtime assembly used by the game.
- `src/FulgurFangs.Code`
  Runtime code project.
