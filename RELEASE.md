# Betelgeuse v1.0.0 — Diploma Release

**Date:** 2026-08-15  
**Status:** Final build for master's thesis defense  
**Unity:** 6000.x URP · product version `1.0.0`

## Theme coverage

| Requirement | Deliverable |
|-------------|-------------|
| Autonomous first-stage landing | RK4 physics + soft-landing criteria |
| Fuzzy logic | Zero-order Sugeno 5×5 |
| Machine learning | MLP 5→8→2 + ES(1+λ) |
| Hybrid intelligent system | Neuro-Fuzzy residual (cap before blend) |
| Comparison research | Monte-Carlo + ResearchExporter packs |
| Demo presentation | Mission Control HUD UA/EN · 8 themes · 3D |

## What's in this release

### GNC
- Modes A–D with distinct mid-flight behaviour; Ideal `[I]` soft-lands all modes
- Correct AttitudeGimbal / lateral TVC signs; Hybrid residual cap before BlendThrust
- UI wind/noise applied on single-flight start

### Visuals
- Circular lunar heightmap (neutral cool gray, natural craters, world UV + normals)
- Premium LZ pad (no edge chevrons); Falcon-class rocket with clean ogive tip
- URP long soft shadows (~1200 m PC via RP asset + reflection)

### HUD
- Single top chrome: brand · mode · time · flight actions · Theme/Lang over Status/Hide (right inset 64 px)
- Left GATE → guidance → telemetry → charts; right how → modes → compare → setup → %
- Bottom step strip (flight phase only); equal 3 px test sliders; theme-aware frames
- RebuildUi preserves graph samples + setup state; sharp TMP (LiberationSans + Cyrillic fallback)

### Export
- `SimulationLogs/Landing_Full_*` — MD / CSV / JSON / SVG
- Comparison packs via `[P]` / `Research_Comparison_*`

## Demo script (defense)

1. `4` Hybrid  
2. Optional `I` Ideal  
3. `Space` — watch GATE + graphs + step strip  
4. `T` trajectory overview  
5. `E` export pack  
6. `P` Monte-Carlo compare  

## Limits (honest)

Not industrial avionics: no Kalman/INS, CFD thrusters, or flight software certification path.  
Sufficient for МКР as a reproducible GNC research simulator with full thesis export.

## How to run

1. Open project in Unity **6000.x** (URP)  
2. `Assets/Scenes/SampleScene.unity` → **Play**  
3. See `README.md` / `DOCS.md` for hotkeys and architecture  

## Version

- App / bundle: **1.0.0**  
- Tag: `v1.0.0`
