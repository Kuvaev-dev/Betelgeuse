# Betelgeuse — Документація проєкту

**Повна назва теми:**  
Розроблення інтелектуальної системи автономної посадки ракетоносія на основі нечіткої логіки та машинного навчання.

**Платформа:** Unity 6000.x (URP) · C#  
**Тип:** симулятор GNC (Guidance, Navigation & Control) першого ступеня  
**Версія:** **v1.1.0** · див. [`RELEASE.md`](RELEASE.md) · архітектура [`ARCHITECTURE.md`](ARCHITECTURE.md)

---

## 1. Призначення

Програма моделює автономну посадку першого ступеня (клас Falcon scale, ~42 м) і порівнює чотири алгоритми:

| Код | Алгоритм | Роль |
|-----|----------|------|
| **A** | Класичний **PID** | Еталон |
| **B** | **Нечітка логіка** Sugeno-0 5×5 | Правила |
| **C** | **Нейромережа** MLP + ES(1+λ) | ML |
| **D** | **Гібрид Neuro-Fuzzy** | ★ Тема: Sugeno + residual MLP |

Мета — показати, що гібрид (інтерпретований Sugeno + обмежена нейро-корекція) стійкіший за чистий PID під збуреннями.

---

## 2. Швидкий старт

1. Unity **6000.x** + URP → `Assets/Scenes/SampleScene.unity` → **Play**.  
2. Права панель: алгоритм **D Гібрид**.  
3. (Опційно) **Ідеал** `[I]`.  
4. **Старт** `[Space]`.  
5. **Порівняти** `[P]` — Monte-Carlo.  
6. **Експорт** `[E]` → `SimulationLogs/`.  

Ракета **чекає** кнопки (не падає одразу).

---

## 3. Інтерфейс

```
┌── BETELGEUSE · mode · t     [Start|Stop|Ideal|Path|View|Export]  Theme Lang ─┐
│                                                         Status Hide          │
├──────────────┬────────────────────────┬──────────────────────────────────────┤
│ ЛІВО:        │  ЦЕНТР 3D              │ ПРАВО:                               │
│ GATE 2×2     │  Місяць + LZ pad       │ Швидкий старт                        │
│ Підказка     │  ракета ~42 м          │ Алгоритм 2×2 (1–4)                   │
│ Головне/dyn  │  траєкторія            │ Порівняти [P] / Скасувати [X]        │
│ Графіки      │  orbit / zoom          │ Камера · Умови (вітер/шум/N/x)       │
│              │                        │ Success % + winner                   │
├──────────────┴────────────────────────┴──────────────────────────────────────┤
│              Крок: Hybrid | термінал / гальмування / …                       │
└──────────────────────────────────────────────────────────────────────────────┘
```

### Модалка результату посадки

- Заголовок + **score pill** (`NN /100`)  
- Один рядок підсумку  
- 4 чіпи метрик (Vᵧ, нахил, промах, Vₕ) з green/red  
- Один ряд кнопок: Траєкторія · Експорт · Зрозуміло  

### Теми UI (`Y`)

Dark · Cyan · Amber · **Light** · Green · Violet · Red · **Ice**

- Rebuild при зміні теми/мови **зберігає** samples strip-charts і setup.  
- Шрифт HUD: dynamic Segoe UI SDF + LiberationSans fallback.

### Критерії soft-landing (`LandingCriteria`)

- |Vᵧ| &lt; **3.5** м/с  
- нахил &lt; **7°**  
- промах &lt; **25** м  
- |Vₕ| &lt; **5** м/с  
- без timeout  

**SuccessScore** 0…100: швидкість 35% + кут 25% + паливо 15% + промах 15% + бічна 10%.

---

## 4. Архітектура (стисло)

```
Assets/Scripts/
├── Domain/Control/     # ILandingController, Context/Command, Resolver, Criteria, PidStrategy
├── Control/            # Fuzzy, Neural, Hybrid, SoftLandingGuidance, IdealLandingPresets
├── Core/               # RocketPhysics, SimulationManager, ResearchExporter, metrics, logger
├── Parameters/         # SimulationParameters
├── Visual/             # LunarTerrainMesh, EnvironmentBuilder, RocketVisualBuilder, FX
├── UI/                 # MissionControlUI, UiTheme, graphs, trajectory, splash
└── Utils/              # SceneBootstrap, CameraFollow, BorderlessWindow
```

### Життєвий цикл Play

1. `SplashScreenUI` → `SceneBootstrap` (контролери, візуал, середовище, камера).  
2. `MissionControlUI` — HUD.  
3. `RocketPhysics.simulationArmed = false`.  
4. Старт → `ResetSimulation` + `ApplyFlightDisturbances`.  
5. `FixedUpdate`: RK4 + `ILandingController.Evaluate` до touchdown.  
6. Metrics + CSV + (NN/Hybrid) ES-крок; траєкторія лишається до нового старту.  

