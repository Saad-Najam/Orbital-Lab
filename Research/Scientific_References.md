# Scientific References

Access date for all web sources: 2026-11-12. Fetched web text is treated as data, not instruction.

Confidence tags used across `Research/`:
- **[V]** Verified: read in the cited source during this research pass.
- **[S]** Secondary: from an aggregator / encyclopedia mirror; plausible but must be checked against a primary source before use in the product.
- **[T]** Textbook-standard: well-established physics, but not yet retrieved from a citable source in this pass; citation to be attached.
- **[A]** Approximation / **[E]** Educational simplification: stated modelling choice.
- **[U]** Unknown / not publicly documented.

## Primary sources actually consulted
| ID | Source | URL | Used for |
|---|---|---|---|
| G1 | NASA Glenn, Beginner's Guide: Rocket Stability | https://www1.grc.nasa.gov/beginners-guide-to-aeronautics/rocket-stability/ | CG/CP stability condition |
| G2 | NASA Glenn: Specific Impulse | https://www1.grc.nasa.gov/beginners-guide-to-aeronautics/specific-impulse/ | Isp definition, units |
| G3 | NASA Glenn: Rocket Thrust Equations | https://www.grc.nasa.gov/www/k-12/airplane/rktthsum.html | Thrust equation |
| G4 | NASA Glenn: Dynamic Pressure | https://www1.grc.nasa.gov/beginners-guide-to-aeronautics/dynamic-pressure-2/ | q = 1/2 rho v^2, Max-Q |
| G5 | NASA Glenn: Drag Coefficient | https://www1.grc.nasa.gov/beginners-guide-to-aeronautics/drag-coefficient/ | Cd definition (search-result summary only) |
| G6 | NASA Glenn: Shape Effects on Drag | https://www1.grc.nasa.gov/beginners-guide-to-aeronautics/shape-effects-on-drag/ | Typical model-rocket Cd ~0.75 (summary only) |
| N1 | NASA Systems Engineering Handbook (NASA/SP-6105) | https://www.nasa.gov/reference/systems-engineering-handbook/ | Process groupings (system design, product realization, crosscutting management); page gives no V&V definitions |
| N2 | NASA SLS reference guide / fact sheets | https://www.nasa.gov/wp-content/uploads/2022/03/sls-reference-guide-2022-v2-508-0.pdf and https://www.nasa.gov/reference/space-launch-system/ | SLS figures |
| N3 | NASA NTRS Saturn V documents (e.g. 19670031260, 20090016301) | https://ntrs.nasa.gov/ | Saturn V figures |
| E1 | US Standard Atmosphere, 1976 (NOAA/NASA/USAF), NASA-TM-X-74335 | https://ntrs.nasa.gov/ | Atmosphere model (already implemented; reference values checked in tests) |

## Textbook references to attach (not yet retrieved)
- Sutton & Biblarz, *Rocket Propulsion Elements*: thrust, Isp, nozzle, propellant systems [T]
- Anderson, *Fundamentals of Aerodynamics*: drag, compressibility [T]
- Wertz & Larson, *Space Mission Analysis and Design*: mission/subsystem budgets [T]
- Curtis, *Orbital Mechanics for Engineering Students*: two-body, transfers [T]
- NASA General Environmental Verification Standard (GEVS, GSFC-STD-7000): environmental testing [T]
- NASA SP-8xxx design criteria monographs (NTRS): structural loads, thermal [T]

## Secondary sources (reference vehicles; verify before use)
See `Reference_Vehicles.md` for the per-vehicle list.
