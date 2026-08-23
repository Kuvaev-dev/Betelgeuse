# Betelgeuse — How to run (defense / demo)

## Unity Editor

1. Install **Unity 6000.x** with **URP**.
2. Open project folder `Betelgeuse`.
3. Scene: `Assets/Scenes/SampleScene.unity` → **Play**.
4. Wait for splash (procedural moon + rocket build).

## Defense demo (one click)

| Step | Action |
|------|--------|
| 1 | Press **`D`** or button **ДЕМО ЗАХИСТУ** (right panel) |
| 2 | Auto: Hybrid → Ideal → Start landing |
| 3 | After touchdown: trajectory overview |
| 4 | Optional: **`E`** export landing pack |
| 5 | Optional: **`P`** Monte-Carlo compare (DefenseBaseline, paired) |

Help overlay anytime: **F1** / **?** or top-bar **HELP** (right after Export).

## Reproducible Monte-Carlo

Protocol **`DefenseBaseline` v2** applies automatically on Compare:

- seed **42** · N **15** · wind **8** · jitter **±18 m** · noise **ON**
- **paired seeds** — trial `i` identical for PID / Fuzzy / Neural / Hybrid
- fixed NN weights, training OFF · Hybrid residual ON

1. Press **P** or button **ПОРІВНЯТИ** (UI sliders sync to baseline).
2. Wait for A→D → `SimulationLogs/Comparison_*`.
3. Re-run **P** → same pack statistics (SimRng + paired protocol).

### Ablation (thesis)

Toggle **Hybrid residual NN** OFF → Hybrid becomes Fuzzy-only leave-one-out.  
Re-run Compare and contrast success % / Score±σ in `01_SUMMARY.md`.

## Standalone build (optional)

1. **File → Build Settings → PC, Mac & Linux Standalone** (Windows x86_64).
2. Add `SampleScene`, **Build**.
3. Run the `.exe` — same keys as Editor (no Unity required for commission).

## Soft-landing criteria

|Vy| &lt; 3.5 m/s · tilt &lt; 7° · miss &lt; 25 m · |Vh| &lt; 5 m/s

## Hotkeys (short)

`1–4` mode · `Space` start · `Esc` stop · `I` ideal · `D` demo · `P`/`X` compare · `E`/`O` export · `F1` help · `H` UI · `G` lang · `Y` theme
