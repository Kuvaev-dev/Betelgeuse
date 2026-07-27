# Betelgeuse — Intelligent Autonomous Rocket Landing

**Тема:** Розроблення інтелектуальної системи автономної посадки ракетоносія на основі нечіткої логіки та машинного навчання.

Unity (URP) + C# симулятор GNC посадки першого ступеня з порівнянням класичного та інтелектуальних алгоритмів керування.

## Режими керування

| Режим | Метод | Опис |
|--------|--------|------|
| **PID** | Класичний | Soft-landing профіль `v = −√(2ah)` + PID по вертикалі; PID по тангажу/ рисканню |
| **Fuzzy** | **Zero-order Sugeno (TSK-0)** | База правил 5×5 (висота × \|Vᵧ\|), AND=product, дефазифікація — зважене середнє; окремий fuzzy-канал gimbal |
| **Neural** | MLP 5→8→2 + **ES (1+λ)** | Еволюційна оптимізація ваг за cost посадки (V, кут, паливо, промах) |
| **Hybrid** | **Neuro-Fuzzy** | Sugeno-база + обмежений residual MLP (тема диплому) |

> У коді fuzzy — саме **Sugeno 0-order**, не Mamdani (немає центроїда вихідних MF).

## Фізика

- Трансляція: **RK4**
- Орієнтація: semi-implicit Euler + демпфінг
- Гравітація: inverse-square (`AtmosphereModel`)
- Атмосфера: експоненційна густина, drag Cd·A
- Витрата палива: `ṁ = F / (Isp · g₀)`
- Вітер: початковий kick + безперервний air-relative drag (Monte-Carlo)
- Таймаут: `maxSimulationTime` (захист від зависання)

## Критерії успішної посадки

- \|Vᵧ\| &lt; **3.5** м/с  
- кут нахилу &lt; **7°**  
- горизонтальний промах &lt; **25** м  
- \|V_horiz\| &lt; **5** м/с  
- без timeout  

Оцінка `SuccessScore` 0…100: швидкість, кут, паливо, промах, бокова швидкість.

## Як запустити

1. Відкрити проєкт у **Unity 6000.x** (URP).
2. Сцена: `Assets/Scenes/SampleScene.unity`.
3. Play → кнопки **Run PID / Fuzzy / Neural / Full Test** на Experiment Dashboard.
4. Або на `SimulationManager` увімкнути `runFullExperiment`.

На об’єкті **Rocket** мають бути компоненти:

- `RocketPhysics`, `DataLogger`
- `FuzzyLandingController`, `NeuralController`
- `HybridController` (додається автоматично, якщо відсутній)

## Результати

- `SimulationLogs/Landing_*.csv` — траєкторії  
- `SimulationLogs/Final_Comparison_*.csv` — зведена таблиця алгоритмів  
- `BestWeights_Neural.json` — елітні ваги MLP (корінь проєкту)

## Структура `Assets/Scripts`

```
Control/   PIDController, FuzzyLandingController, NeuralController, HybridController
Core/      RocketPhysics, RocketState, AtmosphereModel, LandingMetrics,
           SimulationManager, DataLogger
Parameters/ SimulationParameters (+ LandingParams.asset)
UI/        TelemetryHUD, ExperimentDashboard, TrajectoryVisualizer, MissionControlTheme
Utils/     CameraFollow
```

## UI (Mission Control)

Повний runtime HUD (`MissionControlUI`) збирається автоматично при Play:

```
┌ Top bar: BETELGEUSE GNC · MODE · T+ · STATUS ──────────────┐
│ Left telemetry + live charts │  3D view   │ Right controls │
│ ALT VEL THR TILT FUEL MISS   │  (camera)  │ PID/Fuzzy/NN/  │
│ graphs: alt / vel / thrust   │  rocket    │ Hybrid, MC,    │
│                              │  + pad     │ settings, %    │
└────────────────────────────────────────────────────────────┘
```

- 3D: ракета ~42 м, pad, сітка, висотні маркери, туман (`RocketVisualBuilder` + `EnvironmentBuilder`)
- Камера: `CameraFollow` з адаптивним zoom
- Старий scene-UI (усі кнопки в центрі) ховається автоматично

## Автор

Магістерська кваліфікаційна робота, 2026.
