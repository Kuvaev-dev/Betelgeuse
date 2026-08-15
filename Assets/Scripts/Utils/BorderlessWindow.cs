using UnityEngine;

/// <summary>
/// Borderless / frameless player window for custom HUD chrome (− □ ×).
/// Works in standalone; Editor gets safe fallbacks (notify + resolution change).
/// </summary>
public static class BorderlessWindow
{
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
    const int GWL_STYLE = -16;
    const int WS_CAPTION = 0x00C00000;
    const int WS_THICKFRAME = 0x00040000;
    const int WS_MINIMIZEBOX = 0x00020000;
    const int WS_MAXIMIZEBOX = 0x00010000;
    const int WS_SYSMENU = 0x00080000;
    const int SW_MINIMIZE = 6;
    const int SW_RESTORE = 9;

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    static extern System.IntPtr GetActiveWindow();

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    static extern System.IntPtr GetForegroundWindow();

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    static extern int GetWindowLong(System.IntPtr hWnd, int nIndex);

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    static extern int SetWindowLong(System.IntPtr hWnd, int nIndex, int dwNewLong);

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    static extern bool ShowWindow(System.IntPtr hWnd, int nCmdShow);

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    static extern bool SetWindowPos(System.IntPtr hWnd, System.IntPtr hWndInsertAfter,
        int X, int Y, int cx, int cy, uint uFlags);

    const uint SWP_FRAMECHANGED = 0x0020;
    const uint SWP_NOMOVE = 0x0002;
    const uint SWP_NOSIZE = 0x0001;
    const uint SWP_NOZORDER = 0x0004;

    static System.IntPtr Hwnd
    {
        get
        {
            var h = GetActiveWindow();
            if (h == System.IntPtr.Zero) h = GetForegroundWindow();
            return h;
        }
    }
#endif

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Boot()
    {
#if !UNITY_EDITOR
        Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
        Screen.fullScreen = true;
#endif
    }

    public static void ApplyBorderlessChrome()
    {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        try
        {
            var h = Hwnd;
            if (h == System.IntPtr.Zero) return;
            int style = GetWindowLong(h, GWL_STYLE);
            style &= ~(WS_CAPTION | WS_THICKFRAME | WS_MINIMIZEBOX | WS_MAXIMIZEBOX | WS_SYSMENU);
            SetWindowLong(h, GWL_STYLE, style);
            SetWindowPos(h, System.IntPtr.Zero, 0, 0, 0, 0,
                SWP_FRAMECHANGED | SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER);
        }
        catch { /* ignore */ }
#endif
    }

    public static void GoFullscreen()
    {
        Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
        Screen.fullScreen = true;
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        // Next frame: strip any residual chrome
        ApplyBorderlessChrome();
#endif
    }

    public static void GoWindowed(int w = 1600, int h = 900)
    {
        int sw = Mathf.Max(1280, Display.main.systemWidth);
        int sh = Mathf.Max(720, Display.main.systemHeight);
        w = Mathf.Clamp(w, 1024, sw - 48);
        h = Mathf.Clamp(h, 576, sh - 72);
        Screen.fullScreen = false;
        Screen.fullScreenMode = FullScreenMode.Windowed;
        Screen.SetResolution(w, h, FullScreenMode.Windowed);
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        // Delay style strip until window exists
        ApplyBorderlessChrome();
#endif
    }

    public static void ToggleFullscreen()
    {
        bool isFs = Screen.fullScreen
                    || Screen.fullScreenMode == FullScreenMode.FullScreenWindow
                    || Screen.fullScreenMode == FullScreenMode.ExclusiveFullScreen;
        if (isFs)
            GoWindowed();
        else
            GoFullscreen();
    }

    public static void Minimize()
    {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        try
        {
            var h = Hwnd;
            if (h != System.IntPtr.Zero)
                ShowWindow(h, SW_MINIMIZE);
        }
        catch { /* ignore */ }
#elif UNITY_EDITOR
        // Editor: no OS minimize of Game view — drop to windowed as feedback
        GoWindowed(1280, 720);
#else
        GoWindowed(1280, 720);
#endif
    }
}
