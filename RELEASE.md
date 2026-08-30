# Betelgeuse v1.3.1 — нотатки релізу

**Дата:** 2026-08-30  
**Статус:** GNC-симулятор посадки **1-го ступеня** · Earth LZ · готовий до захисту  
**Unity:** 6000.x URP  

## Фокус v1.3.1

| Вимога | Результат |
|--------|-----------|
| Сцена Земля, не Місяць | Earth terrain, meadow, природа, небо |
| Об'єкт = 1-й ступінь | Stage-1 only visual + mass + GNC |
| Маса/GNC на посадці | Stage1 dry+fuel; A–D лише Stage1 |
| MC на ділянці посадки | `skipStackPhase` у SimulationManager |
| UI/логи Earth · Stage-1 | UILocale, ResearchExporter metadata |
| Візуальний polish | **Одне** світло = Сонце; хмари-спрайти; природа на mesh |
| Документація | README / DOCS / ARCH / HOW_TO_RUN (UA) |

## Покриття теми

| Вимога | Результат |
|--------|-----------|
| Автономна посадка **1-го ступеня** | RK4 + `LandingCriteria` |
| Нечітка логіка | Sugeno 5×5 |
| ML | MLP + ES |
| Гібрид | Neuro-Fuzzy |
| Порівняння | DefenseBaseline paired MC |
| Демо | **D**: Hybrid Stage-1 Ideal path |

## Сценарій захисту

1. **F1** — «посадка першого ступеня · Earth LZ»  
2. **D** — Hybrid садить 1-й ступінь  
3. **E** експорт · **P** MC  
4. Опційно residual OFF  

Baseline seed: **42** (див. `DefenseBaseline`).

Цитувати лише пакети `SimulationLogs/` після v1.3.x (не місячні звіти).

## Ассети (CC0)

- Kenney Nature Kit — 3D природа  
- Poly Haven — grass/mud/rock + sky photo → хмари  
- Деталі: `Assets/Art/Nature/ATTRIBUTION.md`
