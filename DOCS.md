# Betelgeuse — Документація проєкту

**Повна назва теми:**  
Розроблення інтелектуальної системи автономної посадки ракетоносія на основі нечіткої логіки та машинного навчання.

**Платформа:** Unity 6000.x (URP) · C#  
**Тип:** симулятор GNC (Guidance, Navigation & Control) першого ступеня ракетоносія

---

## 1. Призначення

Програма моделює автономну посадку першого ступеня (клас Falcon 9 / New Glenn scale, спрощена модель ~42 м) і порівнює чотири алгоритми керування:

| Код | Алгоритм | Роль у роботі |
|-----|----------|----------------|
| **A** | Класичний **PID** | Базовий еталон |
| **B** | **Нечітка логіка** (zero-order Sugeno / TSK-0) | Інтелектуальне керування на правилах |
| **C** | **Нейромережа** MLP + еволюційна стратегія ES(1+λ) | Машинне навчання |
| **D** | **Гібрид Neuro-Fuzzy** | ★ Тема диплому: Sugeno + residual MLP |

Мета експерименту — показати, що гібридний підхід (нечітка база + обмежена нейро-корекція) забезпечує стійку м’яку посадку за наявності збурень (вітер, варіація маси, кут).

---

## 2. Швидкий старт

1. Відкрийте проєкт у **Unity 6000.x** з Universal Render Pipeline.
2. Відкрийте сцену `Assets/Scenes/SampleScene.unity`.
3. Натисніть **Play**.
4. У **правій панелі**:
   - **Крок 1** — оберіть алгоритм (рекомендовано **D Гібрид**).
   - **Крок 2** — натисніть **«ЗАПУСТИТИ ПОСАДКУ»**.
   - Спостерігайте 3D-спуск у центрі та телеметрію зліва.
5. Для порівняння всіх методів: **«ПОРІВНЯТИ ВСІ (авто-тест)»**.

Ракета на старті **чекає** кнопки запуску (не падає одразу). Камера автоматично тримає апарат у кадрі.

---

## 3. Інтерфейс користувача

```
┌─────────────── BETELGEUSE · статус · час · алгоритм ───────────────┐
│ ЛІВОРУЧ (scroll)     │     ЦЕНТР (3D)      │ ПРАВОРУЧ (scroll)     │
│ Телеметрія + бари    │  Космос, ракета,   │ 1. Алгоритм A/B/C/D   │
│ Графіки live:        │  pad, траєкторія   │ 2. Запуск / порівняння│
│  min/max/current     │  ПКМ orbit · zoom  │ Камера Follow/Manual  │
│  поріг V=3.5         │                    │ Експорт CSV/JSON/MD   │
│                      │                    │ 3. Умови + % успіху   │
└──────────────────────┴────────────────────┴───────────────────────┘
│ Критерії: |Vy|<3.5 · нахил<7° · промах<25м · |Vh|<5 · SimulationLogs │
└────────────────────────────────────────────────────────────────────┘
```

### Статуси

| Статус | Значення |
|--------|----------|
| **ОЧІКУВАННЯ** | Обрано режим, спуск ще не почався |
| **СТАРТ / СПУСК** | Симуляція активна |
| **ПОСАДКА УСПІШНА** | Усі критерії виконані |
| **ПОСАДКА НЕВДАЛА** | Перевищення швидкості/кута/промаху або timeout |

### Критерії успішної посадки

- Вертикальна швидкість \|Vᵧ\| < **3.5 м/с**
- Кут нахилу корпусу < **7°**
- Горизонтальний промах < **25 м**
- Бічна швидкість \|Vₕ\| < **5 м/с**
- Без перевищення `maxSimulationTime`

Оцінка **SuccessScore** (0…100): комбінація швидкості, кута, залишку палива, промаху, бічної швидкості.

---

## 4. Архітектура коду

