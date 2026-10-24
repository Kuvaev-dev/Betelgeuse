# Betelgeuse — як запустити (захист / демо)

## Unity Editor

1. **Unity 6000.x** + **URP**, відкрити теку `Betelgeuse`, дочекатися імпорту.
2. `Assets/Scenes/SampleScene.unity` → **Play**.
3. Splash: **Earth LZ** + **1-й ступінь**. Навчені ваги MLP (`BestWeights_Neural.json`,
   `BestWeights_Hybrid.json` у корені проєкту) завантажуються автоматично — опис видно в правій панелі.

## Демо захисту

| Крок | Дія |
|------|-----|
| 0 | **`F1`** — тема МКР, GNC, методи A–D, обмеження |
| 1 | **`M`** / **ДЕМО** — Hybrid у номінальних умовах |
| 2 | **`1–4`** — кожен метод · **`I`** номінал · **Space** старт |
| 3 | **`B`** — протокол захисту (seed 42) → **`P`** — Monte-Carlo A–D (paired seeds) |
| 4 | Тогл **Hybrid residual** OFF → **`P`** — ablation (D ≡ B) |
| 5 | **`E`** — експорт звіту (`SimulationLogs/`, SVG + CSV + MD) |

## Monte-Carlo (`P`)

- Умови = слайдери правої панелі (вітер, ±маса, ±кут, розкид точки входу, шум, ПУ) або протокол **`B`**.
- Paired seeds: trial `i` однаковий для PID / Fuzzy / Neural / Hybrid; той самий seed дає той самий результат.
- Прогони йдуть у фонових потоках на ядрі симуляції (без рендера) — 100 запусків на алгоритм за секунди.
- «Шум» вмикає розкид точки входу та шум сенсорів навігації (0.35σ).
- Ваги MLP під час пакета заморожені.

## Навчання нейромережі (ES 1+λ)

Будь-який із трьох способів (той самий код `EsTrainer`):

1. **Headless (рекомендовано, повне навчання):**
   ```
   Tools\HeadlessSim\build.cmd
   Tools\HeadlessSim\run.cmd train --mode neural
   Tools\HeadlessSim\run.cmd train --mode hybrid
   ```
   Результат: `BestWeights_*.json` + `BestWeights_*_training_log.csv` (крива навчання) у корені проєкту.
2. **Unity Editor:** меню **Betelgeuse → Train MLP (ES 1+λ) → Both**.
3. **Застосунок:** тогл **Навчання NN (ES 1+λ)** — коротке фонове навчання (80 поколінь на режим),
   по завершенні ваги зберігаються і перезавантажуються.

Інші команди стенда: `run.cmd mc --n 200` (протокол), `run.cmd robustness --csv wind.csv` (успішність від вітру),
`run.cmd ideal`, `run.cmd trace --mode hybrid`, `run.cmd selftest`.
Шлях до Unity задається змінною `UNITY_EDITOR_DATA` (типово `C:\Program Files\Unity\Hub\Editor\6000.4.8f1\Editor\Data`).

## Критерії soft-landing

|Vᵧ| < 3.5 м/с · нахил < 7° · промах < 40 м · |Vₕ| < 6.5 м/с

## Гарячі клавіші

`1–4` режим · `Space` старт · `M` демо · `I` номінал · `B` протокол · `P`/`X` MC / скасувати ·
`E`/`O` експорт / тека · `F` follow · `T` overview · `F1` довідка · `Y` тема · `G` мова · `U` пауза
