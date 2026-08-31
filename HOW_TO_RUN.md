# Betelgeuse — як запустити (захист / демо)

## Unity Editor

1. Встановити **Unity 6000.x** з **URP**.
2. Відкрити теку проєкту `Betelgeuse`.
3. Сцена: `Assets/Scenes/SampleScene.unity` → **Play**.
4. Splash: **Earth LZ** + **1-й ступінь** (booster; `skipStackPhase=true`).

## Демо для захисту (одним натиском)

| Крок | Дія |
|------|-----|
| 1 | **`D`** або **ДЕМО ЗАХИСТУ** |
| 2 | Авто: Hybrid Neuro-Fuzzy · **посадка 1-го ступеня** (Ideal IC) |
| 3 | Після touchdown: огляд траєкторії |
| 4 | **`E`** — експорт (Earth LZ · first stage · after sep) |
| 5 | **`P`** — Monte-Carlo A–D **лише на Stage-1** (DefenseBaseline v5) |

Довідка: **F1** — об'єкт роботи: посадка першого ступеня · Earth LZ · NAV IMU/GPS.

## Ideal (стабільний soft-landing)

**`I`** → IC посадки Stage-1, вітер/шум OFF, NAV NoiseScale=0 → **Space**.

## Відтворюваний Monte-Carlo

**DefenseBaseline** при **P**:

- seed **42** · N **15** · paired seeds · protocol v5  
- Hybrid residual **ON**  
- лише **landing after separation**  
- вітер/шум на Stage-1; NAV noise scale 0.2 якщо enableNoise  

Результат: `SimulationLogs/Comparison_*/01_SUMMARY.md`  
(старі «місячні» звіти **не** цитувати)

### Ablation

Residual OFF (чекбокс Hybrid residual) → Hybrid ≈ Fuzzy-only → повторити **P**.

## Критерії soft-landing

|Vᵧ| &lt; 3.5 м/с · нахил &lt; 7° · промах &lt; 40 м · |Vₕ| &lt; 6.5 м/с

## Гарячі клавіші

`1–4` режим · `Space` старт · `D` демо Hybrid · `I` ідеал Stage-1 · `P`/`X` MC · `E`/`O` експорт · `F1` help · `Y` тема UI · `G` мова
