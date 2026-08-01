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

## Камера

| Дія | Керування |
|-----|-----------|
| Orbit навколо ракети | **ЛКМ** / **ПКМ** · **WASD** · **стрілки** · **Q/E** |
| Зум | **Колесо** · **+/-** |
| Слідкувати | кнопка / **F** |
| Повна траєкторія | кнопка / **T** |
| Лінія траєкторії on/off | кнопка на правій панелі |
| Мова UA / EN | кнопка **🌐** зверху |
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

## Автор

Магістерська кваліфікаційна робота, 2026.
