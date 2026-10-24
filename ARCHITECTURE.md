# Betelgeuse — архітектура

**Версія:** 2.0 · **Стек:** Unity 6000 URP · C# · **Об'єкт GNC:** 1-й ступінь (Falcon 9-class) · Earth LZ

## Принцип

Уся фізика і GNC живуть у **чистому ядрі** `LandingSimulation` без залежностей від сцени Unity.
Те саме ядро виконує: політ у грі (`RocketPhysics` → `FixedUpdate`), Monte-Carlo (`SimulationManager`,
фонові потоки), навчання нейромережі (`EsTrainer`) і headless-стенд (`Tools/HeadlessSim`, .NET).
Тому результати порівняння, навчання й демонстрації завжди отримані на одній і тій самій моделі.

## Шари

```
Presentation   UI/ · Visual/ · CameraFollow · RocketPhysics (обгортка: трансформ, лог, камера, UI)
      ↓
Application    SimulationManager (MC A–D) · IdealLandingPresets · NeuralTrainingJob · Editor/NeuralTrainingMenu
      ↓
Control        PidLandingStrategy · FuzzyLandingController · NeuralController · HybridController
               MlpPolicy · EsTrainer · ControllerSet · SoftLandingGuidance · LateralGuidance
      ↓
Domain         ControlMode · ILandingController · ControlContext · ControlCommand · LandingCriteria · Resolver
      ↓
Core           LandingSimulation (RK4 + TVC) · NavigationEstimator · LandingScenario/Setup · LandingExperiment
Parameters     SimulationParameters (ассет) · Stage1Vehicle (константи апарата)
```

## Такт GNC (`LandingSimulation.Step`, dt = 5 мс)

1. **Навігація** — `NavigationEstimator`: IMU (гіроскоп + акселерометр) → прогноз; GPS і радіовисотомір → корекція
   (complementary filter). Орієнтація — інтегрування гіроскопа.
2. **Наведення (спільне для A–D)** — `LateralGuidance`: PID по горизонтальній позиції/швидкості →
   бажане горизонтальне прискорення → бажана вісь корпусу (нахил ≤ 12°; термінальна зона — лише гасіння Vh,
   нижче 6 м — вертикаль). Вертикальний профіль `SoftLandingGuidance.TargetDescentRate`: v* = −√(2ah).
3. **Стратегія A–D** — `ILandingController.Evaluate(ControlContext)` → `ControlCommand` (тяга, кути TVC,
   масштаб бічного наведення). `ControlContext` містить помилку орієнтації відносно **бажаної** осі,
   розкладену по осях корпусу (єдина конвенція знаків для всіх алгоритмів).
4. **Safety (спільний)** — компенсація cos(нахилу), аварійне вирівнювання при нахилі > 28°, ліміти TVC,
   інерційність приводів: TVC 12°/с, дроселювання 65 % Tmax/с.
5. **Інтегрування** — RK4 поступального руху (гравітація g(h), тяга, опір ρ(h)·Cd·S відносно повітря,
   grid fins), обертання від моменту TVC з демпфуванням.
6. **Торкання** — `LandingMetrics` на **істинному** стані за `LandingCriteria`.

## Стратегії (Strategy + Resolver)

| | Тяга | Орієнтація (TVC) | Масштаб наведення |
|--|------|------------------|-------------------|
| **A PID** | hover + PID(v* − Vy) | PID по помилці, D — гіроскоп | 1 |
| **B Fuzzy** | профіль ⊕ Sugeno 5×5 (h × \|Vy\|) | Sugeno 5×5 (e × ė) | 1 |
| **C Neural** | профіль + MLP ΔT | PD | MLP k |
| **D Hybrid** | закон B + MLP ΔT | закон B | MLP k |

`ControllerSet` — composition root: один набір налаштувань, `Clone()` для потоків.

## Навчання MLP (офлайн)

`EsTrainer` — ES(1+λ) з правилом 1/5: на кожному поколінні батько і λ нащадків оцінюються на одному
міні-батчі рандомізованих сценаріїв; найкращий за валідацією зберігається в `BestWeights_*.json`.
Навчальні, валідаційні й тестові (MC, seed 42) сценарії мають різні seed. У польоті та MC ваги заморожені.

Точки запуску (один і той самий код): `Tools/HeadlessSim` (`run.cmd train`), меню
**Betelgeuse → Train MLP (ES 1+λ)**, тогл «Навчання NN» у застосунку (`NeuralTrainingJob`, фоновий потік).

## Відтворюваність

- `LandingScenario.Sample(settings, seed)` — власний ГПВЧ на кожен сценарій, без глобального стану.
- Monte-Carlo: trial `i` має seed `DeriveSeed(experimentSeed, i)` для всіх алгоритмів (paired).
- Шум навігації детермінований від (seed, такт, канал).
- Результат не залежить від кількості потоків (перевіряє `MonteCarloFairnessTests`).

## Тести

`Assets/Tests/EditMode` — ядро, конвенція знаків, нечіткі правила, MLP, ES, чесність MC, експорт.
`Assets/Tests/PlayMode` — компонент `RocketPhysics`, камера, логер.
`Tools/HeadlessSim run.cmd selftest` — швидка перевірка ключових інваріантів без Unity.
