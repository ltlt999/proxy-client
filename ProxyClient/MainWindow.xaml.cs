using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using ProxyClient.ViewModels;
using Wpf.Ui.Controls;
using Button = System.Windows.Controls.Button;
using MessageBoxButton = System.Windows.MessageBoxButton;
using MessageBoxImage = System.Windows.MessageBoxImage;

namespace ProxyClient;

public partial class MainWindow : FluentWindow
{
    private readonly MainViewModel _vm;
    internal bool _forceHiddenStart;

    public MainWindow()
    {
        InitializeComponent();
        _vm = new MainViewModel();
        DataContext = _vm;
        _vm.StatusChanged += OnStatusChanged;
        ((System.Collections.Specialized.INotifyCollectionChanged)LogList.Items).CollectionChanged += (_, _) =>
        {
            if (LogList.Items.Count > 0)
                LogList.ScrollIntoView(LogList.Items[LogList.Items.Count - 1]);
        };
    }

    private void OnStatusChanged(object? sender, bool coreRunning)
        => (App.Current as App)?.ReportStatus(coreRunning, _vm.SystemProxyOn);

    private void ServerList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource is DependencyObject src && FindAncestor<ListBoxItem>(src) == null)
            return;
        if (_vm.SelectedServer != null)
            _vm.ActivateCommand.Execute(null);
    }

    private void ServerList_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource is DependencyObject src && FindAncestor<ListBoxItem>(src) is { } item)
            item.IsSelected = true;
    }

    private static T? FindAncestor<T>(DependencyObject? node) where T : DependencyObject
    {
        while (node != null && node is not T)
            node = VisualTreeHelper.GetParent(node);
        return node as T;
    }

    private void AddMenu_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button b && b.ContextMenu != null)
        {
            b.ContextMenu.PlacementTarget = b;
            b.ContextMenu.Placement = PlacementMode.Bottom;
            b.ContextMenu.IsOpen = true;
        }
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        _vm.AutoStartIfNeeded();
    }

    private void Window_Closing(object sender, CancelEventArgs e)
    {
        if (App.Current is App app && !app.HandleWindowClose())
        {
            e.Cancel = true;
            return;
        }
        _vm.OnWindowClosing();
    }

    private void Settings_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new SettingsWindow(App.Data.Settings) { Owner = this };
        dlg.ShowDialog();
        _vm.SyncSettings();
    }

    private void About_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show(
            "ProxyClient v1.0\n\n" +
            "基于 Xray-core 的现代化轻量代理客户端\n\n" +
            "支持协议：VMess / VLESS / Trojan / Shadowsocks\n" +
            "功能：订阅导入  分享链接解析  规则/全局路由\n" +
            "托盘常驻  开机自启  启动后自动连接\n\n" +
            "本地端口：SOCKS5 10808  HTTP 10809\n" +
            "关闭按钮可最小化到托盘",
            "关于 ProxyClient", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void Exit_Click(object sender, RoutedEventArgs e) => Close();
}
