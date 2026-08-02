# Betelgeuse — Intelligent Autonomous Rocket Landing

**Тема:** Розроблення інтелектуальної системи автономної посадки ракетоносія на основі нечіткої логіки та машинного навчання.

Unity (URP) + C# симулятор GNC посадки першого ступеня з порівнянням класичного та інтелектуальних алгоритмів керування.

> **Повна документація:** [`DOCS.md`](DOCS.md)

## Швидкий старт

1. Unity **6000.x** (URP) → `Assets/Scenes/SampleScene.unity` → **Play**
2. Справа: оберіть алгоритм (**D Гібрид** рекомендовано)
3. Натисніть **«ЗАПУСТИТИ ПОСАДКУ»**
4. Або **«ПОРІВНЯТИ ВСІ»** для Monte-Carlo експерименту
5. **«ЕКСПОРТУВАТИ РЕЗУЛЬТАТИ»** → CSV + JSON + Markdown у `SimulationLogs/`

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
| **Esc** | Стоп / закрити результат |
| **H** | Сховати / показати панелі |
| **F / T / C / R** | Follow / Overview / Manual / Reset cam |
| **L** | Лінія траєкторії |
| **E / O** | Експорт / відкрити папку звітів |
| **G** | Мова UA ↔ EN |
| **P / X** | Порівняти всі / скасувати |

## Камера

| Дія | Керування |
|-----|-----------|
| Orbit навколо ракети | **ЛКМ** / **ПКМ** · **WASD** · **стрілки** · **Q/E** |
| Зум | **Колесо** · **+/-** |
| Слідкувати | **F** |
| Повна траєкторія | **T** |
| Скинути | **R** |

## Критерії успіху

|Vᵧ| < 3.5 м/с · нахил < 7° · промах < 25 м · |Vₕ| < 5 м/с

## Структура

```
Control/   PID, Fuzzy (Sugeno), Neural (ES), Hybrid
Core/      RocketPhysics (RK4), SimulationManager, ResearchExporter, metrics, logger
Visual/    Rocket + Engine FX + Space environment
UI/        MissionControlUI, TelemetryGraph
Utils/     CameraFollow (Follow/Manual/Overview), SceneBootstrap
Tests/     EditMode + PlayMode (Unity Test Framework)
```

## Результати / експорт

Після посадки або авто-тесту:

| Шлях | Зміст |
|------|--------|
| `SimulationLogs/Landing_Full_*/` | **Повний пакет**: CSV кроків + SVG графіки + MD/JSON |
| `…/01_step_calculations.csv` | Покрокові розрахунки GNC |
| `…/03–07_*.svg` | Графіки траєкторії / швидкості / тяги |
| `SimulationLogs/Research_Comparison_*` | Monte-Carlo |
| `BestWeights_Neural.json` | Ваги MLP |

## Тести

У Unity: **Window → General → Test Runner**

- **EditMode** — PID, атмосфера, метрики, експорт, fuzzy
- **PlayMode** — інтеграція фізики, контролерів, камери, логера

## Ключові алгоритмічні гарантії

- **SoftLandingGuidance** — спільна база для A/B/C/D (профіль v=−√(2ah) + PD)
- **RK4** узгоджений з `Time.fixedDeltaTime`
- **Gimbal** — негативний FB + rate damp
- **Lateral guidance** до pad (h &lt; 1200 м)
- **Hybrid** = profile + Sugeno + residual MLP
- **UI**: `UiTypography` — читабельний TMP + контрастна палітра
- Старт: h≈2200 м, Vᵧ≈−85 м/с, крен 3°

## Автор

Магістерська кваліфікаційна робота, 2026.
