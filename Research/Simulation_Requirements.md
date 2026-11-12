# Simulation Requirements

Derived from the brief and research. Priority: P1 next, P2 soon, P3 later.

| ID | Requirement | Status | Pri |
|---|---|---|---|
| S1 | Engine-free simulation assembly (no UnityEngine), unit-tested outside Unity | Done (14 checks) | - |
| S2 | Atmosphere 1976 with density, pressure, temperature, speed of sound | Done (valid 0-86 km; indicative above) | - |
| S3 | Tsiolkovsky and per-stage delta-v | Done | - |
| S4 | RK4 integration with convergence test | RK4 done, convergence test missing | P1 |
| S5 | Mach-dependent Cd | Missing | P1 |
| S6 | Mass properties from assembled components (mass, CG, inertia) | Missing | P1 |
| S7 | CP estimate and static margin; stability verdict | Missing | P1 |
| S8 | Altitude-dependent gravity | Done (inverse-square) | - |
| S9 | 3-DOF pitch dynamics with gimbal control | Missing | P2 |
| S10 | Staging events using real component masses | Partial | P1 |
| S11 | Telemetry log driving Unity visualisation and mission control | Log exists; no 3D link | P1 |
| S12 | Pause / resume / restart / review | Missing (batch run only) | P1 |
| S13 | Deterministic stepping for replay | Fixed dt; to verify | P2 |
| S14 | Power budget, thermal lumped model, vibration SDOF, load margin | Missing | P2 |
| S15 | Avionics fault model | Missing | P2 |
| S16 | Orbit propagation (two-body) and transfers | Partial (elements only) | P2 |
| S17 | Earth rotation and launch azimuth | Missing | P3 |
| S18 | Cross-check vs Python implementation | Missing | P2 |

## Data policy
All sim output labelled SIMULATED. Reference vehicles labelled with source quality. Hypothetical vehicles labelled HYPOTHETICAL.

## Architecture
Simulation core independent of Unity; presentation reads a stepping interface (`Step(dt)`, `State`, `Log`) rather than running the whole ascent in one call.
