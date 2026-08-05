using System.Windows;
using ProxyClient.Core;
using ProxyClient.Models;
using ProxyClient.Parsers;
using ProxyClient.Storage;
using Wpf.Ui.Controls;
using MessageBoxButton = System.Windows.MessageBoxButton;
using MessageBoxImage = System.Windows.MessageBoxImage;

namespace ProxyClient;

public partial class SettingsWindow : FluentWindow
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
        RulesList.ItemsSource = settings.CustomRules;
    }

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        _settings.MinimizeOnStart = MinimizeOnStartChk.IsChecked == true;
        _settings.MinimizeOnClose = MinimizeOnCloseChk.IsChecked == true;
        _settings.AutoStartCore = AutoStartCoreChk.IsChecked == true;

        var autoStart = AutoStartChk.IsChecked == true;
        try { AutoStartHelper.SetEnabled(autoStart, _settings.MinimizeOnStart); }
        catch (System.Exception ex)
        {
            MessageBox.Show("设置开机自启失败: " + ex.Message, "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        _settings.AutoStartWithWindows = autoStart;
        DialogResult = true;
    }

    private void AddRule_Click(object sender, RoutedEventArgs e)
    {
        var rule = new RoutingRule();
        var dlg = new RoutingRuleEditWindow(rule) { Owner = this };
        if (dlg.ShowDialog() == true)
        {
            _settings.CustomRules.Add(rule);
            RulesList.Items.Refresh();
        }
    }

    private void EditRule_Click(object sender, RoutedEventArgs e)
    {
        if (RulesList.SelectedItem is not RoutingRule rule) return;
        var copy = new RoutingRule { Type = rule.Type, Value = rule.Value, Action = rule.Action };
        var dlg = new RoutingRuleEditWindow(copy) { Owner = this };
        if (dlg.ShowDialog() == true)
        {
            rule.Type = copy.Type;
            rule.Value = copy.Value;
            rule.Action = copy.Action;
            RulesList.Items.Refresh();
        }
    }

    private void DeleteRule_Click(object sender, RoutedEventArgs e)
    {
        if (RulesList.SelectedItem is not RoutingRule rule) return;
        _settings.CustomRules.Remove(rule);
        RulesList.Items.Refresh();
    }

    private void ImportRules_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "规则文件 (*.json;*.txt)|*.json;*.txt|所有文件 (*.*)|*.*",
            Title = "导入路由规则"
        };
        if (dlg.ShowDialog() != true) return;

        try
        {
            var imported = RuleImporter.Import(dlg.FileName);
            foreach (var r in imported) _settings.CustomRules.Add(r);
            RulesList.Items.Refresh();
        }
        catch (System.Exception ex)
        {
            MessageBox.Show("导入失败: " + ex.Message, "错误");
        }
    }
}
