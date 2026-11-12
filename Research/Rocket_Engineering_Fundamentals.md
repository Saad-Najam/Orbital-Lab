# Rocket Engineering Fundamentals

Per concept: meaning / why it matters / where used / effect on performance / visualisation / simulation / experiment. Tags per `Scientific_References.md`.

## 1. Thrust [V: G3]
- Meaning: F = mdot * Ve + (pe - p0) * Ae. mdot = propellant mass flow; Ve = exit velocity; pe exit pressure; p0 ambient pressure; Ae nozzle exit area. Flow is choked at the throat, so throat area sets mdot.
- Why: only external force a rocket generates on itself; the pressure term makes thrust higher in vacuum than at sea level.
- Simulate: already modelled as thrust interpolated between sea-level and vacuum values by ambient pressure [E].
- Experiment: same engine at 0 km vs 100 km, compare thrust and Isp.

## 2. Specific impulse [V: G2]
- Isp = F / (mdot * g0) = Veq / g0, units seconds, g0 = 9.80665 m/s^2.
- Effect: higher Isp means more delta-v per kg propellant. Visualise: bar comparison of delta-v vs Isp for fixed mass ratio.
- Experiment: sweep Isp 250-450 s with fixed masses.

## 3. Tsiolkovsky rocket equation [T]
- dv = Isp * g0 * ln(m0 / mf). m0 initial mass, mf final mass.
- Why: it governs staging and payload fraction; exponential penalty for dry mass.
- Implemented and tested against the analytic value (`SimTests`). Experiment: payload-mass sweep vs delta-v.

## 4. Thrust-to-weight ratio [T]
- TWR = F / (m g). Needs > 1 at liftoff to climb; too high raises acceleration loads.
- Experiment: reduce thrust until the vehicle cannot lift off; show liftoff TWR.

## 5. Gravity and drag losses [T]
- Real ascent needs more dv than orbital speed (~7.8 km/s at LEO) because of gravity and drag losses and steering.
- Our sim reaches 7.77 km/s at ~190-200 km with ideal dv ~12 km/s for the demo vehicle: consistent with large losses, but numbers are hypothetical.

## 6. Dynamic pressure and Max-Q [V: G4]
- q = 1/2 rho v^2. Zero at liftoff, rises, peaks (Max-Q), falls as density drops. Max-Q is a structural design constraint for full-scale rockets.
- Experiment: vary ascent pitch program and observe Max-Q; sim output has max-Q time and value.

## 7. Drag coefficient [V: G5 definition; G6 typical model-rocket ~0.75 based on cross-section]
- Cd = D / (q * A_ref); reference area must be stated. Real Cd varies with Mach number and shape. Our sim uses a constant Cd = 0.30 [A/E] - flagged as a limitation (no transonic drag rise).

## 8. Stability: CG and CP [V: G1]
- Stable when centre of pressure is below (aft of) centre of gravity. Remedies: more fin area, lower CP, raise CG (nose mass). Full-scale rockets typically rely on gimballed nozzles rather than fins.
- Not yet simulated. Needs mass-properties-from-components and aero CP estimate (stage 5 of roadmap).
- Educational metric: static margin in calibres (e.g. 1-2 cal is a common model-rocket rule of thumb [T]).

## 9. Orbital velocity and insertion [T]
- Circular speed v = sqrt(mu / r), mu_Earth = 3.986004418e14 m^3/s^2; implemented. A 200 km circular orbit needs ~7.79 km/s (sim: 7.774 km/s achieved).
- Ascent strategy used: burn to target apoapsis, coast, circularise (Hohmann-style) [E].

## 10. Staging [T]
- Dropping dead mass improves final mass ratio; sum of per-stage Tsiolkovsky dv. Implemented, instantaneous separation with short coast [E].

## Unknowns / gaps
- Transonic and supersonic Cd curves for generic bodies: need a sourced table. [U]
- Typical structural mass fractions by stage type: need sourced ranges (SMAD or NASA reports). [U]
