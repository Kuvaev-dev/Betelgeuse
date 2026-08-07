using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Двомовний UI: українська (UK) та англійська (EN).
/// Зберігає вибір у PlayerPrefs.
/// </summary>
public static class UILocale
{
    public enum Lang { UK = 0, EN = 1 }

    const string PrefKey = "Betelgeuse.UILang";
    static Lang current = Lang.UK;
    static bool loaded;

    public static Lang Current
    {
        get
        {
            EnsureLoaded();
            return current;
        }
        set
        {
            current = value;
            PlayerPrefs.SetInt(PrefKey, (int)value);
            PlayerPrefs.Save();
            OnLanguageChanged?.Invoke();
        }
    }

    public static event System.Action OnLanguageChanged;

    public static bool IsUK => Current == Lang.UK;

    static void EnsureLoaded()
    {
        if (loaded) return;
        loaded = true;
        current = (Lang)PlayerPrefs.GetInt(PrefKey, 0);
    }

    public static void Toggle()
    {
        Current = Current == Lang.UK ? Lang.EN : Lang.UK;
    }

    public static string T(string key)
    {
        EnsureLoaded();
        if (!Table.TryGetValue(key, out var pair))
            return key;
        return current == Lang.UK ? pair.uk : pair.en;
    }

    public static string ModeName(RocketPhysics.ControlMode m) => m switch
    {
        RocketPhysics.ControlMode.Fuzzy => T("mode_fuzzy"),
        RocketPhysics.ControlMode.Neural => T("mode_neural"),
        RocketPhysics.ControlMode.Hybrid => T("mode_hybrid"),
        _ => T("mode_pid")
    };

    public static string CamLabel(CameraFollow.ViewMode m) => m switch
    {
        CameraFollow.ViewMode.Overview => T("cam_overview"),
        CameraFollow.ViewMode.Manual => T("cam_manual"),
        _ => T("cam_follow")
    };

    struct Pair
    {
        public string uk, en;
        public Pair(string u, string e) { uk = u; en = e; }
    }

