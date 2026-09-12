# Betelgeuse v1.3.3 — нотатки релізу

**Дата:** 2026-09-12  
**Статус:** GNC-симулятор посадки **1-го ступеня** · Earth LZ · **готовий до захисту**  
**Unity:** 6000.x URP · GNC protocol **v14**

## Покриття теми

| Вимога | Результат |
|--------|-----------|
| Автономна посадка **1-го ступеня** | RK4 + NAV + `LandingCriteria` |
| Нечітка логіка | Sugeno 5×5 (режим B) |
| ML | MLP 5→8→2 + online ES (`BestWeights_Neural.json`) |
| Гібрид | Neuro-Fuzzy residual (тогл ON/OFF = ablation) |
| Порівняння | Monte-Carlo A–D, **paired seeds**, умови з UI |
| Демо | **D**: Hybrid + Ideal IC · камера лишається на ступені |

## Критерії soft-landing

|Vᵧ| &lt; **3.5** м/с · нахил &lt; **7°** · промах &lt; **40** м · |Vₕ| &lt; **6.5** м/с

## Реалізм (v13–v14)

- Вітер у UI = **ambient м/с** (drag relative to air)
- Isp посадки **SL ≈ 282 с** (`Stage1Vehicle.IspLandingS`)
- Grid fins — помірний aero-damp (не «безкоштовний» успіх)
- Ideal `[I]` — чисті ПУ; шум/вітер — jitter + NAV
- **P** не перезаписує UI-умови і не затирає `BestWeights_Neural.json`

## Сценарій захисту

1. **F1** — тема: 1-й ступінь · Earth LZ · NAV  
2. **D** — Hybrid Ideal soft-landing  
3. **1–4** + **I** + **Space** — кожен алгоритм  
4. Вітер 8–12 + шум ON → **P** — диференціація A–D  
5. **E** — експорт `SimulationLogs/`  
6. Опційно: residual OFF → ablation Hybrid ≈ Fuzzy  

Цитувати лише пакети `SimulationLogs/` після v1.3.x.

## Ассети (CC0)

- Kenney Nature Kit · Poly Haven grass/sky  
- `Assets/Art/Nature/ATTRIBUTION.md`
