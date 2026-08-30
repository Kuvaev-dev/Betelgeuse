# Betelgeuse — документація проєкту

**Повна назва теми:**  
Розроблення інтелектуальної системи автономної посадки **першого ступеня** ракети-носія на основі нечіткої логіки та машинного навчання.

**Платформа:** Unity 6000.x (URP) · C#  
**Тип:** симулятор GNC **1-го ступеня** · **Earth LZ**  
**Версія:** **v1.3.1**

---

## 1. Призначення

Моделюється автономна посадка **першого ступеня** у **земних** умовах і порівнюються:

| Код | Алгоритм | Роль |
|-----|----------|------|
| **A** | Класичний **PID** | Еталон |
| **B** | **Нечітка логіка** Sugeno-0 5×5 | Правила |
| **C** | **Нейромережа** MLP + ES(1+λ) | ML |
| **D** | **Гібрид Neuro-Fuzzy** | ★ Тема |

Мета — показати стійкість гібриду під збуреннями на **ділянці посадки Stage-1**.

### Обмеження (чесно)

- **Немає** посадки 2/3 ступенів, повного виведення на орбіту  
- **Немає** Місяця / лунної g  
- **Немає** INS/Калмана, CFD, сертифікації бортового ПЗ  
- Гібрид **≠** ANFIS; стенд **≠** flight software  
- Відділення — подія (h/t), без складної балістики розділення (у демо за замовчуванням `skipStackPhase=true`)

### Практичне значення

Закон керування + відтворюваний стенд для **земного повернення 1-ї ступені**.

---

## 2. Фази польоту

| Фаза | Опис |
|------|------|
| **Stage1** | Посадка **лише 1-го ступеня**: ноги, grid fins, 9 сопел, GNC **A–D**. |

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
- F1: одна фраза об'єкта роботи.

### Критерії (`LandingCriteria`)

- |Vᵧ| &lt; **3.5** м/с · нахил &lt; **7°** · промах · |Vₕ|  
- SuccessScore 0…100

---

## 5. GNC (лише Stage1)

| Режим | Закон |
|-------|--------|
| **A PID** | Hover FF + PID на `v_target` |
| **B Fuzzy** | Sugeno 5×5 + soft-landing blend |
| **C Neural** | MLP residual + ES |
| **D Hybrid** | Sugeno + MLP residual → один blend |

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

Атрибуція: `Assets/Art/Nature/ATTRIBUTION.md`.

---

## 7. Експорт і MC

- Середовище: `Earth LZ` · об'єкт: `first stage` · фаза: `landing after separation`  
- Monte-Carlo: paired seeds, fairness v4  
- Не коронувати winner при ~0% success  

---

## 8. Версія

**v1.3.1** — Earth Stage-1 framing, візуальний polish, єдина палітра природи, узгоджене освітлення.
