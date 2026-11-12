# ORBITAL LAB: Gap Analysis (Phase 1 Audit)

Date: 2026-11-12. Scope: current repo state vs. the "complete aerospace engineering education platform" vision.
Method: file/config inspection, compile + test runs. **Not yet done:** a visual review of the running game (see Known Issues), and the Phase 2 research (no sources have been gathered yet; nothing below claims research-backed content).

---

## A. Current project status

| Area | State | Evidence |
|---|---|---|
| Unity 6 (6000.6.9f1), URP 17.6 | Working, compiles clean | Assembly newer than all scripts; no `error CS` in Editor.log |
| Scene | One greybox scene `Facility.unity` (hangar, corridor, mission control shell) | `Editor/FacilityBuilder.cs` generates it from code |
| Player | First-person controller (collision, gravity, smoothing, sprint), teleport keys 1-4 | `FirstPersonController.cs` |
| Interaction | Raycast prompt + E key | `InteractionSystem.cs`, `Interactable.cs` |
| Rocket workstation | IMGUI panel: 5 sliders, runs sim, 3 charts | `RocketWorkstation.cs` |
| Physics (C#) | US Std Atmosphere 1976, Tsiolkovsky, planar RK4 ascent, 3-phase guidance (ascent / coast / circularise) | `Scripts/Simulation/`; 14 checks pass in `SimTests/` |
| Physics (Python) | Atmosphere, rocket eq., orbital mechanics, NASA API client; 3 tests pass | `PythonPhysics/` (not yet cross-checked against C#) |
| Blender | One procedural two-stage rocket (28 plain meshes) | `BlenderAssets/Rockets/orbital_rocket_v1.blend` |
| NASA data | 2 cached SBDB JSON files, client written, **not integrated in Unity** | `NASAData/`, `nasa_client.py` |
| Textures / audio / animation | None | empty folders |
| Version control | Only the initial commit; all new work untracked | `git status` |

## B. Visual quality assessment (honest)

Judged from configuration and code; I have not yet viewed the render, so treat as a likely-weak baseline.

- Environment is built from Unity primitive cubes with untextured flat-colour URP/Lit materials. This is exactly the "random cubes" look the brief rejects.
- No texture maps (albedo/normal/roughness), so no surface detail.
- No scene post-processing: no Global Volume, so no tonemapping, bloom control, or colour grading. (URP's default profile exists but the scene doesn't use one.)
- Lighting is flat ambient plus ~25 unshadowed point lights. No baked GI, no reflection probes, no contact shadows, no AO confirmed.
- PC render asset: HDR on, **MSAA off**, 2048 shadow map, 50 m shadow distance, forward+ style additional lights (4/object).
- The only real asset is the rocket, and it is simple: smooth cylinders, a few boxes for fins, no internals.
- Hardware constraint: Intel UHD (integrated, 1 GB), i5-10310U, 16 GB RAM. Targeting HDRP or heavy realtime GI is not viable on this machine.

Biggest weaknesses, in order: (1) no textures, (2) flat lighting with no baked GI, (3) primitive geometry, (4) no post-processing, (5) one crude rocket.

## C. Missing environments

Existing: hangar (greybox), corridor, mission control (greybox, non-interactive).
Missing (all 11 departments in the spec except partial hangar/mission control): Lobby, Rocket Design Lab, Mechanical, Propulsion, Avionics, Aerodynamics, Thermal, Systems Integration, Testing & Verification, Mission Planning. Also the campus layout and inter-department navigation, facility map, doors.

## D. Missing rocket components

None of the component library exists. Needed: structural (nose cone, fairing, interstage, adapters, equipment bay, engine mount, fins), propulsion (conceptual engine, chamber, nozzle, tanks, turbopump, injector, feed system, cooling), avionics, electrical, payload, recovery. The current rocket is one fixed monolithic model with no component separation.

## E. Missing subsystem visualizations

Everything: exploded view, cutaway, x-ray, layer views (structural / propulsion / avionics / electrical / payload / thermal), labels and info markers, internal models.

## F. Missing scientific calculations

Have: atmosphere (valid 0-86 km, indicative above), Tsiolkovsky, drag with constant Cd, point-mass gravity, RK4, planar ascent.
Missing: speed-of-sound/Mach-dependent Cd, centre of gravity / centre of pressure / static margin, mass properties from components, thrust curves (solid/liquid), 3-DOF/6-DOF pitch dynamics, stage separation events with real component masses, structural loads (max-Q, axial g), thermal/aeroheating estimates, power budget, data-link budget, fault models, mission constraints, orbital mechanics beyond circularisation (inclination, transfer), Unity-side orbital propagation of real NASA bodies. Also no deterministic regression suite wired into CI.

## G. Missing interaction systems

Assembly (drag/drop, snapping, validation, undo/redo, history), inspection (rotate/zoom/isolate), doors, laboratories' workstations, block-based avionics wiring, experiment runners, 3D-driven launch (current charts are plots only, no rocket flies), pause/resume/restart/review, accessible menu navigation, facility map.

## H. Missing educational features

All of it: 10 learning modules, lessons, quizzes, experiments with baselines, failure scenarios, progress tracking, achievements, references. Needs a data model (ScriptableObjects) and an authored curriculum, which needs sourced content.

## I. Recommended Unity architecture

- **Keep URP** (do not migrate to HDRP: hardware cannot support it and migration risks existing work). Maximise quality inside URP: PBR textures, baked lightmaps and reflection probes, post-processing volume (ACES, restrained bloom, vignette, SSAO), MSAA/FXAA tiers.
- Quality tiers Low / Medium / High / Ultra implemented as URP asset variants + a runtime selector.
- Strict layering: `Simulation` (pure C#, engine-free, tested outside Unity) -> `Domain` (vehicle, components, missions as ScriptableObjects) -> `Presentation` (scenes, UI, interaction).
- Component database via ScriptableObjects (`ComponentDefinition`: subsystem, mass, dimensions, interfaces, explanation, sources, sim parameters).
- Scene per department (additive loading) plus a persistent core scene (player, UI, state).
- UI: UI Toolkit (UXML/USS) for the design system, replacing the temporary IMGUI.
- Assembly definitions per layer so the simulation compiles standalone and tests run via `dotnet test`.
- Repo structure follows the layout in the brief (adopt incrementally; do not break existing files).

## J. Recommended Blender asset pipeline

- One `.blend` per asset family; Python-scripted generators checked into the repo, so assets are reproducible and tweakable.
- Modular kit: wall/floor/ceiling panels, trusses, cable trays, doors, consoles, racks, then hero assets (components, engine cutaway, avionics, workstations).
- Standards: metres, +Z up in Blender, FBX export with fixed axis settings, pivots at attachment interface, `LOD0/1/2` naming, `UCX_` collision meshes, UV unwrap + shared texel density, atlas/trim sheets for environment.
- Textures: CC0 only (ambientCG / Poly Haven, license checked and logged in ASSET_INVENTORY.md) plus procedural Blender materials baked to textures.
- Verification loop: Blender `look` screenshots, then Unity review-capture (already built: `ReviewBridge` + `ReviewCapture`) to inspect actual in-engine renders.

## K. Development roadmap (summary; detail in IMPLEMENTATION_ROADMAP.md)

0. Foundation hygiene: commit, folders, asmdefs, tests, docs (now).
1. Research package (13 documents) with sourced facts: runs in parallel with 2.
2. Visual vertical slice: one hero room (Rocket Assembly Lab) at target quality: textures, bake, post, kit assets.
3. Component library + assembly mechanics.
4. Simulation upgrade (CG/CP/stability, mass from components, Mach-dependent Cd, 3-DOF) wired to the assembled vehicle and a 3D launch.
5. Departments one by one (Aero, Propulsion, Avionics first).
6. Education layer, then mission control tied to live simulation.
7. Optimisation, validation, full learning-journey test.

## L. Complexity and dependencies

| Work item | Size | Depends on |
|---|---|---|
| Visual vertical slice | L | PBR textures, Blender kit, lighting bake |
| Component data model + library | L | Research docs (sourced specs) |
| Assembly system | L | Component library, interface definitions |
| Vehicle-aware simulation (CG/CP, mass props) | M | Component data model |
| 3D launch driven by sim | M | Vehicle-aware sim, rocket asset |
| Each department lab | M each (x9) | Kit assets, component library, a sim feature each |
| Avionics wiring sim | M | Subsystem dependency map (research) |
| Learning system | L | Research, UI Toolkit design system |
| Optimisation / Ultra tier | M | Everything visual |

Total: multi-month scope for a solo developer. It cannot be completed in one session; it needs staged delivery with the status documents as the memory.

## M. Priority ranking

1. Visual vertical slice (largest gap vs. your stated top priority, and it sets the asset standards).
2. Component data model and library (central to the whole concept).
3. Assembly workflow (the most important feature per the brief).
4. Vehicle-aware simulation + 3D launch (makes the sim visible and meaningful).
5. Research package (runs alongside; gates the accuracy of 2 and the curriculum).
6. Aerodynamics, Propulsion, Avionics labs.
7. Education layer.
8. Remaining departments, mission control, NASA data.
9. Optimisation and Ultra tier.

## N. Risks and technical limitations

- **Hardware:** Intel UHD 1 GB limits Ultra visuals and baking time. Mitigation: baked lighting, trim-sheet textures, aggressive LOD, tiered quality. Ultra can be built but not tested well here.
- **Scope:** the brief is a multi-month, multi-person project. Mitigation: vertical slices, honest status tracking.
- **Visual verification:** I could not yet see Unity's output. Mitigation: the capture bridge. Your first run of it needs Unity focused (and my first attempt was interrupted).
- **Research integrity:** specs for real vehicles must come from fetched sources. Mitigation: no component or lesson number is committed without a source; unknowns are labelled.
- **Safety boundary:** propulsion/avionics content stays conceptual and non-actionable (no manufacturing, ignition or pyrotechnic detail), as the brief requires.
- **Asset licensing:** CC0 only, logged per file.
- **IMGUI debt:** the current UI is temporary and must be replaced.
- **Python vs. C# duplication:** two physics implementations risk divergence; plan is C# as source of truth, Python as an independent cross-check.
