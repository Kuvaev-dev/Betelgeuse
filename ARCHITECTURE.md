# Betelgeuse — архітектура

**Версія:** 1.3.2  
**Стек:** Unity 6000 URP · C#  
**Середовище:** Earth LZ · об'єкт GNC — **1-й ступінь** (Falcon 9-class analogue)

## Цілі

- Демо захисту: посадка Stage-1 алгоритмами A–D на Earth LZ.
- SOLID: новий контролер = реєстрація стратегії, без правки фізики.
- DRY: спільні пресети, критерії, візуальні палітри в одному місці.
- Автономія: GNC читає `NavigationEstimator`, а не god-mode plant.

## Фази місії

```
Idle ──Start──► [Stack опційно] ──sep──► Stage1 ──touchdown──► Finished
                                      │
                                      │ NavigationEstimator → ILandingController A–D
                                      │ stage1 mass + fuel
```

Презентація за замовчуванням: `skipStackPhase=true` → одразу **Stage1**.

| Фаза | Власник фізики | Керування | Візуал |
|------|----------------|-----------|--------|
| **Idle** | — | — | Stage-1 на landing IC (типово) |
| **Stack** | `UpdateStackControl` | open-loop | (якщо увімкнено) |
| **Stage1** | `UpdateControl` + RK4 | **A–D** via Resolver, state from NAV | 1-й ступінь |

Monte-Carlo і Ideal `[I]` — **Stage1** only.

## Шари

```
Presentation     UI/ · Visual/ (Earth LZ, booster) · Camera
       ↓
Application      Control/* (Fuzzy/Neural/Hybrid) · IdealLandingPresets
       ↓
Domain           ILandingController · Context/Command · Resolver · Criteria
       ↓
Core             RocketPhysics · NavigationEstimator · SimulationManager · Export
Parameters       SimulationParameters / Stage1Vehicle
```

## SOLID

| | |
|--|--|
| **S** | PID у `PidLandingStrategy`; gate у `LandingCriteria`; NAV ≠ plant; візуал ≠ фізика |
| **O** | Новий режим → `ILandingController` + `Register` |
| **L** | Усі стратегії → `ControlCommand`; safety envelope спільний |
| **I** | Mode / Evaluate / Reset / IsAvailable |
| **D** | `RocketPhysics` → resolver, не конкретні Fuzzy/NN |

## Патерни

| Патерн | Де |
|--------|-----|
| **Strategy** | `ILandingController` + Resolver |
| **Facade** | `EnvironmentBuilder`, `RocketVisualBuilder` |
| **Factory / static builder** | `SmoothMesh`, `VisualMaterials`, `NatureLibrary` |
| **Template scatter** | `NaturalPoint` у `BuildNatureProps` (єдиний розкид) |

## DRY (візуал)

| Відповідальність | Клас |
|------------------|------|
| Палітра / текстури | `EnvironmentTextures` |
| Світло + Сонце (одна позиція) | `EnvironmentBuilder.SunWorldPosition` |
| Висота рельєфу | `LunarTerrainMesh.SampleSurfaceY` (+ MeshCollider) |
| Посадка props | `NatureLibrary.PlantOnGround` |
| Матеріали URP | `VisualMaterials` |
| Ідентичність 1-го ступеня | `Stage1Vehicle` |

## Потік FixedUpdate

1. (Опційно Stack) open-loop → sep  
2. Stage1: `navigation.Step` (IMU/GPS/alt) → `ControlContext` → `resolver.Evaluate` → lateral (оцінка x,z) → RK4  
3. Touchdown → `LandingCriteria` на **істинному** plant (оцінка лише для закону)

## Візуальний конвеєр (старт)

1. `SetupLighting` (напрям = від `SunWorldPosition`)  
2. `LunarTerrainMesh.CreateRoutine` + collider  
3. `BuildNatureProps` (Kenney FBX, natural scatter)  
4. Pad / approach beacons  
5. Sky dome + cloud quads + sun disc  

## Тести

- EditMode / PlayMode у `Assets/Tests/`  
- Не запускати batchmode, якщо Editor тримає lock проєкту  