```
Assets/Scripts/
├── Control/          # Алгоритми GNC
│   ├── PIDController.cs
│   ├── FuzzyLandingController.cs   # Sugeno-0, 5×5
│   ├── NeuralController.cs         # MLP 5→8→2 + ES
│   └── HybridController.cs         # Neuro-Fuzzy blend
├── Core/
│   ├── RocketPhysics.cs            # RK4 + орієнтація + режими
│   ├── RocketState.cs
│   ├── AtmosphereModel.cs           # g(h), ρ(h)
│   ├── LandingMetrics.cs
│   ├── SimulationManager.cs        # Monte-Carlo порівняння
│   ├── ResearchExporter.cs         # CSV / JSON / Markdown звіти
│   └── DataLogger.cs               # CSV траєкторій
├── Parameters/
│   ├── SimulationParameters.cs
│   └── LandingParams.asset
├── UI/
│   ├── MissionControlUI.cs         # Головний runtime HUD + експорт + камера
│   ├── TelemetryGraph.cs           # Live-графіки (min/max/current/поріг)
│   ├── TrajectoryVisualizer.cs
│   ├── ExperimentDashboard.cs      # Legacy bridge
│   ├── MissionControlTheme.cs
│   └── TelemetryHUD.cs             # Legacy (вимикається)
├── Visual/
│   ├── RocketVisualBuilder.cs      # 3D модель ~42 м
│   ├── RocketEngineFX.cs           # Полум’я / дим / іскри
│   ├── EnvironmentBuilder.cs       # Космос + pad
│   ├── VisualMaterials.cs          # URP матеріали
│   └── SpaceAmbience.cs            # Легка анімація середовища
└── Utils/
    ├── SceneBootstrap.cs           # Auto-setup після Load
    └── CameraFollow.cs             # Follow / Manual orbit / Overview

Assets/Tests/
├── EditMode/   # PID, Atmosphere, Metrics, Export, Fuzzy
└── PlayMode/   # Фізика, контролери, камера, логер
```

### Життєвий цикл запуску (Play)

1. `SceneBootstrap` (AfterSceneLoad) — контролери, візуал, середовище, камера.
2. `MissionControlUI` — будує HUD, ховає старий scene-UI.
3. `RocketPhysics.Start` — ініціалізує стан; **`simulationArmed = false`**.
4. Користувач → **ЗАПУСТИТИ** → `ResetSimulation()` → `simulationArmed = true`.
5. `FixedUpdate` — RK4 + контролер до touchdown / timeout.
6. `LandingMetrics` + CSV + (для NN/Hybrid) крок ES-навчання.

---

## 5. Фізична модель

| Аспект | Реалізація |
|--------|------------|
| Трансляція | **RK4**, крок `fixedTimeStep` (типово 5 мс) |
| Орієнтація | Semi-implicit Euler + демпфінг |
| Гравітація | Inverse-square через `AtmosphereModel.GetGravity` |
| Атмосфера | Експоненційна густина, drag Cd·A |
| Паливо | ṁ = F / (Isp · g₀) |
| Вітер | Початковий kick + безперервний air-relative drag |
| Тяга | Вектор gimbal у тілі ракети, важіль ~16 м |

### Типові параметри (`LandingParams`)

- Старт: h ≈ **2500 м**, Vᵧ ≈ **−100 м/с**, невеликий початковий крен
- Суха маса ~25.6 т, паливо ~14 т
- Макс. тяга ~845 кН, Isp ~311 с

---

## 6. Алгоритми керування

### 6.1 PID (еталон)

- Вертикаль: soft-landing профіль `v_target = −√(2 a h)` + PID на помилку швидкості + компенсація ваги.
- Тангаж / рискання: окремі PID → обмежений gimbal (±28°).

### 6.2 Fuzzy — Zero-order Sugeno (TSK-0)

- **Не Mamdani** (немає центроїда вихідних MF).
- Фазифікація: 5 функцій належності (трикутні/трапеції) для висоти та \|Vᵧ\|.
- База правил **5×5**, AND = **product**.
- Дефазифікація: **зважене середнє** чітких консеквентів (множник до mg).
- Окремий fuzzy-канал gimbal: \|кут\| × \|ω\| → кут відхилення тяги.
- Біля землі (<25 м) — м’яка корекція під soft-landing профіль.

### 6.3 Neural — MLP + ES(1+1)

- Архітектура: **5 → 8 → 2** (tanh hidden, linear out).
- Входи (нормовані): висота, вертикальна швидкість, маса, нахил, \|V_horiz\|.
- Виходи: множник тяги, bias gimbal.
- **Gimbal:** негативний зворотний зв’язок `−k·error` (+ обмежений нейро-bias), узгоджено з Fuzzy/PID.
- Біля землі (&lt;30 м) — soft-landing профіль як у Fuzzy.
- Навчання: ES(1+1) — мутація ваг від еліти після епізоду; cost = f(V_touch, кут, паливо, промах).
- Ваги: `BestWeights_Neural.json` (корінь проєкту).

### 6.4 Hybrid Neuro-Fuzzy ★

