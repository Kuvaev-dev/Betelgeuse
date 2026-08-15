# Betelgeuse — Документація проєкту

**Повна назва теми:**  
Розроблення інтелектуальної системи автономної посадки ракетоносія на основі нечіткої логіки та машинного навчання.

**Платформа:** Unity 6000.x (URP) · C#  
**Тип:** симулятор GNC (Guidance, Navigation & Control) першого ступеня ракетоносія  
**Версія / статус:** **v1.0.0** — diploma release (2026-08-15) · див. [`RELEASE.md`](RELEASE.md)

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
┌── BETELGEUSE · mode · t     [Start|Stop|Ideal|Path|View|Export]  Theme Lang ┐
│                                                      Status Hide (під ними) │
├──────────────┬────────────────────────┬────────────────────────────────────┤
│ ЛІВО:        │  ЦЕНТР 3D              │ ПРАВО:                             │
│ GATE 2×2     │  Місяць + LZ pad       │ Швидкий старт                      │
│ Підказка     │  ракета ~43 м          │ Алгоритм 2×2 (1–4)                 │
│ Головне/dyn  │  траєкторія            │ Порівняти [P] / Скасувати [X]      │
│ Графіки      │  orbit / zoom          │ Камера · Умови (вітер/шум/N/x)     │
│              │                        │ Success % + winner                 │
├──────────────┴────────────────────────┴────────────────────────────────────┤
│              Крок: Hybrid | термінал / гальмування / …                      │
└────────────────────────────────────────────────────────────────────────────┘
```

### Теми UI (`Y`)

Dark · Cyan · Amber · **Light** · Green · Violet · Red · **Ice**

- Панелі / edge / accent / слайдери / рамки графіків — **theme-aware**.  
- Rebuild при зміні теми **зберігає** samples strip-charts, значення слайдерів і toggles.  
- Шрифт HUD: LiberationSans + dynamic Cyrillic fallback; без dilate/soft underlay.

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
| `LunarTerrainMesh` | Диск R=2000 м, heightmap ~448, C2-кратери, world-UV albedo+normal 2K |
| `EnvironmentBuilder` | Premium LZ (steel deck, bullseye, leg pads, curb LED, beacons) |
| `RocketVisualBuilder` | Falcon-class ~43 м, cyl-UV skins, CFRP interstage, tangent ogive fairing, bells |
| `RocketEngineFX` | Core + outer plume, smoke, sparks, ground dust |
| `TrajectoryVisualizer` | Catmull-Rom + Chaikin, live tip, лишається після посадки |
| `CameraFollow` | Follow / Manual / Overview |
| Фізика | Clamp горизонталі ≤ 0.92·R диска |

### UI layout (актуально)

| Зона | Вміст |
|------|--------|
| **Top chrome** | Brand + mode pill + time; flight actions; справа 2×2: Theme/Lang зверху, Status/Hide знизу |
| **Ліва панель** | GATE → Підказка → Головне → Динаміка → Рушій → Піки → Графіки |
| **Права панель** | Quick-start → Алгоритм → Порівняння → Камера → Умови тесту (3 слайдери 5 px + toggles) → % |
| **Низ** | Смуга кроку фази посадки (`PID \| Крок: …`) |
| **Слайдери** | Однакова товщина треку 5 px; fill без stretch-height; handle 11×11 |

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

### Вердикт — v1.0.0 (2026-08-15)

**Тема повністю реалізована** — **реліз v1.0.0** для захисту МКР.

| Шар | Стан |
|-----|------|
| Алгоритми A–D + Ideal + Monte-Carlo | ✅ коректні знаки TVC/lateral, hybrid residual cap |
| Фізика RK4 + критерії + metrics/export | ✅ |
| 3D: lunar disk, LZ pad, Falcon-class, FX | ✅ premium procedural |
| HUD: UA/EN, 8 тем, GATE, step strip | ✅ theme-safe rebuild |
| Тести Edit/Play | ✅ за архітектурою |

**Межі (чесно для захисту):** не industrial avionics (немає Kalman/INS, CFD, повного thruster CFD). Достатньо для МКР: інтерпретований Sugeno + NN residual, відтворювані експерименти, пакет експорту, наочна 3D-демо.

**Рекомендована демо-послідовність:** `4` Hybrid → `I` Ideal (опційно) → `Space` → ліва телеметрія/GATE → `T` траєкторія → `E` експорт → `P` порівняння.

---

## 12. Автор

Магістерська кваліфікаційна робота, 2026.
