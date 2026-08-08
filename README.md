# Betelgeuse — Intelligent Autonomous Rocket Landing

**Тема:** Розроблення інтелектуальної системи автономної посадки ракетоносія на основі нечіткої логіки та машинного навчання.

Unity (URP) + C# симулятор GNC посадки першого ступеня з порівнянням класичного та інтелектуальних алгоритмів керування.

> **Повна документація:** [`DOCS.md`](DOCS.md)

## Швидкий старт

1. Unity **6000.x** (URP) → `Assets/Scenes/SampleScene.unity` → **Play**
2. Справа: оберіть алгоритм (**D Гібрид** рекомендовано)
3. (Опційно) **«Ідеал»** `[I]` — номінал без вітру/шуму
4. **«Старт»** `[Space]` — запуск посадки
5. Або **«Порівняти»** `[P]` — Monte-Carlo для всіх режимів
6. **«Експорт»** `[E]` → CSV + JSON + Markdown + SVG у `SimulationLogs/`

## Режими керування

| | Режим | Метод |
|---|--------|--------|
| A | **PID** | Soft-landing `v=−√(2ah)` + PID |
| B | **Fuzzy** | Zero-order **Sugeno** 5×5 |
| C | **Neural** | MLP 5→8→2 + **ES (1+λ)** |
| D | **Hybrid** ★ | Neuro-Fuzzy (тема роботи) |

## Гарячі клавіші

| Клавіша | Дія |
|---------|-----|
| **1 / 2 / 3 / 4** | PID / Fuzzy / Neural / Hybrid |
| **Space** | Запустити посадку |
| **I** | Ідеальні параметри |
| **Esc** | Стоп / закрити результат |
| **H** | Сховати / показати панелі |
| **F / T / C / R** | Follow / Overview / Manual / Reset cam |
| **L** | Лінія траєкторії (on/off) |
| **E / O** | Експорт / відкрити папку звітів |
| **G** | Мова UA ↔ EN |
| **Y** | Тема UI (Dark / Cyan / Amber / Light / …) |
| **P / X** | Порівняти всі / скасувати |

## Камера

| Дія | Керування |
|-----|-----------|
| Orbit | **ЛКМ** / **ПКМ** · **WASD** · **стрілки** · **Q/E** |
| Зум | **Колесо** · **+/-** |
| Слідкувати | **F** |
| Повна траєкторія | **T** |
| Скинути | **R** |

## Критерії успіху

|Vᵧ| < 3.5 м/с · нахил < 7° · промах < 25 м · |Vₕ| < 5 м/с

## Структура

```
Control/   PID, Fuzzy (Sugeno), Neural (ES), Hybrid, SoftLandingGuidance
Core/      RocketPhysics (RK4), SimulationManager, ResearchExporter, metrics, logger
Visual/    Rocket + Engine FX + LunarTerrain + Space environment
UI/        MissionControlUI, TelemetryGraph, TrajectoryVisualizer, UiTheme
Utils/     CameraFollow, SceneBootstrap
Tests/     EditMode + PlayMode
```

## Результати / експорт

| Шлях | Зміст |
|------|--------|
| `SimulationLogs/Landing_Full_*/` | Повний пакет: CSV + SVG + MD/JSON |
| `…/01_step_calculations.csv` | Покрокові розрахунки GNC |
| `…/03–07_*.svg` | Графіки траєкторії / швидкості / тяги |
| `SimulationLogs/Research_Comparison_*` | Monte-Carlo |
| `BestWeights_Neural.json` | Ваги MLP |

## Тести

Unity: **Window → General → Test Runner**

- **EditMode** — PID, атмосфера, метрики, експорт, fuzzy
- **PlayMode** — фізика, контролери, камера, логер

## Ключові гарантії

- **A PID** — hover FF + PID
- **B Fuzzy** — Sugeno 5×5, weight≈0.55
- **C Neural** — MLP residual + ES
- **D Hybrid** — Sugeno + обмежений NN residual (один BlendThrust)
- Спільне: RK4, TVC-PD, lateral, термінал h&lt;25 м → soft-landing
- UI-вітер/шум впливають на одиночний старт (`ApplyFlightDisturbances`)
- **Ідеал `[I]`:** h≈1400 / Vᵧ≈−48 / вітер=0 → очікуваний успіх A–D
- Траєкторія: Chaikin-згладжування, лишається після посадки
- Місяць: crater-disk без порожнього краю, чорні днища кратерів

## Вердикт (тема)

Тема **реалізована**: автономна посадка + нечітка логіка (Sugeno) + ML (MLP/ES) + гібрид Neuro-Fuzzy + порівняльний експеримент + експорт. Рівень — дипломна симуляція GNC (не industrial flight software).

## Автор

Магістерська кваліфікаційна робота, 2026.
