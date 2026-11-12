# Aerodynamics Research

## Verified
- Dynamic pressure q = 1/2 rho u^2; peak = Max-Q; design constraint [V: G4].
- Cd = D/(q*A_ref); reference area must be stated [V: G5 summary].
- Typical model-rocket Cd ~0.75 on cross-section area; long thin shapes have less drag than short thick [V: G6 summary]. Not a full-scale launch-vehicle value.
- Stability: CP below CG for stability; fins increase stability; full-scale rockets often use gimballing [V: G1].

## Standard concepts [T]
- Mach number M = v / a, a = sqrt(gamma R T). Drag rises sharply through transonic (M ~ 0.8-1.2) then falls; compressibility, shock waves.
- Nose shape effects (ogive vs conic vs blunt), boat-tail, base drag.
- CP estimation methods (e.g. Barrowman for small rockets; not valid for large vehicles with strap-ons, so label it a model-rocket method) [T].

## Simulation requirements
- Current: constant Cd 0.30 [E]; speed of sound is computed (atmosphere gives it) but not used for drag. **Gap.**
- Next: Cd(M) piecewise curve with a labelled source or labelled "illustrative"; Mach number already logged.
- CG/CP/static margin from component geometry; pitch-damping qualitative.

## Visualisation
Wind-tunnel view: streamline particles around a rocket silhouette (illustrative, not CFD); pressure colour map by a simple Newtonian/schematic model, labelled "schematic".

## Experiments
Compare nose shapes (qualitative Cd differences), fin sizes vs static margin, constant Cd vs Cd(M) trajectories.

## Gaps
- A citable generic Cd-vs-Mach curve for slender bodies. [U]
