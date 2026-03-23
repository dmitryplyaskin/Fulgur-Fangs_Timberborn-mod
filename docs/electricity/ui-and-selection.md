# Electric UI And Selection

## Electric Consumer UI Rules

- Electric consumers must show electric UI, not vanilla mechanical UI.
- This is required even when the building is copied from a mechanical workshop shell.
- The current suppression hook is:
  `HideMechanicalPanelForElectricConsumersPatch`.

## Entity Panel Placement

- For production electric consumers,
  place the electric network block with:
  `EntityPanelModule.Builder.AddTopFragment(fragment, 10)`.
- This places electric info directly under the worker block.
- `AddMiddleFragment(...)` is the wrong place for the workshop-style layout under workers.

## Panel Implementation Rules

- Do not inject raw `Label` elements for production-like stats.
- Reuse Timberborn entity panel fragments and UXML templates.
- Good vanilla references:
  `Game/EntityPanel/MechanicalNodeFragment`
  and `Game/EntityPanel/BatteryFragment`
- Register through `EntityPanelModule` and `IEntityPanelFragment`.
- For live values, implement:
  `InitializeFragment()`,
  `ShowFragment(BaseComponent)`,
  `UpdateFragment()`,
  `ClearFragment()`.

## Electric Consumer Selection Rules

- Selecting an electric consumer should highlight the wire subnetwork that serves it.
- Patching only `SelectableObject.OnSelect()` was not stable enough.
- The current working approach is:
  patch `EntitySelectionService`
  and resolve the consumer from either the selected `BaseComponent`
  or the final `SelectedObject`.
- Keep selection state in a tracker.
- Let `ElectricityConsumerComponent` remember the exact poles it highlighted.
- Do not depend on recomputing the highlighted node set only during unselect.