```
thrust = clamp( lerp(fuzzy, neural, α(h)), fuzzy ± residualMax )
gimbal = lerp(fuzzyGimbal, neuralGimbal, β(h))
```

- α ≈ 0.20 (thrust), β ≈ 0.15 (gimbal); при h&lt;40 м α,β → 0 (пріоритет fuzzy).
- Residual обмежений (`maxResidualMult` ≈ 0.30 · mg).
- Центральна ідея теми: **інтерпретована нечітка логіка + адаптивне ML**.

### 6.5 Спільне бічне наведення (усі режими)

У `RocketPhysics.ApplyLateralGuidance` (h &lt; 1200 м, tilt &lt; 35°):

```
gx ≈ +k·z + c·vz ,   gz ≈ −k·x − c·vx
```

малий bias gimbal спрямовує апарат до центру pad.

---

## 7. Експерименти Monte-Carlo

`SimulationManager` послідовно ганяє N запусків для PID → Fuzzy → Neural → Hybrid:

- Випадковий вітер, варіація палива, збурення кута.
- Прискорений `timeScale` під час batch.
- Підсумок у UI (% успіху) і CSV:

`SimulationLogs/Final_Comparison_YYYYMMDD_HHMMSS.csv`

Траєкторії окремих посадок: `SimulationLogs/Landing_*.csv`.

---

## 8. Візуалізація

| Компонент | Опис |
|-----------|------|
| `RocketVisualBuilder` | Корпус, stripes, grid fins, ноги, 9 сопел |
| `RocketEngineFX` | Полум’я, дим, іскри, point light ∝ тяга |
| `EnvironmentBuilder` | Зоряне небо, nebula, нічний pad, approach lights |
| `CameraFollow` | Слідкує за `state.position`, mid-body focus, snap при рестарті |
| `TrajectoryVisualizer` | Лінія траєкторії (cyan → green/red) |

Усе збирається **процедурно в runtime** (без обов’язкових prefab-моделей) — зручно для відтворюваності дипломної демонстрації.

---

## 9. Камера (поведінка)

Три режими (`CameraFollow.ViewMode`):

| Режим | Опис |
|-------|------|
| **Follow** | Авто-слідкування за ракетою (за замовчуванням) |
| **Manual** | Orbit-камера: ПКМ обертання, колесо зум |
| **Overview** | **Повна траєкторія**: позиція, з якої видно старт + шлях + pad |

Кнопка **«ПОВНА ТРАЄКТОРІЯ (старт→pad)»** / клавіша **T** миттєво ставить камеру
в оптимальний діагональний ракурс (`SnapToFullTrajectoryView`). Кадр автоматично
розширюється під час польоту. У цьому режимі ПКМ/колесо **не** виходять у Manual.

### Керування

| Дія | Миша / клавіші |
|-----|----------------|
| Перетягнути (pan) | **ЛКМ** drag · **WASD** · **СКМ** |
| Обертання (orbit) | **ПКМ** drag · **Q/E** · **←→** |
| Нахил | **↑↓** |
| Зум | **Колесо миші** · **+/-** |
| Слідкувати | UI-кнопка · **F** |
| Ручне | UI-кнопка · **C** |
| Повна траєкторія | UI-кнопка · **T** |
| Скинути ракурс / pan | UI-кнопка · **R** |

Камера обмежена сферою сцени (`worldBoundRadius` ≈ 4500 м) — не вилітає «в нікуди».
Товщина лінії траєкторії масштабується відстанню до камери (видно здалеку).

Технічні деталі Follow:

- Джерело позиції: **`RocketPhysics.state`**, не «відсталий» Transform.
- Фокус: центр корпусу (~18 м над соплами) + look-ahead за швидкістю.
- Adaptive distance: ближче біля землі, далі на висоті; коліщатко масштабує `followDistanceMul`.
- При `Reset` / зміні режиму — **миттєвий SnapNow**.

---

## 10. Файли результатів і експорт

Кнопка **«ЕКСПОРТУВАТИ РЕЗУЛЬТАТИ»** (та автопісля посадки) створює **повний пакет**:

```
SimulationLogs/Landing_Full_<algo>_<timestamp>/
  00_REPORT.md                 — звіт + відповідність темі
  01_step_calculations.csv     — покрокові розрахунки (стан, T/W, gimbal…)
  02_summary.json              — машиночитаний підсумок
  03_altitude_vs_time.svg      — графік h(t)
  04_trajectory_XZ.svg         — ground track
  05_velocity_vs_time.svg      — Vy(t)
  06_thrust_vs_time.svg        — F(t)
  07_trajectory_side_XY.svg    — бічний профіль
  08_step_analysis.md          — екстремуми + формули моделі
```

