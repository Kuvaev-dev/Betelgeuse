# Betelgeuse — як запустити (захист / демо)

## Клонування

```bash
git clone https://github.com/Kuvaev-dev/Betelgeuse.git
```

Шрифти, текстури Earth LZ і Kenney Nature FBX — **звичайні файли** у репо (~31 MB).  
**Не потрібно** `git lfs pull`.

Перевірка: `Assets/Resources/Fonts/LiberationSans.ttf` ~350 KB.  
Editor: **Betelgeuse → Validate Runtime Assets**.

## Unity Editor

1. **Unity 6000.x** + **URP**.  
2. Відкрити `Betelgeuse` → дочекатися імпорту.  
3. `Assets/Scenes/SampleScene.unity` → **Play**.  
4. Splash: **Earth LZ** + **1-й ступінь**.

## Демо захисту

| Крок | Дія |
|------|-----|
| 1 | **`D`** / **ДЕМО** — Hybrid + Ideal IC |
| 2 | Після touchdown камера **на ступені** + лінія шляху |
| 3 | **`E`** — експорт звіту |
| 4 | **`P`** — MC A–D з **поточних слайдерів** (paired seeds) |

**F1** — довідка (об'єкт: 1-й ступінь · Earth LZ · NAV).

## Ideal soft-landing

**`I`** → вітер/шум OFF, чисті IC → **Space**.

## Monte-Carlo (`P`)

- Умови = **слайдери користувача** (h₀, Vy, вітер м/с, N, seed, шум…)  
- Paired seeds: trial `i` однаковий для PID/Fuzzy/NN/Hybrid  
- Training OFF під час pack; ideal weights **лише в RAM** (файл ваг не затирається)  
- Residual = тогл Hybrid residual (OFF = ablation Fuzzy-only)  
- Звіт: `SimulationLogs/Comparison_*/01_SUMMARY.md`

Опційний пресет констант: `DefenseBaseline` (seed 42, protocol v14) — **не** авто на P.

## Навчання NN

1. Тогл **Навчання NN** ON.  
2. Режим **3** Neural (або Hybrid) → кілька **Space**.  
3. Console: `[NN-ES] NEW best…` · файл `BestWeights_Neural.json`.

## Критерії

|Vᵧ| &lt; 3.5 · нахил &lt; 7° · промах &lt; 40 м · |Vₕ| &lt; 6.5

## Гарячі клавіші

`1–4` режим · `Space` старт · `D` демо · `I` ідеал · `P`/`X` MC · `E`/`O` експорт · `F` follow · `T` overview · `F1` help · `Y` тема · `G` мова
