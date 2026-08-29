# Betelgeuse — Архітектура

**Версія:** 1.2.0 (готово до захисту)  
**Стек:** Unity 6000 URP · C#

## Цілі

- Стабільне демо для дипломної роботи (режими A–D, метрики, експорт, UI, 3D).
- **SOLID** там, де це зменшує зв’язність без зайвого ускладнення сцен.
- Додавання 5-го контролера = **реєстрація стратегії**, а не правка диспетчеризації фізики.

## Шари

```
Presentation     UI/ · Visual/ · Utils (камера, вікно)
       ↓
Application      Control/* стратегії (Fuzzy/Neural/Hybrid MB + чистий PID)
       ↓
Domain           ILandingController · Context/Command · Resolver · Criteria
       ↓
Core             RocketPhysics (RK4) · SimulationManager · Export · Logger
Parameters       SimulationParameters (ScriptableObject)
```

| Шар | Відповідальність |
|-----|------------------|
| **Domain** | Контракти GNC і правила soft-landing |
| **Control** | Конкретні стратегії + спільний профіль наведення |
| **Core** | Інтегрування, Monte-Carlo, метрики, експорт |
| **Presentation** | HUD, меші, камера, splash |

## SOLID

| | |
|--|--|
| **S** | PID у `PidLandingStrategy`; gate у `LandingCriteria`; UI окремо |
| **O** | Новий режим → реалізувати `ILandingController` + `Register` |
| **L** | Усі стратегії повертають `ControlCommand`; фізика застосовує спільний safety envelope |
| **I** | Вузький інтерфейс: Mode / Evaluate / Reset / IsAvailable |
| **D** | `RocketPhysics` залежить від resolver, а не від конкретних Fuzzy/NN для диспетчеризації |

## Патерни

| Патерн | Де |
|--------|-----|
| Strategy | `ILandingController.Evaluate` |
| Registry | `LandingControllerResolver` |
| DTO / Snapshot | `ControlContext`, `ControlCommand` |
| Composition root | `RocketPhysics.Start` → `CreateDefault` |
| Facade | `SoftLandingGuidance` |
| Builder | `EnvironmentBuilder`, `RocketVisualBuilder` |
| Observer | події змін `UILocale` / `UiTheme` |

## Потік керування (FixedUpdate)

1. `SimulationTick` — RK4-трансляція + інтегрування орієнтації  
2. `ControlContext.FromState`  
3. `resolver.Resolve(mode).Evaluate(ctx)` → `ControlCommand`  
4. Змішування gimbal стратегії з upright PD safety net  
5. Бічне наведення × `LateralScale`  
6. Touchdown → `LandingMetrics` + `LandingCriteria.ApplySuccessFlag`  

## Карта каталогів

```
Assets/Scripts/
├── Domain/Control/
├── Control/
├── Core/
├── Parameters/
├── Visual/
├── UI/
├── Utils/
└── (тести в Assets/Tests)
```

## Як додати контролер

1. Реалізувати `ILandingController` (MonoBehaviour або чистий клас).  
2. `resolver.Register(instance)` у composition root.  
3. Розширити `RocketPhysics.ControlMode` + кнопку UI, якщо режим для користувача.  
4. **Не** додавати `switch` диспетчеризації в `UpdateControl`.  

## Свідомі non-goals

- Повний DI-контейнер (Zenject/VContainer)  
- Розбиття `MissionControlUI` на багато файлів одним проходом  
- Перепис на ECS  

## Тести

EditMode / PlayMode покривають PID, Fuzzy, знаки Neural, фізику, метрики, експорт, інтеграцію.
