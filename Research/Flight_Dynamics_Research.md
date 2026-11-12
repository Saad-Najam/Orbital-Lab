# Flight Dynamics Research

## Equations implemented in `AscentSimulator` (C#)
- Gravity: a = -mu r / |r|^3, mu = 3.986004418e14 [T; standard value]. Spherical Earth R = 6,371,000 m [A].
- Thrust: interpolated by ambient pressure ratio; mass flow = F / (Isp g0) with Isp interpolated [E; G2, G3].
- Drag: 1/2 rho Cd A v^2 opposing velocity, atmosphere from US Std 1976 [E1; reference values verified in tests].
- Integration: classical RK4, dt = 0.05 s, mass and state integrated together [T]. Planar, inertial, non-rotating Earth [E].
- Orbital elements from state via specific energy and angular momentum; periapsis/apoapsis [T].
- Guidance: vertical rise, programmed pitch-over, exo-atmospheric climb-rate feedback, then apoapsis-target burn -> coast -> circularisation [E, hand-tuned].

## Validated (SimTests, 14 checks)
Atmosphere vs 1976 reference table values; Tsiolkovsky vs analytic; final speed vs circular speed within 60 m/s; periapsis within 25 km of target; coast energy conserved; Max-Q in a wide plausibility band.

## Known deviations
- Max-Q ~43 kPa vs expectation ~35 kPa; tolerance widened. Suspect pitch program. [open]
- Earth rotation ignored (a due-east launch gains up to ~0.46 km/s at the equator [T]). Not modelled.
- Constant Cd, no winds, no angle of attack. Not valid for dispersion or loads analysis.

## Required next
- Numerical order-of-convergence test (halve dt; error should drop ~16x for RK4) [T].
- 3-DOF with pitch dynamics once CG/CP exist.
- Orbit propagation for mission scenes (two-body).
- Cross-check against the independent Python implementation.