    static readonly Dictionary<string, Pair> Table = new()
    {
        // Top
        ["app_title"] = new("BETELGEUSE", "BETELGEUSE"),
        ["app_sub"] = new("Автономна посадка ракетоносія", "Autonomous booster landing"),
        ["time_fmt"] = new("Час  {0:F1} с", "Time  {0:F1} s"),
        ["algo_fmt"] = new("Алгоритм:  {0}", "Algorithm:  {0}"),

        // Status
        ["st_ready"] = new("ГОТОВО", "READY"),
        ["st_wait"] = new("ОЧІКУВАННЯ", "STANDBY"),
        ["st_start"] = new("СТАРТ", "START"),
        ["st_descent"] = new("СПУСК", "DESCENT"),
        ["st_success"] = new("УСПІХ", "SUCCESS"),
        ["st_fail"] = new("НЕВДАЧА", "FAILURE"),
        ["st_stop"] = new("ЗУПИНЕНО", "STOPPED"),
        ["st_batch"] = new("АВТО-ТЕСТ", "AUTO-TEST"),

        // Modes
        ["mode_pid"] = new("Класичний PID", "Classical PID"),
        ["mode_fuzzy"] = new("Нечітка логіка (Sugeno)", "Fuzzy logic (Sugeno)"),
        ["mode_neural"] = new("Нейромережа (ES)", "Neural net (ES)"),
        ["mode_hybrid"] = new("Гібрид Neuro-Fuzzy", "Hybrid Neuro-Fuzzy"),
        ["mode_btn_a"] = new("[1]  PID", "[1]  PID"),
        ["mode_btn_b"] = new("[2]  Нечітка логіка", "[2]  Fuzzy logic"),
        ["mode_btn_c"] = new("[3]  Нейромережа", "[3]  Neural network"),
        ["mode_btn_d"] = new("[4]  Гібрид", "[4]  Hybrid"),
        ["mode_sub_a"] = new("Простий еталон", "Baseline controller"),
        ["mode_sub_b"] = new("Sugeno — правила «як пілот»", "Sugeno — pilot-like rules"),
        ["mode_sub_c"] = new("Машинне навчання", "Machine learning"),
        ["mode_sub_d"] = new("Нечітка + нейромережа", "Fuzzy + neural residual"),

        // Headers
        ["h_telem"] = new("ТЕЛЕМЕТРІЯ ПОЛЬОТУ", "FLIGHT TELEMETRY"),
        ["h_live"] = new("ЗМІНИ ПІД ЧАС ПОЛЬОТУ", "LIVE CHANGES"),
        ["h_crit"] = new("КРИТЕРІЇ М'ЯКОЇ ПОСАДКИ", "SOFT-LANDING CRITERIA"),
        ["h_insight"] = new("ВИСНОВОК СИСТЕМИ", "SYSTEM INSIGHT"),
        ["h_graphs"] = new("ГРАФІКИ В РЕАЛЬНОМУ ЧАСІ", "REAL-TIME CHARTS"),
        ["h_step1"] = new("1. АЛГОРИТМ", "1. ALGORITHM"),
        ["h_step2"] = new("2. КЕРУВАННЯ", "2. CONTROL"),
        ["h_cam"] = new("КАМЕРА", "CAMERA"),
        ["h_export"] = new("ЕКСПОРТ", "EXPORT"),
        ["h_step3"] = new("3. УМОВИ ТЕСТУ", "3. TEST CONDITIONS"),
        ["h_results"] = new("РЕЗУЛЬТАТИ %", "RESULTS %"),
        ["h_lang"] = new("МОВА / LANGUAGE", "LANGUAGE / МОВА"),

        // Metrics
        ["m_alt"] = new("Висота h", "Altitude h"),
        ["m_vy"] = new("Швидкість вниз |Vy|", "Descent speed |Vy|"),
        ["m_vh"] = new("Бічна швидкість |Vh|", "Lateral speed |Vh|"),
        ["m_thr"] = new("Тяга двигуна", "Engine thrust"),
        ["m_twr"] = new("Тягооснащеність T/W", "Thrust/weight T/W"),
        ["m_tilt"] = new("Нахил корпусу", "Body tilt"),
        ["m_rate"] = new("Кутова швидкість |ω|", "Angular rate |ω|"),
        ["m_fuel"] = new("Паливо", "Fuel"),
        ["m_fuel_pct"] = new("Залишок палива", "Fuel remaining"),
        ["m_mass"] = new("Повна маса", "Total mass"),
        ["m_miss"] = new("Промах до pad", "Pad miss distance"),
        ["m_acc"] = new("Прискорення a_y", "Acceleration a_y"),
        ["m_eta"] = new("Оцінка t до землі", "Est. time to ground"),
        ["m_score"] = new("Оцінка SuccessScore", "SuccessScore"),
        ["m_peak_vy"] = new("Пік |Vy| за політ", "Peak |Vy| this flight"),
        ["m_peak_tilt"] = new("Пік нахилу", "Peak tilt"),
        ["m_min_h"] = new("Мін. висота (live)", "Min altitude (live)"),
        ["u_m"] = new("м", "m"),
        ["u_ms"] = new("м/с", "m/s"),
        ["u_kn"] = new("кН", "kN"),
        ["u_deg"] = new("°", "°"),
        ["u_dps"] = new("°/с", "°/s"),
        ["u_kg"] = new("кг", "kg"),
        ["u_pct"] = new("%", "%"),
        ["u_t"] = new("т", "t"),
        ["u_ms2"] = new("м/с²", "m/s²"),
        ["u_s"] = new("с", "s"),
        ["u_score"] = new("/100", "/100"),

        // Actions (без emoji — шрифт їх не містить)
        ["btn_start"] = new("ЗАПУСТИТИ ПОСАДКУ", "START LANDING"),
        ["btn_stop"] = new("СТОП / ПАУЗА", "STOP / PAUSE"),
        ["btn_ideal"] = new("ІДЕАЛЬНІ ПАРАМЕТРИ (100%)", "IDEAL PRESETS (100%)"),
        ["btn_compare"] = new("ПОРІВНЯТИ ВСІ", "COMPARE ALL"),
        ["btn_cancel"] = new("СКАСУВАТИ ПОРІВНЯННЯ", "CANCEL COMPARISON"),
        ["btn_follow"] = new("СЛІДКУВАТИ ЗА РАКЕТОЮ", "FOLLOW ROCKET"),
        ["btn_traj_view"] = new("ПОВНА ТРАЄКТОРІЯ", "FULL TRAJECTORY"),
        ["btn_manual"] = new("РУЧНЕ КЕРУВАННЯ", "MANUAL CONTROL"),
        ["btn_reset_cam"] = new("СКИНУТИ РАКУРС", "RESET VIEW"),
        ["btn_traj_on"] = new("ЛІНІЯ ТРАЄКТОРІЇ: УВІМК", "TRAJECTORY LINE: ON"),
        ["btn_traj_off"] = new("ЛІНІЯ ТРАЄКТОРІЇ: ВИМК", "TRAJECTORY LINE: OFF"),
        ["btn_export"] = new("ЕКСПОРТУВАТИ РЕЗУЛЬТАТИ", "EXPORT RESULTS"),
        ["btn_folder"] = new("ВІДКРИТИ ПАПКУ ЗВІТІВ", "OPEN REPORTS FOLDER"),
        ["btn_lang"] = new("ENGLISH", "УКРАЇНСЬКА"),
        ["btn_theme"] = new("ТЕМА", "THEME"),
        ["btn_ok"] = new("ЗРОЗУМІЛО", "GOT IT"),
        ["btn_show_traj"] = new("Траєкторія", "Trajectory"),
        ["btn_export_short"] = new("Експортувати", "Export"),

        // Camera
        ["cam_follow"] = new("Слідкування за ракетою", "Following rocket"),
        ["cam_manual"] = new("Ручне керування", "Manual control"),
        ["cam_overview"] = new("Повна траєкторія", "Full trajectory"),
        ["cam_prefix"] = new("Камера: ", "Camera: "),
        ["cam_help"] = new("ЛКМ/ПКМ — оберт (можна знизу) · WASD · колесо — плавний зум\nF слідкувати · T вся траєкторія · C ручне · R скинути",
            "LMB/RMB — orbit (look under OK) · WASD · scroll — smooth zoom\nF follow · T full path · C manual · R reset"),

        // How-to
        ["how"] = new("[1-4] алгоритм  ->  [Space] запуск",
            "[1-4] algorithm  ->  [Space] start"),
        ["hint"] = new("① [4] Гібрид   ② [Space] Запуск   ③ Успіх/Невдача",
            "① [4] Hybrid   ② [Space] Start   ③ Success/Fail"),
        ["tip"] = new("1-4 алгоритм · I ідеал · Y тема · G мова · Space старт · Esc стоп · E експорт · H панелі",
            "1-4 algorithm · I ideal · Y theme · G lang · Space start · Esc stop · E export · H panels"),
        ["bottom"] = new("Успіх: |Vy|<3.5 · нахил<7° · промах<25м · |Vh|<5  ·  I ідеал · Y тема · Space старт · E експорт",
            "Success: |Vy|<3.5 · tilt<7° · miss<25m · |Vh|<5  ·  I ideal · Y theme · Space start · E export"),
        ["graph_hint"] = new("min/max · поточне · 0 = жовта лінія",
            "min/max · current · 0 = yellow line"),

        // Sliders
        ["sl_tests"] = new("Запусків на алгоритм", "Runs per algorithm"),
        ["sl_wind"] = new("Сила вітру", "Wind strength"),
        ["sl_time"] = new("Прискорення часу (тест)", "Time scale (test)"),
        ["tg_noise"] = new("Випадкові збурення", "Random disturbances"),
        ["tg_train"] = new("Навчати нейромережу", "Train neural network"),

        // Results
        ["winner_none"] = new("Переможець: ще не визначено", "Winner: not determined yet"),
        ["winner_fmt"] = new("Переможець: {0}  ({1:F1}%)", "Winner: {0}  ({1:F1}%)"),
        ["res_ok"] = new("ПОСАДКА УСПІШНА", "LANDING SUCCESSFUL"),
        ["res_fail"] = new("ПОСАДКА НЕВДАЛА", "LANDING FAILED"),
        ["res_footer"] = new("\n\nТраєкторія · Експорт звіту · ЗАПУСТИТИ — ще раз",
            "\n\nTrajectory · Export report · START — again"),

        // Insights
        ["ins_wait"] = new("Оберіть алгоритм (D — гібрид) і натисніть «ЗАПУСТИТИ ПОСАДКУ».",
            "Select algorithm (D — hybrid) and press START LANDING."),
        ["ins_batch"] = new("Йде авто-тест Monte-Carlo. Алгоритми змінюються автоматично.",
            "Monte-Carlo auto-test running. Algorithms switch automatically."),
        ["ins_ok"] = new("Посадку виконано. Score {0:F0}/100. Експортуйте звіт.",
            "Landing complete. Score {0:F0}/100. Export the report."),
        ["ins_high_low_twr"] = new("Високий спуск: тяга нижче зависання — гальмування ближче до землі.",
            "High altitude: thrust below hover — braking expected lower."),
        ["ins_high_ok"] = new("Високий спуск: контролер тримає профіль. Слідкуйте за нахилом.",
            "High altitude: controller holds profile. Watch tilt."),
        ["ins_fast"] = new("Швидкість висока — потрібне сильне гальмування. T/W > 1.",
            "Speed high — strong braking needed. T/W > 1."),
        ["ins_tilt"] = new("Нахил перевищує норму. Gimbal має вирівняти корпус.",
            "Tilt exceeds limit. Gimbal should upright the vehicle."),
        ["ins_miss"] = new("Відхилення від pad велике. Бічна корекція в пріоритеті.",
            "Large pad miss. Lateral correction is priority."),
        ["ins_mid"] = new("Середня висота. ETA≈{0:F0} с. Параметри в нормі.",
            "Mid altitude. ETA≈{0:F0} s. Parameters nominal."),
        ["ins_term_ok"] = new("Фінальний етап: умови м'якої посадки виконуються.",
            "Terminal phase: soft-landing conditions met."),
        ["ins_term_v"] = new("Критично: |Vy|={0:F1} > {1}. Потрібне гальмування.",
            "Critical: |Vy|={0:F1} > {1}. Braking required."),
        ["ins_term_bad"] = new("Фінал: {0} критерій(ї) поза нормою.",
            "Terminal: {0} criterion(s) out of range."),

        // Messages
        ["msg_cancel_first"] = new("Спочатку скасуйте авто-тест.", "Cancel auto-test first."),
        ["msg_started"] = new("Посадка: {0}. LMB — оберт навколо ракети.", "Landing: {0}. LMB — orbit around rocket."),
        ["msg_stopped"] = new("Політ зупинено. ЗАПУСТИТИ — знову.", "Flight stopped. START — again."),
        ["msg_ideal"] = new("{0}", "{0}"),
        ["msg_ideal_ok"] = new("Ідеальні параметри виставлено. ЗАПУСТИТИ — м’яка посадка.",
            "Ideal presets applied. START — soft landing."),
        ["ins_ideal_hint"] = new("Ідеал [I] — гарантований номінал. Без нього алгоритми різняться (вітер/шум).",
            "Ideal [I] — guaranteed nominal. Without it algorithms differ (wind/noise)."),
        ["msg_traj_on"] = new("Лінію траєкторії увімкнено.", "Trajectory line enabled."),
        ["msg_traj_off"] = new("Лінію траєкторії вимкнено.", "Trajectory line disabled."),
        ["msg_export_ok"] = new("Звіт збережено:\n{0}", "Report saved:\n{0}"),
        ["msg_export_cmp"] = new("Експортовано порівняння:\n{0}", "Comparison exported:\n{0}"),
        ["msg_no_data"] = new("Немає даних. Спочатку посадка або авто-тест.", "No data. Run a landing or auto-test first."),
        ["msg_folder"] = new("{0}", "{0}"),
        ["msg_cam_follow"] = new("Камера слідкує за ракетою. LMB — оберт.", "Camera follows rocket. LMB — orbit."),
        ["msg_cam_manual"] = new("Ручне: LMB/WASD — оберт · scroll — зум.", "Manual: LMB/WASD — orbit · scroll — zoom."),
        ["msg_cam_traj"] = new("Повна траєкторія. LMB — оберт · scroll — зум.", "Full trajectory. LMB — orbit · scroll — zoom."),
        ["msg_cam_reset"] = new("Ракурс скинуто.", "View reset."),
        ["msg_selected"] = new("Обрано: {0}\nНатисніть ЗАПУСТИТИ ПОСАДКУ.", "Selected: {0}\nPress START LANDING."),
        ["msg_compare"] = new("Авто-тест: PID->Fuzzy->NN->Hybrid. Прогрес зверху.", "Auto-test: PID->Fuzzy->NN->Hybrid. Progress on top."),
        ["msg_compare_done"] = new("Авто-тест завершено.\n{0} ({1:F1}%).\nЕкспорт у SimulationLogs/.",
            "Auto-test done.\n{0} ({1:F1}%).\nExport in SimulationLogs/."),
        ["stat_a"] = new("A  PID", "A  PID"),
        ["stat_b"] = new("B  Нечітка", "B  Fuzzy"),
        ["stat_c"] = new("C  Нейромережа", "C  Neural"),
        ["stat_d"] = new("D  Гібрид", "D  Hybrid"),
    };
}
