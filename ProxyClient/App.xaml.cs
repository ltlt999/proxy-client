using System.IO;
using System.Windows;
using System.Windows.Media;
using ProxyClient.Core;
using ProxyClient.Storage;
using Wpf.Ui.Appearance;

namespace ProxyClient;

public partial class App : Application
{
    private TrayIconManager? _tray;
    private bool _reallyExit;
    private bool _startMinimized;

    public static AppData Data { get; private set; } = new();

    public App()
    {
        InitializeComponent();

        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
            LogCrash($"UnhandledException: {args.ExceptionObject}");
        DispatcherUnhandledException += (_, args) =>
        {
            LogCrash($"DispatcherUnhandledException: {args.Exception}");
            args.Handled = true;
        };
        TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            LogCrash($"UnobservedTaskException: {args.Exception}");
            args.SetObserved();
        };
    }

    private static void LogCrash(object? ex)
    {
        try
        {
            var dir = Path.Combine(AppContext.BaseDirectory, "data");
            Directory.CreateDirectory(dir);
            File.AppendAllText(Path.Combine(dir, "crash.log"),
                $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {ex}{Environment.NewLine}{Environment.NewLine}");
        }
        catch { }
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Apply WPF-UI dark theme and tint the system accent with our brand color,
        // so every Fluent control (toggle / selection / focus ring) stays on-brand.
        try
        {
            ApplicationThemeManager.Apply(ApplicationTheme.Light);
            ApplicationAccentColorManager.Apply(Color.FromRgb(0x00, 0x7A, 0xFF), ApplicationTheme.Light);
        }
        catch { /* theme apply is best-effort; never block startup */ }

        Data = ConfigStore.Load();

        XrayCoreManager.CleanupPorts();

        _tray = new TrayIconManager();
        _tray.ShowRequested += OnTrayShow;
        _tray.ExitRequested += OnTrayExit;
        _tray.ToggleProxyRequested += OnTrayToggleProxy;
        _tray.Show();

        _startMinimized = Data.Settings.MinimizeOnStart || AutoStartHelper.ShouldStartMinimized();

        var window = new MainWindow { _forceHiddenStart = _startMinimized };
        MainWindow = window;
        if (_startMinimized)
        {
            window.ShowInTaskbar = false;
            window.WindowState = WindowState.Minimized;
            window.Hide();
        }
        else
        {
            window.Show();
        }

        if (_startMinimized)
            _tray.ShowBalloon("ProxyClient", "已在后台运行,双击托盘图标显示窗口。");
    }

    public void ReportStatus(bool coreRunning, bool proxyOn) => _tray?.SetStatus(coreRunning, proxyOn);

    private void OnTrayShow(object? sender, System.EventArgs e)
    {
        var w = MainWindow;
        if (w == null) return;
        w.ShowInTaskbar = true;
        w.WindowState = WindowState.Normal;
        w.Show();
        w.Activate();
    }

    private void OnTrayToggleProxy(object? sender, System.EventArgs e)
    {
        if (MainWindow?.DataContext is ViewModels.MainViewModel vm)
            vm.ToggleProxyCommand.Execute(null);
    }

    private void OnTrayExit(object? sender, System.EventArgs e)
    {
        _reallyExit = true;
        if (MainWindow != null) MainWindow.Close();
        Shutdown();
    }

    public bool HandleWindowClose()
    {
        if (_reallyExit || !Data.Settings.MinimizeOnClose)
            return true;
        MainWindow?.Hide();
        MainWindow!.ShowInTaskbar = false;
        _tray?.ShowBalloon("ProxyClient", "已最小化到托盘,双击图标恢复。");
        return false;
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _tray?.Dispose();
        base.OnExit(e);
    }
}
