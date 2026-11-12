# Implementation Roadmap

> **Pivot (2026-11-12):** product is a 3D rocket anatomy explorer + builder (educational platform), not a facility walk-through or launch game. Stages 2-4 below now mean: Explorer visual quality, component library, Build mode. Labs become 'topic modules' inside the same 3D app (aerodynamics, propulsion, avionics...) rather than rooms.

Rule: every stage ends with something runnable, verified, and recorded in DEVELOPMENT_STATUS.md.

| # | Stage | Exit criteria |
|---|---|---|
| 0 | Foundation hygiene | Git commits; folder layout; asmdefs (Simulation / Domain / Presentation); `dotnet test` runs sim checks; review-capture verified end-to-end |
| 1 | Research package | The 13 documents in `Research/`, each claim sourced and tagged Verified / Approximation / Simplification / Unknown |
| 2 | Visual vertical slice | One room (Rocket Assembly Lab) at target quality: CC0 PBR textures, Blender kit assets, baked lighting, reflection probes, post volume, quality tiers. Judged by in-engine screenshots |
| 3 | Component library | `ComponentDefinition` ScriptableObjects + Blender models for structural components first, with sourced descriptions |
| 4 | Assembly | Platform, catalogue, snapping, validation with explained errors, undo/redo, exploded/cutaway/layer views |
| 5 | Vehicle-aware simulation | Mass properties from components, CG/CP/static margin, Mach-dependent Cd, pitch dynamics; independent Python cross-check |
| 6 | 3D launch | Rocket flight driven by the simulation; telemetry from the same data; pause/resume/restart/review |
| 7 | Labs (in order) | Aerodynamics, Propulsion (conceptual only), Avionics (block wiring), Structures, Thermal, Systems Integration, Testing, Mission Planning |
| 8 | Education | Modules 1-10, experiments with baselines, failure scenarios, progress |
| 9 | Lobby, campus, mission control, NASA data | Navigation + map; live telemetry from sim; real vs simulated vs hypothetical labels |
| 10 | Optimise + validate | Profiling, LOD, Ultra tier; end-to-end learning journey test |

## Open decisions (need user input)
1. Which scope first: (a) research package, (b) visual vertical slice, or (c) both in parallel (default).
2. Approve restructuring `Assets/` to the folder layout in the brief (additive; no deletions).
3. Approve keeping URP (recommended) rather than evaluating HDRP.
