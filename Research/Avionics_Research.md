# Avionics Research (conceptual)

**Safety boundary:** architecture-level only. No ignition circuits, pyrotechnic initiation, safe/arm hardware details, or deployable guidance implementations.

## Functional chain (educational)
Sensors (IMU = accelerometers + gyros, pressure, temperature, GPS) -> data acquisition -> flight computer (state estimation + guidance/control logic) -> actuator commands (gimbal, valves, separation events: abstracted as "event signals") -> telemetry -> ground. [T: standard avionics architecture; cite SMAD / NASA avionics references]

## Concepts
- Inertial measurement: accelerometers measure specific force (not gravity); gyros measure rates; integrating them gives attitude/velocity with drift; GPS/other aiding corrects drift [T].
- Redundancy: dual/triple modular redundancy and voting; single-point-of-failure analysis [T].
- Data buses and telemetry framing at a block level (do not specify a real implementation) [T].
- Fault detection: out-of-range checks, cross-comparison between redundant sensors, watchdogs [T].

## Simulation abstraction
- Virtual block diagram with typed ports (power, data). Validation rules: every consumer needs power; every sensor path must reach the flight computer; the flight computer must reach telemetry. Harmless injected faults: stuck sensor, noisy sensor, dropped link, brown-out [E].
- All displayed values are SIMULATED.

## Experiments
Connect blocks correctly; find the single point of failure; inject a sensor fault and observe the cross-check; compare 1 vs 2 vs 3 sensors.

## Gaps
- Citable open sources on launch-vehicle avionics architecture. [U until fetched]
