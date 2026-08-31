# Betelgeuse — документація проєкту

**Повна назва теми:**  
Розроблення інтелектуальної системи автономної посадки **першого ступеня** ракети-носія на основі нечіткої логіки та машинного навчання.

**Платформа:** Unity 6000.x (URP) · C#  
**Тип:** симулятор GNC **1-го ступеня** · **Earth LZ**  
**Версія:** **v1.3.2**

---

## 1. Призначення

Моделюється автономна посадка **першого ступеня** ракети-носія (аналог Falcon 9 Block 5: суха маса 25.6 т, Ø 3.66 м, 9 РРД, 4 grid fins, 4 ноги; посадка — 1-engine burn ~845 кН) у **земних** умовах і порівнюються:

| Код | Алгоритм | Роль |
|-----|----------|------|
| **A** | Класичний **PID** | Еталон |
| **B** | **Нечітка логіка** Sugeno-0 5×5 | Правила |
| **C** | **Нейромережа** MLP 5→8→2 + ES(1+λ) | ML |
| **D** | **Гібрид Neuro-Fuzzy** (Sugeno + MLP residual) | ★ Тема |

Мета — показати стійкість гібриду під збуреннями на **ділянці посадки Stage-1** (RTLS-style, вертикальна посадка на pad).

Як тема виконується (система, не один скрипт):

| Шар | Клас | Роль |
|-----|------|------|
| Plant | `RocketPhysics` + RK4 | 1-й ступінь: маса, паливо, TVC, аеро (корпус+grid fins) |
| Navigation | `NavigationEstimator` | IMU + радіовисотомір + GPS-подібний, complementary filter |
| Guidance | `SoftLandingGuidance` | v=−√(2ah) → pad, термінал h&lt;25 м |
| Control | `ILandingController` A–D | TVC / throttle / attitude |
| Intelligence | Fuzzy / Neural / Hybrid | Sugeno, MLP+ES, Neuro-Fuzzy blend |
| Evaluation | `LandingCriteria` + MC | Vy, Vh, tilt, miss; Compare A–D |

### Обмеження (чесно)

- **Немає** посадки 2/3 ступенів, повного виведення на орбіту  
- **Немає** Місяця / лунної g  
- Навігація — спрощений complementary filter, **не** industrial Kalman/INS  
- **Немає** CFD, сертифікації бортового ПЗ  
- Гібрид **≠** ANFIS; стенд **≠** flight software  
- Візуал корпусу компактний (~28 м) при повномасштабних масі/Ø/тязі; див. `Stage1Vehicle`  
- Відділення — подія (h/t); у демо `skipStackPhase=true`  
- Ваги MLP: `BestWeights_Neural.json` (фізично обґрунтований residual + опційний ES)

### Практичне значення

Закон керування + відтворюваний стенд для **земного повернення 1-ї ступені** з порівнянням інтелектуальних алгоритмів.

---

## 2. Фази польоту

| Фаза | Опис |
|------|------|
| **Stage1** | Посадка **лише 1-го ступеня**: ноги, grid fins, 9 сопел, GNC **A–D**, навігація IMU/GPS. |

Ideal `[I]`, демо `[D]` і Monte-Carlo `[P]` — ділянка посадки Stage-1 (Earth LZ).  
Опційний Stack-path лишається в коді для досліджень, але **презентація** — Stage-1 only.

---

## 3. Швидкий старт

1. Unity **6000.x** + URP → `SampleScene` → **Play**.  
2. Splash: Earth LZ + **1-й ступінь** (booster).  
3. **`D`** або **4** Hybrid → **`I`** (опційно) → **Space**.  
4. **`P`** — MC. **`E`** — експорт.

---

## 4. Інтерфейс

- Центр: **Earth airfield / бетонна LZ**, природа, горизонт, небо.  
- Підписи UA/EN: «перший ступінь / first stage», Earth LZ.  
- F1: об'єкт роботи + автономний GNC (NAV).

### Критерії (`LandingCriteria`)

- |Vᵧ| &lt; **3.5** м/с · нахил &lt; **7°** · промах &lt; **40** м · |Vₕ| &lt; **6.5** м/с  
- SuccessScore 0…100

---

## 5. GNC (лише Stage1)

Замкнений контур **без пілота** на ділянці посадки:

1. **Navigation** — `NavigationEstimator`: IMU (accel+gyro), радіовисотомір, GPS-подібний; complementary. Plant лишається truth; закон керування читає оцінку. Ideal: NoiseScale=0 (pipeline все одно працює). MC: малий шум (0.2).  
2. **Guidance** — профіль зниження + бічне наведення до центру pad.  
3. **Control** — TVC gimbal + throttle.

| Режим | Закон |
|-------|--------|
| **A PID** | Hover FF + PID на `v_target` |
| **B Fuzzy** | Sugeno 5×5 + soft-landing blend |
| **C Neural** | MLP residual + ES; ваги `BestWeights_Neural.json` |
| **D Hybrid** | Sugeno + MLP residual → один blend (за замовч. residual **ON**) |

Ablation: вимкнути Hybrid residual → Hybrid ≈ Fuzzy-only.  
Архітектура стратегій: `ILandingController` + `LandingControllerResolver` (SOLID O/D).

---

## 6. Візуальне середовище

| Компонент | Реалізація |
|-----------|------------|
| Рельєф | `LunarTerrainMesh` + `EarthSurface`, baked albedo (Poly Haven grass/mud) |
| Природа | Kenney Nature Kit (CC0) через `NatureLibrary` + `SampleSurfaceY` / MeshCollider |
| Хмари | Спрайти з Poly Haven sky photo → Quad (`EnvironmentTextures`) |
| Сонце | Один жовтий диск; **світло** = `LookRotation(-SunWorldPosition)` |
| Pad / маяки | Бетонна LZ + approach beacons на `SampleSurfaceY` |
| Апарат | 9 сопел, 4 grid fins, 4 ноги, Ø3.66 м (`RocketVisualBuilder`) |

Атрибуція: `Assets/Art/Nature/ATTRIBUTION.md`.

---

## 7. Експорт і MC

- Середовище: `Earth LZ` · об'єкт: `first stage` · фаза: `landing after separation`  
- Monte-Carlo: paired seeds, fairness v5, Hybrid residual ON  
- Не коронувати winner при ~0% success  

---

## 8. Версія

**v1.3.2** — автономна навігація (IMU/GPS/висотомір), Hybrid residual ON за замовчуванням, ідентичність F9-class 1-го ступеня, Earth Stage-1 framing.
