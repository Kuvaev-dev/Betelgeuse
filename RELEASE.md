# Betelgeuse v1.3.2 — нотатки релізу

**Дата:** 2026-08-31  
**Статус:** GNC-симулятор посадки **1-го ступеня** · Earth LZ · готовий до захисту  
**Unity:** 6000.x URP  

## Фокус v1.3.2

| Вимога | Результат |
|--------|-----------|
| Об'єкт = 1-й ступінь F9-class | `Stage1Vehicle` + маса/Ø/9 РРД/ноги/fins |
| Автономна навігація | `NavigationEstimator` (IMU+GPS+висотомір) |
| Hybrid реально Neuro-Fuzzy | residual **ON** за замовч. (ablation лишається) |
| Сцена Земля, не Місяць | Earth terrain, meadow, природа, небо |
| MC на ділянці посадки | `skipStackPhase` у SimulationManager |
| UI/логи Earth · Stage-1 | UILocale, ResearchExporter metadata |

## Покриття теми

| Вимога | Результат |
|--------|-----------|
| Автономна посадка **1-го ступеня** | RK4 + NAV + `LandingCriteria` |
| Нечітка логіка | Sugeno 5×5 |
| ML | MLP + ES (`BestWeights_Neural.json`) |
| Гібрид | Neuro-Fuzzy residual ON |
| Порівняння | DefenseBaseline paired MC v5 |
| Демо | **D**: Hybrid Stage-1 Ideal path |

## Сценарій захисту

1. **F1** — «посадка першого ступеня · Earth LZ · NAV»  
2. **D** — Hybrid садить 1-й ступінь  
3. **E** експорт · **P** MC  
4. Опційно residual OFF (ablation)

Baseline seed: **42** (див. `DefenseBaseline`).

Цитувати лише пакети `SimulationLogs/` після v1.3.x (не місячні звіти).

## Ассети (CC0)

- Kenney Nature Kit — 3D природа  
- Poly Haven — grass/mud/rock + sky photo → хмари  
- Деталі: `Assets/Art/Nature/ATTRIBUTION.md`
