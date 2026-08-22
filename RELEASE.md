# Betelgeuse v1.1.0 — Release notes

**Date:** 2026-08-22  
**Status:** Diploma-ready GNC simulator  
**Unity:** 6000.x URP  

## Theme coverage

| Requirement | Deliverable |
|-------------|-------------|
| Autonomous first-stage landing | RK4 + soft-landing criteria (`LandingCriteria`) |
| Fuzzy logic | Zero-order Sugeno 5×5 |
| Machine learning | MLP 5→8→2 + ES(1+λ) |
| Hybrid intelligent system | Neuro-Fuzzy residual (cap before blend) |
| Comparison research | Monte-Carlo + ResearchExporter |
| Demo presentation | Mission Control HUD UA/EN · 8 themes · 3D |

## v1.1.0 highlights

### Architecture
- **Domain/Control** layer: `ILandingController`, `ControlContext` / `ControlCommand`, `LandingControllerResolver`, `PidLandingStrategy`, `LandingCriteria`
- `RocketPhysics` dispatches via Strategy registry (no mode-type `switch`)
- SOLID-oriented GNC path documented in `ARCHITECTURE.md`

### Visuals
- Procedural lunar disk: smooth circular craters, cool-gray albedo, no external tile seams
- Falcon-class booster presentation (skins, fins, legs, nozzles)
- Balanced lighting (sun + fill/rim) for white airframe on dark regolith

### HUD
- Compact landing result modal (score pill + 4 metric chips + one button row)
- Phase step strip with status accent color
- Tighter left/right panel rhythm; theme-aware chrome

### Cleanup
- Removed Unity `_Recovery` scenes, unused NASA source textures, dead `MoonSurfaceAssets`, root test logs, offline texture tools

## Verdict

**Thesis match: YES** · **Presentation-ready: YES**  
(Fuzzy + ML + Hybrid + autonomous landing + Monte-Carlo + 3D/HUD.)

## Demo script (defense)

1. `4` Hybrid  
2. `I` Ideal (recommended live)  
3. `Space` — GATE + graphs + step strip + result modal  
4. `T` trajectory overview  
5. `E` export pack  
6. `P` Monte-Carlo compare  
7. Optional: enable **Train**, run Neural — show ES learning

## Limits (honest)

Not industrial avionics: no Kalman/INS, CFD thrusters, or flight-software certification.  
Sufficient for МКР as a reproducible GNC research simulator with full thesis export.

## How to run

1. Open in Unity **6000.x** (URP)  
2. `Assets/Scenes/SampleScene.unity` → **Play**  
3. See `README.md` / `DOCS.md` / `ARCHITECTURE.md`  

## Version

- App / docs: **1.1.0**  
- Prior diploma tag baseline: `v1.0.0` (2026-08-15)