Також дзеркаляться `Landing_Report_*.{csv,json,md}` у корені `SimulationLogs/`.
Monte-Carlo: `Research_Comparison_*` + `Final_Comparison_*.csv`.

Клас: `ResearchExporter` + `DataLogger` (збагачений семпл).

---

## 11. Вимоги та обмеження

- Unity **6000.x** + **URP**.
- TextMesh Pro (входить у шаблон проєкту).
- Input System (UI EventSystem підхоплює автоматично).
- Модель **спрощена** (не CFD, не повний 6-DOF flight software): достатня для порівняння GNC-алгоритмів у магістерській роботі.
- Процедурні примітиви ≠ AAA-ассети; якість досягається освітленням, FX і композицією.

---

## 12. Відповідність темі диплому

| Формулювання теми | Де в проєкті |
|-------------------|--------------|
| Автономна посадка ракетоносія | `RocketPhysics` + критерії soft-landing |
| Нечітка логіка | `FuzzyLandingController` (Sugeno-0) |
| Машинне навчання | `NeuralController` (MLP + ES) |
| Інтелектуальна система (поєднання) | `HybridController` Neuro-Fuzzy |
| Дослідження / порівняння | `SimulationManager` Monte-Carlo + CSV |

Режим за замовчуванням: **Hybrid**.

---

## 13. Типові сценарії демонстрації

1. **Один красивий спуск:** D Гібрид → ЗАПУСТИТИ → показати телеметрію і touchdown.
2. **Порівняння методів:** ПОРІВНЯТИ ВСІ → показати % і переможця.
3. **Стрес-тест:** підняти «Сила вітру», увімкнути збурення → знову порівняти.
4. **Навчання NN:** багато одиночних запусків у режимі C з увімкненим навчанням → `BestWeights_Neural.json` оновлюється.

---

## 14. Тести

Unity Test Framework (`com.unity.test-framework`).

**Window → General → Test Runner**

| Набір | Що перевіряє |
|-------|----------------|
| EditMode | PID anti-windup, AtmosphereModel, LandingMetrics/SuccessScore, ResearchExporter (CSV/JSON/MD), Fuzzy thrust/gimbal, RocketState |
| PlayMode | Disarmed hold, PID landing finish, Fuzzy/Hybrid NaN-free, CameraFollow modes, DataLogger save, PrepareMode |

Запуск з CLI (коли проєкт не відкритий в Editor):

```text
Unity.exe -batchmode -nographics -projectPath <path> -runTests -testPlatform EditMode -testResults results.xml
```

---

## 15. Закриття теми (checklist)

Тема: *Розроблення інтелектуальної системи автономної посадки ракетоносія на основі нечіткої логіки та машинного навчання.*

| Вимога теми | Статус | Де |
|-------------|--------|-----|
| Модель посадки ракетоносія | ✅ | `RocketPhysics` RK4 + `SoftLandingGuidance` + lateral GNC |
| Нечітка логіка | ✅ | `FuzzyLandingController` Sugeno-0 поверх soft-landing |
| Машинне навчання | ✅ | `NeuralController` MLP residual + ES, база = soft-landing |
| Інтелектуальна гібридна система | ✅ | `HybridController` Neuro-Fuzzy (profile + fuzzy + NN) |
| Порівняння / дослідження | ✅ | `SimulationManager` Monte-Carlo |
| Експорт результатів і графіків | ✅ | `ResearchExporter` CSV/JSON/MD/SVG |
| Демонстраційний UI | ✅ | `MissionControlUI` UA/EN + hotkeys + Segoe UI SDF |
| Візуалізація космосу / апарата | ✅ | мінімальний void + industrial pad + rocket FX |
| Тести | ✅ | EditMode + PlayMode (GNC sign, фізика, fuzzy, metrics) |

**Рекомендована демо-послідовність для захисту:**  
1) D Гібрид → ЗАПУСТИТИ → показати телеметрію/Δ.  
2) ЛКМ orbit навколо апарата.  
3) T — повна траєкторія.  
4) Експорт → відкрити `00_REPORT.md` + SVG.  
5) ПОРІВНЯТИ ВСІ → таблиця % успіху.

---

## 16. Автор

Магістерська кваліфікаційна робота, 2026.  
Проєкт: **Betelgeuse**.
