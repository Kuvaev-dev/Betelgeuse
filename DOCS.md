# Betelgeuse — Документація проєкту

**Повна назва теми:**  
Розроблення інтелектуальної системи автономної посадки ракетоносія на основі нечіткої логіки та машинного навчання.

**Платформа:** Unity 6000.x (URP) · C#  
**Тип:** симулятор GNC (Guidance, Navigation & Control) першого ступеня ракетоносія  
**Статус:** фінальна версія для захисту (2026)

---

## 1. Призначення

Програма моделює автономну посадку першого ступеня (клас Falcon 9 scale, ~42 м) і порівнює чотири алгоритми:

| Код | Алгоритм | Роль |
|-----|----------|------|
| **A** | Класичний **PID** | Еталон |
| **B** | **Нечітка логіка** Sugeno-0 5×5 | Інтелектуальне керування на правилах |
| **C** | **Нейромережа** MLP + ES(1+λ) | Машинне навчання |
| **D** | **Гібрид Neuro-Fuzzy** | ★ Тема: Sugeno + residual MLP |

Мета — показати, що гібрид (інтерпретований Sugeno + обмежена нейро-корекція) стійкіший за чистий PID під збуреннями.

---

## 2. Швидкий старт

1. Unity **6000.x** + URP → `Assets/Scenes/SampleScene.unity` → **Play**.
2. Права панель: алгоритм **D Гібрид**.
3. (Опційно) **Ідеал** `[I]`.
4. **Старт** `[Space]` (або top-menu).
5. **Порівняти** `[P]` — Monte-Carlo.
6. **Експорт** `[E]` → `SimulationLogs/`.

Ракета **чекає** кнопки (не падає одразу).

---

## 3. Інтерфейс

```
┌── BETELGEUSE · статус · час · алгоритм · Hide · Lang · Theme ──────┐
│ Top-menu: Старт · Ідеал · Стоп · Порівняти · Траєкторія · Огляд · Експорт │
├──────────────┬────────────────────────┬──────────────────────────┤
│ ЛІВО:        │  ЦЕНТР 3D              │ ПРАВО:                   │
│ телеметрія   │  Місяць, pad, ракета   │ 1. Алгоритм A/B/C/D      │
│ критерії     │  згладжена траєкторія  │ 2. Порівняти / скасувати │
│ live-графіки │  orbit / zoom          │ 3. Вітер / шум / N       │
│              │                        │ 4. % успіху              │
└──────────────┴────────────────────────┴──────────────────────────┘
```

### Теми UI (`Y`)

Dark · Cyan · Amber · **Light** · Green · Violet · Red · **Ice**

Світлі теми: світлий chrome top-menu, контрастний ink-текст, theme-aware графіки й слайдери.  
Шрифт: динамічний Segoe UI SDF (кирилиця), underlay/outline для чіткості.

### Критерії soft-landing

- \|Vᵧ\| < **3.5** м/с  
- нахил < **7°**  
- промах < **25** м  
- \|Vₕ\| < **5** м/с  
- без timeout  

**SuccessScore** 0…100: швидкість 35% + кут 25% + паливо 15% + промах 15% + бічна 10%.

---

## 4. Архітектура

```
Assets/Scripts/
├── Control/
│   ├── PIDController.cs              # PID + anti-windup
│   ├── SoftLandingGuidance.cs        # v=−√(2ah), BlendThrust, TVC-PD
│   ├── FuzzyLandingController.cs     # Sugeno-0 5×5 + EvaluateSugenoThrust
│   ├── NeuralController.cs           # MLP 5→8→2 + ES
│   ├── HybridController.cs           # Neuro-Fuzzy (один blend)
│   └── IdealLandingPresets.cs        # [I] номінал + per-mode тюнінг
├── Core/
│   ├── RocketPhysics.cs              # RK4, режими A–D, lateral, metrics
│   ├── RocketState.cs
│   ├── AtmosphereModel.cs             # g(h), ρ(h)
│   ├── LandingMetrics.cs
│   ├── SimulationManager.cs          # Monte-Carlo
│   ├── ResearchExporter.cs
│   └── DataLogger.cs
├── Visual/
│   ├── LunarTerrainMesh.cs           # heightmap + кратери до краю диска
│   ├── EnvironmentBuilder.cs         # pad, сонце, зорі
│   ├── RocketVisualBuilder.cs        # корпус ~42 м
│   ├── RocketEngineFX.cs             # core/outer plume, дим, dust
│   ├── SmoothMesh.cs / VisualMaterials.cs / SpaceAmbience.cs
├── UI/
│   ├── MissionControlUI.cs           # HUD UA/EN
│   ├── TelemetryGraph.cs             # theme-aware strip charts
│   ├── TrajectoryVisualizer.cs       # Chaikin-згладжена лінія
│   ├── UiTheme.cs / UiTypography.cs / UILocale.cs
└── Utils/
    ├── SceneBootstrap.cs
    └── CameraFollow.cs
```

### Життєвий цикл Play

