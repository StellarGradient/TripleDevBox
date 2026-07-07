using System;
using System.IO;
using Microsoft.UI.Xaml;

namespace TripleDevBox;

public partial class App : Application
{
    private Window? _window;

    private static readonly string LogPath =
        Path.Combine(Path.GetTempPath(), "TripleDevBox_crash.log");

    public App()
    {
        InitializeComponent();

        UnhandledException += (_, e) =>
        {
            Log("Application.UnhandledException", e.Exception);
        };
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            Log("AppDomain.UnhandledException", e.ExceptionObject as Exception);
        };
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        try
        {
            _window = new MainWindow();
            _window.Activate();
        }
        catch (Exception ex)
        {
            Log("OnLaunched", ex);
            throw;
        }
    }

    private static void Log(string where, Exception? ex)
    {
        try
        {
            File.AppendAllText(LogPath,
                $"[{where}] {DateTime.Now:O}{Environment.NewLine}{ex}{Environment.NewLine}{new string('-', 60)}{Environment.NewLine}");
        }
        catch
        {
            // best-effort logging only
        }
    }
}
