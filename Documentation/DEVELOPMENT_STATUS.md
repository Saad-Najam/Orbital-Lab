# Development Status

Last updated: 2026-11-12

## Direction (updated per user)
ORBITAL LAB is a **3D rocket anatomy and build-your-own-rocket learning platform** (in the style of 3D anatomy browsers), **not a game and not a walkable facility**. The walkable facility scene remains in the repo but is deprioritised.

## Done
- Simulation core (C#, engine-free) + 14 passing checks (`SimTests/`).
- **Explorer rocket model** (Blender script `BlenderAssets/scripts/explorer_rocket.py`): 56 separate named parts across 5 assemblies, with internals (tanks, engine anatomy, avionics units, payload). Exported to `Assets/Models/Explorer/OrbitalRocketExplorer.fbx`.
- **Learning content** for all 56 parts (`Tools/build_parts_json.py` -> `Resources/ExplorerData/parts.json`): what / why / how / key ideas / interfaces / connections, plus quiz questions on key parts, with source tags.
- **Explorer app** (Unity, UI Toolkit): orbit camera, click/hover selection, parts tree with search, subsystem layer toggles, info panel, quizzes, X-ray, isolate, exploded view, labels, progress counter.
- Scene builder `ExplorerSceneBuilder` (studio lighting, ACES + bloom + vignette volume, SMAA).
- Research pass 1 (13 documents in `Research/`).
- `Tools/UnityCompileCheck`: compiles all Unity scripts against Unity's assemblies from the command line (0 errors).

## Not yet verified
- **The Explorer has never been run or viewed.** A screenshot request is queued (`Review/request.txt`); it executes when the Unity window has focus.
- Nozzle shading in the Blender model looks faceted.

## Next
1. Review screenshots, fix visual/UI problems.
2. Build mode (assembly platform, snapping, compatibility with explained errors).
3. Material/texture quality pass; cutaway shader; labels with leader lines.
4. Vehicle-aware simulation hooked to the assembled rocket.
5. More component families (fairing sizes, engines, payloads) and the other learning modules.
