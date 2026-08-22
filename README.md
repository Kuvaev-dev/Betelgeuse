# Betelgeuse — Intelligent Autonomous Rocket Landing

**v1.1.0** · Diploma GNC simulator (Unity URP)

**Тема:** Розроблення інтелектуальної системи автономної посадки ракетоносія на основі нечіткої логіки та машинного навчання.

Симулятор GNC першого ступеня (~42 м, Falcon-class scale) з порівнянням класичного PID та інтелектуальних алгоритмів.

| Документ | Зміст |
|----------|--------|
| [`DOCS.md`](DOCS.md) | Повна специфікація (GNC, UI, візуал, експорт, тести) |
| [`ARCHITECTURE.md`](ARCHITECTURE.md) | Шари, SOLID, патерни, як додати контролер |
| [`RELEASE.md`](RELEASE.md) | Нотатки релізу / демо для захисту |

## Швидкий старт

1. Unity **6000.x** (URP) → `Assets/Scenes/SampleScene.unity` → **Play**
2. Справа: алгоритм **4 Hybrid** (рекомендовано)
3. Опційно **Ідеал** `[I]` — номінал без вітру/шуму
4. **Старт** `[Space]` — посадка
5. **Порівняти** `[P]` — Monte-Carlo A–D
6. **Експорт** `[E]` → `SimulationLogs/`

## Режими керування

| | Режим | Метод |
|---|--------|--------|
| A | **PID** | Soft-landing `v=−√(2ah)` + PID |
| B | **Fuzzy** | Zero-order **Sugeno** 5×5 |
| C | **Neural** | MLP 5→8→2 + **ES (1+λ)** |
| D | **Hybrid** ★ | Neuro-Fuzzy (тема роботи) |

Диспетчеризація — через `ILandingController` / `LandingControllerResolver` (без switch по типах).

## Гарячі клавіші

| Клавіша | Дія |
|---------|-----|
| **1 / 2 / 3 / 4** | PID / Fuzzy / Neural / Hybrid |
| **Space** | Старт посадки |
| **I** | Ідеальні параметри |
| **Esc** | Стоп / закрити результат |
| **H** | Сховати / показати панелі |
| **F / T / C / R** | Follow / Overview / Manual / Reset cam |
| **L** | Траєкторія on/off |
| **E / O** | Експорт / папка звітів |
| **G** | Мова UA ↔ EN |
| **Y** | Тема UI (8 тем) |
| **P / X** | Порівняти всі / скасувати |

## Камера

| Дія | Керування |
|-----|-----------|
| Orbit | ЛКМ / ПКМ · WASD · стрілки · Q/E |
| Зум | Колесо · +/- |
| Follow / Overview / Reset | F / T / R |

## Критерії soft-landing

|Vᵧ| &lt; **3.5** м/с · нахил &lt; **7°** · промах &lt; **25** м · |Vₕ| &lt; **5** м/с  
(`LandingCriteria` — єдине джерело правди)

## Структура коду

```
Assets/Scripts/
├── Domain/Control/   ILandingController, Context/Command, Resolver, Criteria, PidStrategy
├── Control/          Fuzzy, Neural, Hybrid, SoftLandingGuidance, Ideal presets
├── Core/             RocketPhysics (RK4), SimulationManager, export, metrics, logger
├── Parameters/       SimulationParameters (ScriptableObject)
├── Visual/           Місяць, pad, ракета, FX
├── UI/               MissionControlUI, themes, graphs, trajectory
├── Utils/            SceneBootstrap, CameraFollow, BorderlessWindow
└── Tests/            EditMode + PlayMode
```

## Експорт

Кожен запуск = **окремий каталог** у `SimulationLogs/` (нічого не розкидано в корені).

```
SimulationLogs/
  Landing_<Algorithm>_<timestamp>/
    00_README.md          ← з чого почати
    01_SUMMARY.md         ← головний звіт
    02_metrics.json
    03_timeseries.csv
    04_analysis.md
    charts/*.svg
  Comparison_<timestamp>/
    00_README.md · 01_SUMMARY.md · 02_results.csv · 03_results.json
```

`BestWeights_Neural.json` — ваги MLP (корінь проєкту).

## Тести

**Window → General → Test Runner**

- **EditMode** — PID, атмосфера, метрики, fuzzy, signs, export  
- **PlayMode** — інтеграція, камера, логер  

## Ключові гарантії

- A–D мають **різну** mid-flight поведінку; Ideal `[I]` — стабільний soft-landing  
- UI-вітер/шум діють на одиночний старт (`ApplyFlightDisturbances`)  
- Траєкторія: Catmull-Rom + Chaikin, лишається після посадки  
- Місяць: процедурний диск R≈2000 м, cool-gray, гладкі кратери  
- Ракета: Falcon-class ~42 м, grid fins, ноги, bell-сопла  
- HUD: UA/EN · 8 тем · компактна модалка результату · step strip  

## Вердикт (тема)

**Відповідність темі МКР: ТАК.**  
**Готовність до повноцінної презентації: ТАК** (демо-сценарій нижче).

| Блок | Статус |
|------|--------|
| Автономна посадка | ✅ |
| Нечітка логіка (Sugeno) | ✅ |
| Машинне навчання (MLP+ES) | ✅ |
| Гібрид Neuro-Fuzzy (тема) | ✅ |
| Порівняння Monte-Carlo + export | ✅ |
| 3D + UI UA/EN · 8 тем | ✅ |
| SOLID control layer | ✅ |

Рівень — дипломна симуляція GNC (не industrial avionics).  

**Демо на захист:** `4` Hybrid → `I` Ideal → `Space` → GATE/score → `T` → `E` → `P`.  
(Опційно: toggle **Train** + Neural — показати навчання ES.)

## Автор

Магістерська кваліфікаційна робота, 2026.
