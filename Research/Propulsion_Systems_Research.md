# Propulsion Systems Research (conceptual level)

**Safety boundary:** this document and everything built from it is educational. It must not contain dimensions, mixture ratios, ignition methods, propellant preparation or assembly procedures that would help build functional propulsion hardware. Models are non-functional concept visuals.

## Verified physics
- Thrust equation F = mdot*Ve + (pe - p0)*Ae; choked flow at the throat [V: G3].
- Isp = F/(mdot*g0), seconds [V: G2].

## Concepts to teach (all [T], to be cited from Sutton & Biblarz before release)
- Liquid engines: propellant tanks -> feed system (pumps or pressure) -> injector -> combustion chamber -> converging-diverging nozzle. Educational block diagram only.
- Solid motors: propellant grain burning inside a case; thrust curve shaped by grain geometry; cannot be throttled or shut down on demand (conceptual).
- Hybrid: solid fuel + liquid/gas oxidiser (conceptual comparison).
- Nozzle: converging-diverging; expansion ratio trades sea-level vs vacuum performance (why upper stages have larger bells). Rocket Lab publicly describes a vacuum-optimised second-stage engine with larger nozzle [S].
- Cooling: regenerative cooling concept (propellant circulates around the chamber wall) [T].
- Turbopump cycle concepts (gas generator, staged combustion, electric pump) at block-diagram level. Rutherford electric-pump-fed engine is publicly described as such [S].

## Simulation abstraction (what the code does)
- Engine = {thrust at sea level, thrust in vacuum, Isp at sea level, Isp in vacuum}; interpolated by ambient pressure ratio [E].
- No chamber pressure/temperature dynamics, no throttle transients. Any chamber-pressure/temperature displays must be labelled "illustrative model output".

## Educational experiments
1. Thrust vs altitude (pressure term). 2. Isp vs delta-v. 3. Nozzle expansion ratio effect (qualitative curve from a simple isentropic-flow model [T] to be sourced). 4. Solid vs liquid thrust-curve shape comparison.

## Gaps
- Sourced generic ranges for Isp by propellant class: take from NASA/Glenn or Sutton tables. [U until fetched]
- Real engine data used in the product must come from public manufacturer/NASA documents with citation. [U]
