# Betelgeuse — Architecture

**Version:** 1.1.0  
**Stack:** Unity 6000 URP · C#

## Goals

- Stable diploma demo (modes A–D, metrics, export, UI, 3D).
- **SOLID** where it cuts coupling without over-engineering scenes.
- Adding a 5th controller = **register a strategy**, not edit physics dispatch.

## Layering

```
Presentation     UI/ · Visual/ · Utils (camera, window)
       ↓
Application      Control/* strategies (Fuzzy/Neural/Hybrid MB + pure PID)
       ↓
Domain           ILandingController · Context/Command · Resolver · Criteria
       ↓
Core             RocketPhysics (RK4) · SimulationManager · Export · Logger
Parameters       SimulationParameters (ScriptableObject)
```

| Layer | Responsibility |
|-------|----------------|
| **Domain** | GNC contracts and soft-landing rules |
| **Control** | Concrete strategies + shared guidance profile |
| **Core** | Integration, Monte-Carlo, metrics, export |
| **Presentation** | HUD, meshes, camera, splash |

## SOLID

| | |
|--|--|
| **S** | PID in `PidLandingStrategy`; gate in `LandingCriteria`; UI separate |
| **O** | New mode → implement `ILandingController` + `Register` |
| **L** | All strategies return `ControlCommand`; physics applies shared safety envelope |
| **I** | Narrow interface: Mode / Evaluate / Reset / IsAvailable |
| **D** | `RocketPhysics` depends on resolver, not concrete Fuzzy/NN for dispatch |

## Patterns

| Pattern | Where |
|---------|--------|
| Strategy | `ILandingController.Evaluate` |
| Registry | `LandingControllerResolver` |
| DTO / Snapshot | `ControlContext`, `ControlCommand` |
| Composition root | `RocketPhysics.Start` → `CreateDefault` |
| Facade | `SoftLandingGuidance` |
| Builder | `EnvironmentBuilder`, `RocketVisualBuilder` |
| Observer | `UILocale` / `UiTheme` change events |

## Control flow (FixedUpdate)

1. `SimulationTick` — RK4 translation + attitude integration  
2. `ControlContext.FromState`  
3. `resolver.Resolve(mode).Evaluate(ctx)` → `ControlCommand`  
4. Blend strategy gimbal with upright PD safety net  
5. Lateral guidance × `LateralScale`  
6. Touchdown → `LandingMetrics` + `LandingCriteria.ApplySuccessFlag`  

## Folder map

```
Assets/Scripts/
├── Domain/Control/
├── Control/
├── Core/
├── Parameters/
├── Visual/
├── UI/
├── Utils/
└── (Tests under Assets/Tests)
```

## Adding a controller

1. Implement `ILandingController` (MonoBehaviour or pure class).  
2. `resolver.Register(instance)` in composition root.  
3. Extend `RocketPhysics.ControlMode` + UI button if user-facing.  
4. **Do not** add a dispatch `switch` in `UpdateControl`.  

## Deliberate non-goals

- Full DI container (Zenject/VContainer)  
- Splitting `MissionControlUI` into many files in one pass  
- ECS rewrite  

## Tests

EditMode / PlayMode cover PID, Fuzzy, Neural signs, physics, metrics, export, integration.
