# Intelligent Rocket Landing System (Unity + C#)

## Опис
Інтелектуальна система автономної посадки ракетоносія на основі нечіткої логіки та машинного навчання.

## Режими керування
- **PID** — класичний PID-контролер
- **Fuzzy Logic** — нечітка логіка (Mamdani-style)
- **Neural Network** — проста нейронна мережа з онлайн-навчанням

## Як запустити експеримент
1. Відкрий сцену з ракетою
2. Знайди об'єкт `SimulationManager`
3. Постав галочку `runFullExperiment = true`
4. Натисни Play

Результати з'являться в Console + збережаться у папку `SimulationLogs`

## Файли проекту
- `RocketPhysics.cs` — основна фізика + RK4
- `FuzzyLandingController.cs` — нечітка логіка
- `NeuralController.cs` — нейронна мережа + навчання
- `SimulationManager.cs` — batch-тестування + Monte-Carlo
- `ExperimentDashboard.cs` — UI для керування
- `TrajectoryVisualizer.cs` — візуалізація траєкторії

## Результати
- `SimulationLogs/*.csv` — таблиці з метриками
- `BestWeights_Neural.json` — найкращі ваги нейронної мережі

## Автор
Магістерська робота, 2026
