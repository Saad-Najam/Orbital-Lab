# Known Issues

| # | Issue | Impact | Plan |
|---|---|---|---|
| 1 | In-engine render never viewed; screenshot bridge not yet run successfully | Visual claims are unverified | Run `ReviewBridge` once with Unity focused (stage 0) |
| 2 | Facility is primitive-cube greybox, no textures, no post-processing, no baked GI | Far below target quality | Stage 2 |
| 3 | `AscentSimulator` guidance is hand-tuned to the default rocket; single-burn insertion limited to ~200 km | Other vehicles may not reach orbit | Replace with vehicle-aware guidance (stage 5) |
| 4 | Default rocket numbers are hypothetical (Falcon 9-class), not sourced | Must stay labelled simulated | Replace with sourced reference vehicles (stage 1) |
| 5 | Max-Q came out ~43 kPa vs ~35 kPa expectation (check tolerance widened to 15 kPa) | Possibly pitch program too steep early | Investigate when guidance is replaced |
| 6 | Workstation UI is IMGUI | Not premium UI | UI Toolkit design system |
| 7 | Mission Control is non-interactive; no 3D launch | Charts only | Stages 6, 9 |
| 8 | Python physics not cross-checked vs C# | Possible divergence | Add cross-validation test |
| 9 | Unity recompiles only when its window is focused | Automation delays | Focus Unity before reviewing |
| 10 | Unity MCP bridge / plugin not yet loaded in the session | Cannot read Unity console directly; I read Editor.log instead | Restart Claude Code |
| 11 | Nothing committed to git beyond initial commit | Work at risk | Commit at stage 0 |
| 12 | `.env` was re-encoded to UTF-8 (backup deleted) to fix Blender MCP | Re-check keys still intact | User to confirm |
