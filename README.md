# Betelgeuse — інтелектуальна автономна посадка першого ступеня

**v1.3.3** · дипломний GNC-симулятор (Unity URP) · GNC protocol v14

**Тема:** Розроблення інтелектуальної системи автономної посадки **першого ступеня** ракети-носія на основі нечіткої логіки та машинного навчання.

Симулятор GNC **1-го ступеня** (Falcon 9-class analogue: суха маса 25.6 т, Ø 3.66 м, 9 РРД, 4 grid fins, 4 ноги; візуал компактний ~28 м) у **земних** умовах (Earth LZ): порівняння PID / Fuzzy / Neural / Hybrid.

| Документ | Зміст |
|----------|--------|
| [`DOCS.md`](DOCS.md) | Повна специфікація |
| [`ARCHITECTURE.md`](ARCHITECTURE.md) | Шари, SOLID, GNC, DRY |
| [`RELEASE.md`](RELEASE.md) | Демо захисту |
| [`HOW_TO_RUN.md`](HOW_TO_RUN.md) | Запуск + baseline seed |

## Об'єкт роботи

| | |
|--|--|
| **Об'єкт** | Автономна посадка **першого ступеня** ракети-носія (RTLS, pad) |
| **Середовище** | Earth LZ (аеродром / бетонна площадка, природа, небо) |
| **Навігація** | IMU + радіовисотомір + GPS-подібний (`NavigationEstimator`) |
| **Методи** | Нечітка логіка (Sugeno) + ML (MLP+ES) + Hybrid Neuro-Fuzzy |
| **Порівняння** | A PID · B Fuzzy · C Neural · D Hybrid (Monte-Carlo) |

**Не моделюється:** орбітальне виведення, посадка 2/3 ступенів, Місяць, industrial Kalman/INS, CFD, avionics сертифікація.

## Швидкий старт

```bash
git clone https://github.com/Kuvaev-dev/Betelgeuse.git
```

Ассети runtime (шрифти, текстури, Nature FBX) **в репо**, без окремого `git lfs pull`.

1. Unity **6000.x** (URP) → відкрити проєкт → `Assets/Scenes/SampleScene.unity` → **Play**
2. На екрані: **Earth LZ** + **1-й ступінь** (ноги, grid fins, 9 сопел)
3. **`4`** Hybrid → **`I`** Ideal → **`Space`** — посадка  
4. **`D`** — демо · **`P`** — MC A–D (**умови зі слайдерів**) · **`E`** — експорт

## Режими керування

| | Режим | Метод |
|---|--------|--------|
| A | **PID** | Soft-landing `v=−√(2ah)` + PID |
| B | **Fuzzy** | Zero-order **Sugeno** 5×5 |
| C | **Neural** | MLP 5→8→2 + **ES (1+λ)** · `BestWeights_Neural.json` |
| D | **Hybrid** ★ | Neuro-Fuzzy (Sugeno + MLP residual ON) |

## Критерії soft-landing

|Vᵧ| &lt; **3.5** м/с · нахил &lt; **7°** · промах &lt; **40** м · |Vₕ| &lt; **6.5** м/с  
(`LandingCriteria` — реалістичні gate; жорсткий вітер/jitter → відмови)

## Фізика

- g ≈ 9.81, ρ(h), Cd·S (корпус + grid fins), вітер  
- Маса = **1-й ступінь** + залишок палива (`Stage1Vehicle`)  
- RK4 + TVC + lateral guidance від **оцінки** навігації  

## Візуал (презентація)

- **Stage-1 only** (booster): білий корпус, зона стиковки, ноги, fins, 9 сопел  
- **Earth LZ**: meadow albedo (Poly Haven), природа (Kenney CC0), хмари  
- Світло узгоджене з диском Сонця (`EnvironmentBuilder.SunWorldPosition`)

## Вердикт (тема)

| Фрагмент теми | Реалізація | Статус |
|---------------|------------|--------|
| Автономна посадка | GNC без пілота до touchdown | ✅ |
| **Першого ступеня** | F9-class маса/Ø/9 РРД/ноги/fins + Stage-1 GNC | ✅ |
| Навігація | IMU + GPS + висотомір, complementary | ✅ |
| Нечітка логіка | Sugeno-0 5×5 (**B**) | ✅ |
| Машинне навчання | MLP + ES (**C**), ваги в репо | ✅ |
| Інтелектуальна система | Hybrid Neuro-Fuzzy residual ON (**D**) | ✅ |
| Земні умови | Earth LZ, g=9.81 | ✅ |
| Оцінювання | Monte-Carlo A–D, логи Comparison | ✅ |

**Готовність до захисту: ТАК.**

## Автор

Магістерська кваліфікаційна робота, 2026.
