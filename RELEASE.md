# Betelgeuse v1.2.0 — Release notes

**Date:** 2026-03-28  
**Status:** Diploma-ready GNC simulator (defense pack)  
**Unity:** 6000.x URP  

## Theme coverage

| Requirement | Deliverable |
|-------------|-------------|
| Autonomous first-stage landing | RK4 + soft-landing criteria (`LandingCriteria`) |
| Fuzzy logic | Zero-order Sugeno 5×5 |
| Machine learning | MLP 5→8→2 + ES(1+λ) |
| Hybrid intelligent system | Neuro-Fuzzy residual (cap before blend) + **ablation toggle** |
| Comparison research | DefenseBaseline v2 paired Monte-Carlo + ResearchExporter (Score ±σ) |
| Demo presentation | Mission Control HUD UA/EN · 8 themes · 3D · Defense Demo · Help |

## v1.2.0 highlights

### Defense / reproducibility
- **`SimRng`** — seeded disturbances (single flight + Monte-Carlo)
- **`DefenseBaseline` v2** — auto-applied on Compare: seed 42, N=15, wind 8, jitter ±18 m, noise ON
- **Paired seeds** — trial `i` identical across PID/Fuzzy/Neural/Hybrid (fair ranking)
- Stronger lateral GNC (scale: PID weak → Hybrid strong) so MC is not universal 0%
- **Defense Demo** — Hybrid → Ideal → Start → overview
- **Help overlay** — hotkeys, criteria, research toggles
- **Hybrid residual ON/OFF** — thesis ablation (leave-one-out NN)
- Export includes seed, jitter, paired flag, protocol version, residual, Score ±σ

### Performance
- Faster cold start: lower lunar mesh/albedo cost, smaller tank skins, no artificial bootstrap delays

### Architecture
- Domain Strategy GNC path unchanged (`ILandingController` / Resolver)

## Defense script (live)

1. **F1** — show help briefly  
2. **D** — Defense Demo (Hybrid Ideal landing)  
3. After result: **T** overview if needed · **E** export  
4. **P** Monte-Carlo (DefenseBaseline paired) · open `SimulationLogs/Comparison_*`  
5. Optional: residual **OFF**, re-run **P** — ablation slide  

See also [`HOW_TO_RUN.md`](HOW_TO_RUN.md).

## Baseline protocol (cite in thesis)

```
DefenseBaseline v1
  seed                  = 42
  testsPerAlgorithm     = 15
  windStrength          = 10 m/s
  massVariationPercent  = 8
  angleVariationDegrees = 8
  positionJitterMeters  = 22
  enableNoise           = true
  hybridResidual        = true
```

**Expected ranking (qualitative under this protocol):**  
`Hybrid ≥ Fuzzy` and `Hybrid ≥ PID` on success % (Score ±σ in export).

After the defense Compare run on the presentation PC, cite the generated pack:

`SimulationLogs/Comparison_<timestamp>/01_SUMMARY.md`

(Do not hard-code machine-specific % here — seed+protocol make the pack the source of truth.)

## Limits (honest)

Not industrial avionics: no Kalman/INS, CFD thrusters, or flight-software certification.  
Sufficient for МКР as a reproducible GNC research simulator with full thesis export.

## How to run

1. Unity **6000.x** (URP) → `Assets/Scenes/SampleScene.unity` → **Play**  
2. Or standalone build — [`HOW_TO_RUN.md`](HOW_TO_RUN.md)  
3. Specs: `README.md` · `DOCS.md` · `ARCHITECTURE.md`

## Version

- App / docs: **1.2.0**  
- Prior: `v1.1.0` (2026-08-22) · `v1.0.0` (2026-08-15)
