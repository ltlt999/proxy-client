using System.Windows;
using ProxyClient.Core;
using ProxyClient.Storage;

namespace ProxyClient;

public partial class SettingsWindow : Window
{
    private readonly AppSettings _settings;

    public SettingsWindow(AppSettings settings)
    {
        InitializeComponent();
        _settings = settings;
        AutoStartChk.IsChecked = AutoStartHelper.IsEnabled();
        MinimizeOnStartChk.IsChecked = settings.MinimizeOnStart;
        MinimizeOnCloseChk.IsChecked = settings.MinimizeOnClose;
        AutoStartCoreChk.IsChecked = settings.AutoStartCore;
    }

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        var autoStart = AutoStartChk.IsChecked == true;
        try { AutoStartHelper.SetEnabled(autoStart); }
        catch (System.Exception ex)
        {
            MessageBox.Show("设置开机自启失败: " + ex.Message, "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        _settings.AutoStartWithWindows = autoStart;
        _settings.MinimizeOnStart = MinimizeOnStartChk.IsChecked == true;
        _settings.MinimizeOnClose = MinimizeOnCloseChk.IsChecked == true;
        _settings.AutoStartCore = AutoStartCoreChk.IsChecked == true;
        DialogResult = true;
    }
}
