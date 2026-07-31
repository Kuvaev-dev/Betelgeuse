# Betelgeuse — Intelligent Autonomous Rocket Landing

**Тема:** Розроблення інтелектуальної системи автономної посадки ракетоносія на основі нечіткої логіки та машинного навчання.

Unity (URP) + C# симулятор GNC посадки першого ступеня з порівнянням класичного та інтелектуальних алгоритмів керування.

> **Повна документація:** [`DOCS.md`](DOCS.md)

## Швидкий старт

1. Unity **6000.x** (URP) → `Assets/Scenes/SampleScene.unity` → **Play**
2. Справа: оберіть алгоритм (**D Гібрид** рекомендовано)
3. Натисніть **«ЗАПУСТИТИ ПОСАДКУ»**
4. Або **«ПОРІВНЯТИ ВСІ»** для Monte-Carlo експерименту

## Режими керування

| | Режим | Метод |
|---|--------|--------|
| A | **PID** | Soft-landing `v=−√(2ah)` + PID |
| B | **Fuzzy** | Zero-order **Sugeno** 5×5 |
| C | **Neural** | MLP 5→8→2 + **ES (1+λ)** |
| D | **Hybrid** ★ | Neuro-Fuzzy (тема роботи) |

## Критерії успіху

\|Vᵧ\| < 3.5 м/с · нахил < 7° · промах < 25 м · \|Vₕ\| < 5 м/с

## Структура

```
Control/   PID, Fuzzy (Sugeno), Neural (ES), Hybrid
Core/      RocketPhysics (RK4), SimulationManager, metrics, logger
Visual/    Rocket + Engine FX + Space environment
UI/        MissionControlUI (зрозумілий UA-інтерфейс)
Utils/     CameraFollow, SceneBootstrap
```

## Результати

- `SimulationLogs/Landing_*.csv` — траєкторії
- `SimulationLogs/Final_Comparison_*.csv` — порівняння
- `BestWeights_Neural.json` — ваги MLP

## Автор

Магістерська кваліфікаційна робота, 2026.
