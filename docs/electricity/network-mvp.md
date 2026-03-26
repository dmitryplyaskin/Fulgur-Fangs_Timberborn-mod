# Electricity Network MVP

## Current Electricity MVP

- `Basic Dynamo Converter`
  converts mechanical power into electricity with a cap of `500 kW`.
- `Advanced Dynamo Converter`
  converts mechanical power into electricity with a cap of `2000 kW`.
- `Hydroelectric Valve`
  reuses the Iron Teeth valve behavior,
  keeps manual and automation flow control,
  generates up to `500 kW` from the real water flow through the structure,
  and supplies the grid only through `Distributor` area coverage like an accumulator.
- `Throttling Hydro Plant`
  is the first large test building on the reusable multi-port hydraulic runtime,
  uses `3` rear upper intake ports and `3` front lower discharge ports,
  and generates up to `1500 kW` through `Distributor` coverage.
- `Electric Pole`
  short-range transmission node with `20m` wire reach,
  `2` wire connections,
  and fixed network loss of `10 kW`.
- `Splitter`
  short-range branching node with up to `4` wire connections.
- `Transmission Tower (LEP)`
  long-range transmission node with `200m` wire reach
  and fixed network loss of `30 kW`.
- `Distributor`
  wire-connected local distribution node with service range.
- `Accumulator`
  local subnetwork storage with `20000 kWh` capacity,
  `0.1 kWh/hour` passive loss,
  and up to `250 kW` discharge.
- `Grid Tester`
  fixed electric consumer for validation.
- `Electric Lumber Mill`
  first real electric production building.

## Important Technical Decisions

- Electricity simulation is separate from Timberborn mechanical simulation.
- Mechanical power is only the input of converter buildings.
- Electricity uses real connected wire subnetworks, not one global pole pool.
- Only distribution nodes power buildings by area.
- Transmission-only nodes do not provide local area coverage.
- Transmission losses are fixed per-node values, not distance-based.
- Transmission losses apply only when a subnetwork has at least one consumer.
- Accumulators belong to the subnetwork of the distributor whose area contains them.
- Electric consumers receive a shared supply ratio within their assigned subnetwork.
- Manufactory-style buildings scale production through received electric fraction.

## Units

- Active network output and losses: `kW`
- Stored accumulator charge: `kWh`

## Timing Rules

- Network simulation should use `ITickableSingleton`.
- Tick energy transfer should use `IDayNightCycle.FixedDeltaTimeInHours`.
- If charge/discharge feels battery-like, the expected formula is:
  `energy = power * FixedDeltaTimeInHours`.
- Do not use frame updates for battery-style state.

## Finished-State Rules

- `Enabled` alone is not enough.
- Every electric runtime component should track finished state explicitly.
- The stable pattern is:
  implement `IFinishedStateListener`,
  cache `_isFinished`,
  initialize it from `BlockObject.IsFinished`,
  and expose readiness as `Enabled && _isFinished`.
- Resolve `BlockObject` safely with:
  `GetComponent<BlockObject>() ?? Transform.GetComponentInParent<BlockObject>()`.

## Current Open Limitations

- Vanilla buildings do not consume electricity yet.
- Consumer assignment in overlapping distributor areas is deterministic-first.
- Current wire-capable electricity buildings still use placeholder visuals.
