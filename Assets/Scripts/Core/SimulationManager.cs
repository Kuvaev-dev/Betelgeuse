using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;

public class SimulationManager : MonoBehaviour
{
    [Header("Основні налаштування")]
    [Tooltip("Якщо залишити порожнім — скрипт знайде RocketPhysics автоматично")]
    public RocketPhysics rocketPhysics;

    [Header("Параметри тестування")]
    public int testsPerMode = 20;
    public float delayBetweenTests = 0.7f;

    private List<LandingMetrics> pidResults = new List<LandingMetrics>();
    private List<LandingMetrics> fuzzyResults = new List<LandingMetrics>();

    [Header("Керування")]
    [Tooltip("Постав галочку і натисни Play, щоб запустити тест")]
    public bool startBatchTest = false;

    private void Awake()
    {
        // Автоматичний пошук RocketPhysics
        if (rocketPhysics == null)
        {
            // Replace deprecated API with the newer one:
            rocketPhysics = FindAnyObjectByType<RocketPhysics>();
            if (rocketPhysics != null)
                Debug.Log("SimulationManager автоматично знайшов RocketPhysics");
            else
                Debug.LogError("Не вдалося знайти RocketPhysics в сцені!");
        }
    }

    private void Update()
    {
        if (startBatchTest)
        {
            startBatchTest = false;
            StartCoroutine(RunFullComparisonTest());
        }
    }

    private IEnumerator RunFullComparisonTest()
    {
        if (rocketPhysics == null)
        {
            Debug.LogError("RocketPhysics не знайдено!");
            yield break;
        }

        Debug.Log("ПОЧАТОК ПОВНОГО ПОРІВНЯЛЬНОГО ТЕСТУВАННЯ");

        // Тест PID
        rocketPhysics.controlMode = RocketPhysics.ControlMode.PID;
        yield return StartCoroutine(RunTests("PID", pidResults));

        // Тест Fuzzy Logic
        rocketPhysics.controlMode = RocketPhysics.ControlMode.Fuzzy;
        yield return StartCoroutine(RunTests("Fuzzy Logic", fuzzyResults));

        // Вивід результатів
        ShowFinalComparison();
        SaveToCSV();

        Debug.Log("Batch-тестування завершено!");
    }

    private IEnumerator RunTests(string modeName, List<LandingMetrics> resultList)
    {
        resultList.Clear();
        Debug.Log($"Запуск {testsPerMode} симуляцій для {modeName}");

        for (int i = 0; i < testsPerMode; i++)
        {
            rocketPhysics.ResetSimulation();
            yield return new WaitForSeconds(delayBetweenTests);

            // Чекаємо, поки симуляція не завершиться
            while (!rocketPhysics.state.simulationFinished)
                yield return null;

            // Зберігаємо копію результатів
            resultList.Add(new LandingMetrics
            {
                touchdownVelocity = rocketPhysics.metrics.touchdownVelocity,
                landingAngleError = rocketPhysics.metrics.landingAngleError,
                fuelRemaining = rocketPhysics.metrics.fuelRemaining,
                maxAltitude = rocketPhysics.metrics.maxAltitude,
                totalFlightTime = rocketPhysics.metrics.totalFlightTime,
                isSuccessfulLanding = rocketPhysics.metrics.isSuccessfulLanding
            });

            Debug.Log($"   [{modeName}] Тест {i + 1}/{testsPerMode} завершено");
        }
    }

    private void ShowFinalComparison()
    {
        Debug.Log("══════════════════════════════════════════════");
        Debug.Log("ФІНАЛЬНЕ ПОРІВНЯННЯ PID vs FUZZY");
        Debug.Log("══════════════════════════════════════════════");

        PrintStats("PID", pidResults);
        PrintStats("Fuzzy Logic", fuzzyResults);
    }

    private void PrintStats(string name, List<LandingMetrics> list)
    {
        if (list.Count == 0) return;

        float successRate = (float)list.FindAll(m => m.isSuccessfulLanding).Count / list.Count * 100f;
        float avgVelocity = GetAverage(list, m => m.touchdownVelocity);
        float avgAngle = GetAverage(list, m => m.landingAngleError);
        float avgFuel = GetAverage(list, m => m.fuelRemaining);
        float avgScore = GetAverage(list, m => m.SuccessScore);

        Debug.Log($"{name.ToUpper()}");
        Debug.Log($"Успішність: {successRate:F1}%");
        Debug.Log($"Середня швидкість посадки: {avgVelocity:F2} м/с");
        Debug.Log($"Середній кут: {avgAngle:F2}°");
        Debug.Log($"Середній залишок палива: {avgFuel:F1} кг");
        Debug.Log($"Середня оцінка: {avgScore:F1}/100");
        Debug.Log("──────────────────────────────────────────────");
    }

    private float GetAverage(List<LandingMetrics> list, System.Func<LandingMetrics, float> selector)
    {
        float sum = 0f;
        foreach (var item in list)
            sum += selector(item);
        return sum / list.Count;
    }

    private void SaveToCSV()
    {
        string path = Path.Combine(Application.dataPath, "..", "SimulationLogs",
            $"PID_vs_Fuzzy_Comparison_{System.DateTime.Now:yyyyMMdd_HHmmss}.csv");

        var lines = new List<string>
        {
            "Algorithm,SuccessRate(%),AvgTouchdownVel(m/s),AvgAngleError(°),AvgFuel(kg),AvgScore"
        };

        lines.Add(CreateCSVLine("PID", pidResults));
        lines.Add(CreateCSVLine("Fuzzy", fuzzyResults));

        File.WriteAllLines(path, lines);
        Debug.Log($"Порівняльна таблиця збережена: {path}");
    }

    private string CreateCSVLine(string name, List<LandingMetrics> list)
    {
        if (list.Count == 0)
            return $"{name},0,0,0,0,0";

        float success = (float)list.FindAll(m => m.isSuccessfulLanding).Count / list.Count * 100f;
        float vel = GetAverage(list, m => m.touchdownVelocity);
        float angle = GetAverage(list, m => m.landingAngleError);
        float fuel = GetAverage(list, m => m.fuelRemaining);
        float score = GetAverage(list, m => m.SuccessScore);

        return $"{name},{success:F2},{vel:F2},{angle:F2},{fuel:F2},{score:F2}";
    }
}