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

    public static string ModeNameShort(RocketPhysics.ControlMode m) => m switch
    {
        RocketPhysics.ControlMode.Fuzzy => T("mode_short_fuzzy"),
        RocketPhysics.ControlMode.Neural => T("mode_short_neural"),
        RocketPhysics.ControlMode.Hybrid => T("mode_short_hybrid"),
        _ => T("mode_short_pid")
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
        ["app_sub"] = new("Посадка ракетоносія", "Booster landing"),
        ["time_fmt"] = new("t {0:F1}s", "t {0:F1}s"),
        ["algo_fmt"] = new("{0}", "{0}"),
        ["top_path"] = new("ШЛЯХ", "PATH"),
        ["top_path_on"] = new("ШЛЯХ", "PATH"),
        ["top_path_off"] = new("ШЛЯХ", "PATH"),
        ["top_start"] = new("СТАРТ", "START"),
        ["top_stop"] = new("СТОП", "STOP"),
        ["top_pause"] = new("ПАУЗА", "PAUSE"),
        ["top_resume"] = new("ДАЛІ", "RESUME"),
        ["top_ideal"] = new("ІДЕАЛ", "IDEAL"),
        ["top_view"] = new("ОГЛЯД", "VIEW"),
        ["top_export"] = new("ЕКСПОРТ", "EXPORT"),
        ["top_hide"] = new("СХОВАТИ", "HIDE"),
        ["top_exit"] = new("ВИХІД", "EXIT"),
        ["top_fs"] = new("□", "□"),
        ["top_min"] = new("_", "_"),
        ["top_show"] = new("ПОКАЗАТИ", "SHOW"),

        // Status (короткі — вміщуються в бейдж ~118 px)
        ["st_ready"] = new("ГОТОВО ДО ПОСАДКИ", "READY TO LAND"),
        ["st_wait"] = new("ОЧІКУВАННЯ", "WAITING"),
        ["st_start"] = new("СТАРТ ПОСАДКИ", "LANDING START"),
        ["st_descent"] = new("СПУСК", "DESCENT"),
        ["st_success"] = new("ПОСАДКА УСПІШНА", "LANDING SUCCESSFUL"),
        ["st_fail"] = new("ПОСАДКА НЕВДАЛА", "LANDING FAILED"),
        ["st_stop"] = new("ЗУПИНЕНО", "STOPPED"),
        ["st_pause"] = new("ПАУЗА", "PAUSED"),
        ["st_batch"] = new("АВТО-ТЕСТ", "AUTO-TEST"),

        // Modes
        ["mode_pid"] = new("Класичний PID", "Classical PID"),
        ["mode_fuzzy"] = new("Нечітка логіка (Sugeno)", "Fuzzy logic (Sugeno)"),
        ["mode_neural"] = new("Нейромережа (ES)", "Neural net (ES)"),
        ["mode_hybrid"] = new("Гібрид Neuro-Fuzzy", "Hybrid Neuro-Fuzzy"),
        // Short names for top mode pill (full names stay for messages)
        ["mode_short_pid"] = new("PID", "PID"),
        ["mode_short_fuzzy"] = new("Fuzzy", "Fuzzy"),
        ["mode_short_neural"] = new("Neural", "Neural"),
        ["mode_short_hybrid"] = new("Hybrid", "Hybrid"),
        ["mode_btn_a"] = new("1  PID", "1  PID"),
        ["mode_btn_b"] = new("2  Fuzzy", "2  Fuzzy"),
        ["mode_btn_c"] = new("3  Neural", "3  Neural"),
        ["mode_btn_d"] = new("4  Hybrid", "4  Hybrid"),
        ["mode_sub_a"] = new("еталон", "baseline"),
        ["mode_sub_b"] = new("Sugeno", "Sugeno"),
        ["mode_sub_c"] = new("MLP + ES", "MLP + ES"),
        ["mode_sub_d"] = new("Fuzzy+NN", "Fuzzy+NN"),

        // Headers (left panel — short, scannable)
        ["h_telem"] = new("ПОЛІТ", "FLIGHT"),
        ["h_primary"] = new("ГОЛОВНЕ", "PRIMARY"),
        ["h_dyn"] = new("ДИНАМІКА", "DYNAMICS"),
        ["h_prop"] = new("РУШІЙ", "PROPULSION"),
        ["h_live"] = new("ПІКИ / ДЕЛЬТА", "PEAKS / DELTA"),
        ["h_crit"] = new("КРИТЕРІЇ ПОСАДКИ", "LANDING GATE"),
        ["h_insight"] = new("ПІДКАЗКА", "GUIDANCE"),
        ["h_graphs"] = new("ГРАФІКИ", "CHARTS"),
        ["h_step1"] = new("АЛГОРИТМ", "ALGORITHM"),
        ["h_step2"] = new("ПОРІВНЯННЯ", "COMPARE"),
        ["h_cam"] = new("КАМЕРА", "CAMERA"),
        ["h_export"] = new("ЕКСПОРТ", "EXPORT"),
        ["h_step3"] = new("УМОВИ ТЕСТУ", "TEST SETUP"),
        ["h_results"] = new("РЕЗУЛЬТАТИ %", "SUCCESS %"),
        ["h_lang"] = new("МОВА / LANGUAGE", "LANGUAGE / МОВА"),
        ["h_how"] = new("ШВИДКИЙ СТАРТ", "QUICK START"),
        ["h_msg"] = new("ПОВІДОМЛЕННЯ", "STATUS"),

        // Metrics — short labels for dense left column
        ["m_alt"] = new("Висота", "Altitude"),
        ["m_vy"] = new("|Vy| вниз", "|Vy| down"),
        ["m_vh"] = new("|Vh| бічна", "|Vh| lateral"),
        ["m_thr"] = new("Тяга", "Thrust"),
        ["m_twr"] = new("T/W", "T/W"),
        ["m_tilt"] = new("Нахил", "Tilt"),
        ["m_rate"] = new("|ω|", "|ω|"),
        ["m_fuel"] = new("Паливо", "Fuel"),
        ["m_fuel_pct"] = new("Паливо %", "Fuel %"),
        ["m_mass"] = new("Маса", "Mass"),
        ["m_miss"] = new("Промах", "Miss"),
        ["m_acc"] = new("a_y", "a_y"),
        ["m_eta"] = new("ETA", "ETA"),
        ["m_score"] = new("Оцінка", "Score"),
        ["m_peak_vy"] = new("Пік |Vy|", "Peak |Vy|"),
        ["m_peak_tilt"] = new("Пік нахилу", "Peak tilt"),
        ["m_min_h"] = new("Мін. h", "Min h"),
        ["crit_vy"] = new("|Vy|", "|Vy|"),
        ["crit_tilt"] = new("Нахил", "Tilt"),
        ["crit_miss"] = new("Промах", "Miss"),
        ["crit_vh"] = new("|Vh|", "|Vh|"),
        ["graph_hint"] = new("мін/макс, поточне", "min/max, current"),
        ["u_m"] = new("м", "m"),
        ["u_ms"] = new("м/с", "m/s"),
        ["u_kn"] = new("кН", "kN"),
        ["u_deg"] = new("град", "deg"),
        ["u_dps"] = new("град/с", "deg/s"),
        ["u_kg"] = new("кг", "kg"),
        ["u_pct"] = new("%", "%"),
        ["u_t"] = new("т", "t"),
        ["u_ms2"] = new("м/с2", "m/s2"),
        ["u_s"] = new("с", "s"),
        ["u_score"] = new("/100", "/100"),

        // Actions (без emoji — шрифт їх не містить)
        ["btn_start"] = new("ЗАПУСТИТИ ПОСАДКУ", "START LANDING"),
        ["btn_stop"] = new("СТОП / ПАУЗА", "STOP / PAUSE"),
        ["btn_ideal"] = new("ІДЕАЛЬНІ ПАРАМЕТРИ (100%)", "IDEAL PRESETS (100%)"),
        ["btn_compare"] = new("ПОРІВНЯТИ  [P]", "COMPARE  [P]"),
        ["btn_cancel"] = new("СКАСУВАТИ  [X]", "CANCEL  [X]"),
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
        ["how"] = new("1-4: режим   Space: старт   Esc: стоп   H: панелі   G: мова   Y: тема",
            "1-4: mode   Space: start   Esc: stop   H: panels   G: lang   Y: theme"),
        ["hint"] = new("Підказка: оберіть Hybrid [4], потім Start [Space]",
            "Hint: pick Hybrid [4], then Start [Space]"),
        ["tip"] = new("1-4 mode | I ideal | Space start | Esc stop | P compare | E export | H hide | G lang | Y theme",
            "1-4 mode | I ideal | Space start | Esc stop | P compare | E export | H hide | G lang | Y theme"),
        ["cam_keys"] = new("F follow | T огляд | C ручне | R скинути | L шлях",
            "F follow | T overview | C manual | R reset | L path"),
        // Sliders — what changes + unit in value column
        ["sl_tests"] = new("Запусків на алгоритм", "Runs per algorithm"),
        ["sl_wind"] = new("Швидкість вітру", "Wind speed"),
        ["sl_time"] = new("Прискорення часу", "Time scale"),
        ["sl_tests_u"] = new("зап.", "runs"),
        ["sl_wind_u"] = new("м/с", "m/s"),
        ["sl_time_u"] = new("x", "x"),
        ["tg_noise"] = new("Шум маси/кута", "Mass/angle noise"),
        ["tg_train"] = new("Навчання NN", "Train NN"),

        // Flight phase strip (bottom)
        ["step_ready"] = new("Крок: готовність | оберіть алгоритм і Start", "Step: ready | pick algorithm and Start"),
        ["step_high"] = new("Крок: високий спуск | профіль швидкості", "Step: high descent | speed profile"),
        ["step_approach"] = new("Крок: підхід | гальмування + вирівнювання", "Step: approach | brake + upright"),
        ["step_powered"] = new("Крок: активне гальмування | T/W > 1", "Step: powered descent | T/W > 1"),
        ["step_terminal"] = new("Крок: термінал | м'яка посадка h<25 м", "Step: terminal | soft landing h<25 m"),
        ["step_soft"] = new("Крок: м'яке торкання | мала |Vy|", "Step: soft touch | low |Vy|"),
        ["step_touch"] = new("Крок: контакт із pad", "Step: pad contact"),
        ["step_ok"] = new("Крок: посадка успішна", "Step: landing success"),
        ["step_fail"] = new("Крок: посадка невдала", "Step: landing failed"),
        ["step_stop"] = new("Крок: політ зупинено", "Step: flight stopped"),
        ["step_batch"] = new("Крок: авто-порівняння алгоритмів", "Step: auto algorithm compare"),

        // Results
        ["winner_none"] = new("Переможець: —", "Winner: --"),
        ["winner_fmt"] = new("Переможець: {0}  {1:F0}%", "Winner: {0}  {1:F0}%"),
        ["stat_none"] = new("-- %", "-- %"),
        ["res_ok"] = new("ПОСАДКА УСПІШНА", "LANDING SUCCESSFUL"),
        ["res_fail"] = new("ПОСАДКА НЕВДАЛА", "LANDING FAILED"),
        ["res_footer"] = new("\nТраєкторія · Експорт звіту · START — ще раз",
            "\nTrajectory · Export report · START — again"),
        ["res_m_v"] = new("Швидкість", "Velocity"),
        ["res_m_tilt"] = new("Нахил", "Tilt"),
        ["res_m_miss"] = new("Промах", "Miss"),
        ["res_m_hv"] = new("Бічна V", "Lateral V"),
        ["res_m_score"] = new("Оцінка", "Score"),
        ["res_ok_sub"] = new("Усі критерії soft-landing виконано · t={0:F0} с · паливо {1:F0} кг",
            "All soft-landing criteria met · t={0:F0} s · fuel {1:F0} kg"),
        ["res_fail_sub"] = new("Порушені критерії — див. картки нижче",
            "Criteria failed — see cards below"),

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
        ["msg_paused"] = new("Пауза. ПАУЗА / ДАЛІ — продовжити.", "Paused. PAUSE / RESUME — continue."),
        ["msg_resumed"] = new("Політ продовжено.", "Flight resumed."),
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
        ["msg_cam_traj"] = new("Огляд траєкторії. T або F — вийти · LMB — оберт · scroll — зум.",
            "Trajectory overview. T or F — exit · LMB — orbit · scroll — zoom."),
        ["msg_cam_reset"] = new("Ракурс скинуто.", "View reset."),
        ["msg_selected"] = new("Обрано: {0}\nНатисніть ЗАПУСТИТИ ПОСАДКУ.", "Selected: {0}\nPress START LANDING."),
        ["msg_compare"] = new("Авто-тест: PID->Fuzzy->NN->Hybrid. Прогрес зверху.", "Auto-test: PID->Fuzzy->NN->Hybrid. Progress on top."),
        ["msg_compare_zero"] = new(
            "Авто-тест: усі 0%. Зменш вітер/шум або повтори після оновлення симуляції.",
            "Auto-test: all 0%. Lower wind/noise or retry after the simulation fix."),
        ["msg_compare_done"] = new("Авто-тест завершено.\n{0} ({1:F1}%).\nЕкспорт у SimulationLogs/.",
            "Auto-test done.\n{0} ({1:F1}%).\nExport in SimulationLogs/."),
        ["stat_a"] = new("A  PID", "A  PID"),
        ["stat_b"] = new("B  Нечітка", "B  Fuzzy"),
        ["stat_c"] = new("C  Нейромережа", "C  Neural"),
        ["stat_d"] = new("D  Гібрид", "D  Hybrid"),
    };
}