1. `SceneBootstrap` — контролери, візуал, середовище, камера.  
2. `MissionControlUI` — HUD.  
3. `RocketPhysics`: `simulationArmed = false`.  
4. Старт → `ResetSimulation` + `ApplyFlightDisturbances`.  
5. `FixedUpdate`: RK4 + GNC до touchdown.  
6. Metrics + CSV + (NN/Hybrid) ES-крок; траєкторія **лишається** до нового старту.

---

## 5. Фізика

| Аспект | Реалізація |
|--------|------------|
| Трансляція | RK4, `fixedTimeStep` ≈ 5 мс |
| Орієнтація | Semi-implicit Euler + демпфінг |
| g(h) | Inverse-square |
| Атмосфера | Експоненційна ρ, drag |
| Паливо | ṁ = F/(Isp·g₀) |
| Вітер | kick + continuous air-relative |
| TVC | gimbal у тілі, важіль ~11 м |

| Режим | h₀ | Vᵧ | крен |
|-------|----|----|------|
| Номінал | ≈1800 м | ≈−72 м/с | ≈3.5° |
| Ідеал `[I]` | 1400 м | −48 м/с | 0.4° |

---

## 6. Алгоритми

Усі: RK4, TVC-PD, lateral, **термінал h&lt;25 м → ProfileThrust**.

### A PID
Hover FF + PID на `v_target`; слабший під вітром.

### B Fuzzy (Sugeno-0)
5 MF × 5 MF, product-AND, weighted average → mult·mg.  
`EvaluateSugenoThrust` — сирий вихід (для Hybrid без double-blend).

### C Neural
MLP 5→8→2, residual поверх профілю, ES після епізоду, `BestWeights_Neural.json`.

### D Hybrid ★
```
smart  = lerp(sugeno, nnRaw, α(h))
smart  = clamp(smart, sugeno ± residualMax)   // ДО blend
thrust = BlendThrust(profile, smart, …)       // один раз
```
h&lt;50 м → α,β→0 (пріоритет fuzzy).

### Lateral
Знаки TVC узгоджені: `x>0 → gz>0`, `z>0 → gx<0`.

---

## 7. Monte-Carlo

`SimulationManager`: N запусків × {PID, Fuzzy, Neural, Hybrid}, вітер/маса/кут, `timeScale`↑, звіти CSV/JSON/MD.

---

## 8. Візуалізація

| Компонент | Опис |
|-----------|------|
| `LunarTerrainMesh` | Диск R=2000 м, heightmap ~400, C2-кратери, soft-min, сірий albedo 2K |
| `EnvironmentBuilder` | Pad (berm/scorch/шви/LED), smooth-валуни, approach-маркери |
| `RocketVisualBuilder` | Falcon-class ~42 м, smooth meshes, bell-сопла, grid fins, ноги |
| `RocketEngineFX` | Core + outer plume, smoke, sparks, ground dust |
| `TrajectoryVisualizer` | Catmull-Rom + Chaikin, live tip, лишається після посадки |
| `CameraFollow` | Follow / Manual / Overview |
| Фізика | Clamp горизонталі ≤ 0.92·R диска |

### UI (top bar справа)

Порядок: **Час · Статус · Тема · Мова · СХОВАТИ** — однакова ширина кнопок (90 px), блок з відступом 24 px від краю екрана. Графіки телеметрії — з правим полем усередині лівої панелі.

---

## 9. Експорт

```
SimulationLogs/Landing_Full_<algo>_<ts>/
  00_REPORT.md · 01_step_calculations.csv · 02_summary.json
  03–07_*.svg · 08_step_analysis.md
```

---

## 10. Тести

| Набір | Зміст |
|-------|--------|
| EditMode | PID, Atmosphere, Metrics, Export, Fuzzy, signs |
| PlayMode | Hold, landing finish, NaN-free, camera, logger |

---

## 11. Відповідність темі — фінальний вердикт

| Вимога | Статус | Де |
|--------|--------|-----|
| Автономна посадка ракетоносія | ✅ | `RocketPhysics` + soft-landing критерії |
| Нечітка логіка | ✅ | `FuzzyLandingController` Sugeno-0 |
| Машинне навчання | ✅ | `NeuralController` MLP + ES |
| Інтелектуальна (гібридна) система | ✅ | `HybridController` Neuro-Fuzzy |
| Порівняння / дослідження | ✅ | Monte-Carlo + ResearchExporter |
| Демо UI | ✅ | MissionControl · UA/EN · 8 тем |
| Візуалізація | ✅ | Місяць, pad, ракета, FX, траєкторія |

### Вердикт

**Тема повністю реалізована** у вигляді захищеного дипломного симулятора GNC (фінальна візуальна/UI-поліровка 2026).

- Це **не** industrial avionics (немає Kalman/INS, CFD, повного 6-DOF thruster model).  
- Це **достатньо** для МКР: коректні алгоритми, відтворювані експерименти, експорт, наочна 3D-демо (Місяць, pad, ракета, FX, плавна траєкторія, 8 тем UI).  
- Рекомендована послідовність захисту: **D → Space → телеметрія → T → E → P**.

---

## 12. Автор

Магістерська кваліфікаційна робота, 2026.
