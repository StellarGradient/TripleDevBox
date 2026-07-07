using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Windows.Graphics;
using WinRT.Interop;

namespace TripleDevBox;

public sealed partial class MainWindow : Window
{
    // Desired client size in device-independent (96-DPI) pixels.
    private const int LogicalWidth = 1280;
    private const int LogicalHeight = 900;

    private bool _sized;

    [DllImport("user32.dll")]
    private static extern uint GetDpiForWindow(nint hwnd);

    public MainWindow()
    {
        InitializeComponent();
        Title = "Triple Dev Box";

        // Window title-bar + taskbar icon (the .ico is copied next to the exe).
        string iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "AppIcon.ico");
        if (File.Exists(iconPath))
        {
            AppWindow.SetIcon(iconPath);
        }

        // Size once the window is activated: only then is it associated with its
        // target monitor, so GetDpiForWindow returns the real (not default 96) DPI.
        Activated += OnFirstActivated;
    }

    private void OnFirstActivated(object sender, WindowActivatedEventArgs e)
    {
        if (_sized) return;
        _sized = true;
        Activated -= OnFirstActivated;

        nint hwnd = WindowNative.GetWindowHandle(this);
        uint dpi = GetDpiForWindow(hwnd);
        double scale = dpi == 0 ? 1.0 : dpi / 96.0;

        AppWindow appWindow = AppWindow;
        DisplayArea display = DisplayArea.GetFromWindowId(appWindow.Id, DisplayAreaFallback.Primary);

        int width = (int)(LogicalWidth * scale);
        int height = (int)(LogicalHeight * scale);

        if (display is not null)
        {
            // Never exceed the monitor's work area (keep a small margin).
            width = Math.Min(width, display.WorkArea.Width - 40);
            height = Math.Min(height, display.WorkArea.Height - 40);
        }

        appWindow.Resize(new SizeInt32(width, height));

        if (display is not null)
        {
            int x = display.WorkArea.X + Math.Max(0, (display.WorkArea.Width - width) / 2);
            int y = display.WorkArea.Y + Math.Max(0, (display.WorkArea.Height - height) / 2);
            appWindow.Move(new PointInt32(x, y));
        }
    }
}
