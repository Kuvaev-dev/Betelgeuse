# Betelgeuse v1.2.0 — Нотатки релізу

**Дата:** 2026-03-28  
**Статус:** GNC-симулятор, готовий до захисту диплома  
**Unity:** 6000.x URP  

## Покриття теми

| Вимога | Результат |
|--------|-----------|
| Автономна посадка 1-го ступеня | RK4 + критерії soft-landing (`LandingCriteria`) |
| Нечітка логіка | Zero-order Sugeno 5×5 |
| Машинне навчання | MLP 5→8→2 + ES(1+λ) |
| Гібридна інтелектуальна система | Neuro-Fuzzy residual (cap перед blend) + **toggle ablation** |
| Порівняльне дослідження | DefenseBaseline v2 paired Monte-Carlo + ResearchExporter (Score ±σ) |
| Демо-презентація | Mission Control HUD UA/EN · 8 тем · 3D · Defense Demo · Help |

## Основне у v1.2.0

### Захист / відтворюваність
- **`SimRng`** — seeded-збурення (одиночний політ + Monte-Carlo)
- **`DefenseBaseline` v2** — автозастосування при Порівнянні: seed 42, N=15, вітер 8, jitter ±18 м, шум УВІМК
- **Paired seeds** — trial `i` однаковий для PID/Fuzzy/Neural/Hybrid (справедливе ранжування)
- Посилене бічне GNC (масштаб: PID слабкий → Hybrid сильний), щоб MC не був універсальним 0%
- **Defense Demo** — Hybrid → Ideal → Start → overview
- **Help overlay** — гарячі клавіші, критерії, research-перемикачі
- **Hybrid residual УВІМК/ВИМК** — ablation для тези (leave-one-out NN)
- Експорт містить seed, jitter, paired-прапорець, версію протоколу, residual, Score ±σ

### Продуктивність
- Швидший cold start: менша вартість lunar mesh/albedo, менші tank skins, без штучних bootstrap-затримок

### Архітектура
- Domain Strategy GNC-шлях без змін (`ILandingController` / Resolver)

## Сценарій захисту (наживо)

1. **F1** — коротко показати довідку  
2. **D** — Defense Demo (посадка Hybrid Ideal)  
3. Після результату: **T** overview за потреби · **E** експорт  
4. **P** Monte-Carlo (DefenseBaseline paired) · відкрити `SimulationLogs/Comparison_*`  
5. Опційно: residual **ВИМК**, повторити **P** — слайд ablation  

Див. також [`HOW_TO_RUN.md`](HOW_TO_RUN.md).

## Протокол baseline (цитувати в тезі)

```
DefenseBaseline v1
  seed                  = 42
  testsPerAlgorithm     = 15
  windStrength          = 10 м/с
  massVariationPercent  = 8
  angleVariationDegrees = 8
  positionJitterMeters  = 22
  enableNoise           = true
  hybridResidual        = true
```

**Очікуване ранжування (якісно за цим протоколом):**  
`Hybrid ≥ Fuzzy` і `Hybrid ≥ PID` за success % (Score ±σ в експорті).

Після Compare-прогону на ПК презентації цитувати згенерований пакет:

`SimulationLogs/Comparison_<timestamp>/01_SUMMARY.md`

(Не хардкодити машинно-залежні % тут — seed+протокол роблять пакет джерелом істини.)

## Межі (чесно)

Не industrial avionics: немає Kalman/INS, CFD thrusters чи сертифікації бортового ПЗ.  
Достатньо для МКР як відтворюваного GNC research simulator із повним thesis-експортом.

## Як запустити

1. Unity **6000.x** (URP) → `Assets/Scenes/SampleScene.unity` → **Play**  
2. Або standalone-збірка — [`HOW_TO_RUN.md`](HOW_TO_RUN.md)  
3. Специфікації: `README.md` · `DOCS.md` · `ARCHITECTURE.md`

## Версія

- Додаток / документація: **1.2.0**  
- Попередні: `v1.1.0` (2026-08-22) · `v1.0.0` (2026-08-15)