Деталі SOLID/патернів — у [`ARCHITECTURE.md`](ARCHITECTURE.md).

---

## 5. GNC

| Режим | Закон |
|-------|--------|
| **A PID** | Hover FF + PID на `v_target`; слабший terminal / lateral |
| **B Fuzzy** | Sugeno 5×5 product-AND + blend з soft-landing профілем |
| **C Neural** | MLP residual + ES; ваги у `BestWeights_Neural.json` |
| **D Hybrid** | Сирий Sugeno + сирий MLP residual → **один** `BlendThrust`; біля землі α,β→0 |

Спільне:

- Soft-landing профіль `v=−√(2ah)` (`SoftLandingGuidance`)  
- TVC-PD upright safety net  
- Lateral guidance з масштабом від стратегії  
- Термінал h&lt;~25 м  

### Ideal `[I]`

Номінал (орієнтовно h≈1400 м, Vᵧ≈−48 м/с, вітер=0) + per-mode тюнінг → очікуваний soft-landing A–D.

---

## 6. Фізика

- Трансляція: **RK4**  
- Орієнтація: semi-implicit Euler + демпфінг  
- `AtmosphereModel`: g(h), ρ(h)  
- Drag: Cd≈0.85, S≈8.5 м²  
- Вітер: UI → `ApplyFlightDisturbances` на одиночний старт  
- `fixedTimeStep` синхронізується з `Time.fixedDeltaTime`  

---

## 7. Візуалізація

| Компонент | Опис |
|-----------|------|
| `LunarTerrainMesh` | Диск R=2000 м, heightmap, круглі C2-кратери, cool-gray albedo |
| `EnvironmentBuilder` | LZ pad, сонце, зірки, approach markers |
| `RocketVisualBuilder` | Falcon-class ~42 м, UV-skins, fins, ноги, bell nozzles |
| `RocketEngineFX` | Core/outer plume, дим, sparks, dust |
| `TrajectoryVisualizer` | Catmull-Rom + Chaikin, live tip |

---

## 8. Експорт

```
SimulationLogs/Landing_Full_<algo>_<ts>/
  00_REPORT.md · 01_step_calculations.csv · 02_summary.json
  03–07_*.svg · 08_step_analysis.md
```

Monte-Carlo: `SimulationLogs/Research_Comparison_*`.

---

## 9. Тести

| Набір | Зміст |
|-------|--------|
| EditMode | PID, Atmosphere, Metrics, Export, Fuzzy, gimbal/Neural signs, **ThesisCoverage** |
| PlayMode | Hold, landing finish, NaN-free, camera, logger |

---

## 10. Відповідність темі — вердикт

**Тема:** *Розроблення інтелектуальної системи автономної посадки ракетоносія на основі нечіткої логіки та машинного навчання.*

| Фрагмент теми | Реалізація | Де |
|---------------|------------|-----|
| Автономна посадка ракетоносія | GNC без пілота до touchdown | `RocketPhysics`, soft-landing |
| Нечітка логіка | Sugeno-0 5×5 + fuzzy gimbal | `FuzzyLandingController` **B** |
| Машинне навчання | MLP 5→8→2 + ES(1+λ) | `NeuralController` **C**, toggle **Train** |
| Інтелектуальна система | Neuro-Fuzzy (Sugeno + NN residual) | `HybridController` **D** ★ |
| Дослідження / порівняння | Monte-Carlo + export pack | `SimulationManager`, `ResearchExporter` |
| Презентація | 3D + HUD UA/EN + 8 тем | `MissionControlUI`, Visual/* |

| Вимога | Статус |
|--------|--------|
| Автономна посадка | ✅ |
| Нечітка логіка | ✅ |
| Машинне навчання | ✅ (Train off за замовч. — стабільне демо) |
| Гібрид (тема) | ✅ |
| Порівняння + export | ✅ |
| UI / 3D | ✅ |
| SOLID GNC layer | ✅ |

### Готовність до захисту

**Вердикт: ТАК** — логіка відповідає темі; проєкт готовий до повноцінної презентації за демо-сценарієм нижче.

**Межі (чесно):** не industrial avionics (немає Kalman/INS, CFD, сертифікації ПЗ). Рівень МКР: відтворюваний GNC research simulator.

### Демо на захист

1. `4` Hybrid  
2. `I` Ideal (вітер/шум off, ваги NN ideal, Train off)  
3. `Space` — GATE, score, модалка  
4. `T` траєкторія  
5. `E` експорт  
6. `P` Monte-Carlo (A–D)  
7. Опційно: **Train** on + Neural — показати ES  

### Стабільність демо

- `LoadBestWeights` / fallback `InstallIdealWeights`  
- ES-мутація вимкнена за замовчуванням  
- Ideal `[I]` тюнить GNC під обраний режим  

Тести покриття теми: `ThesisCoverageTests` (EditMode).

---

## 11. Автор

Магістерська кваліфікаційна робота, 2026.
