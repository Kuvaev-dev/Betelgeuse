# Результати (v2.0)

| Файл | Зміст |
|------|-------|
| `training_neural.png`, `training_hybrid.png` | Криві навчання ES(1+12): вартість на міні-батчі, на валідації, σ |
| `robustness_wind.png` / `.csv` | Успішність A–D від швидкості вітру (N = 300, seed 42, paired; ±8 %, ±8°, ±25 м, шум NAV, h₀ 1800 м, \|Vy₀\| 72 м/с) |
| `robustness_wind_untrained.csv` | Те саме з нейтральною (ненавченою) MLP |

Відтворити: `Tools\HeadlessSim\run.cmd robustness --n 300 --winds 0,3,6,9,12,14,15,16,17,18,20 --mass 8 --angle 8 --jitter 25 --h 1800 --vy 72 --tilt 3.5 --csv Results\robustness_wind.csv`
(додати `--noweights` для ненавченої мережі). Журнали навчання — `BestWeights_*_training_log.csv` у корені проєкту.
