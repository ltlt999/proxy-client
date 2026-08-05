using System.Windows;
using ProxyClient.Models;
using Wpf.Ui.Controls;

namespace ProxyClient;

public partial class RoutingRuleEditWindow : FluentWindow
{
    public RoutingRule Rule { get; }

    public RoutingRuleEditWindow(RoutingRule rule)
    {
        InitializeComponent();
        Rule = rule;
        TypeBox.ItemsSource = new[] { "domain", "ip", "port" };
        ActionBox.ItemsSource = new[] { "direct", "proxy", "block" };
        TypeBox.SelectedItem = rule.Type;
        ValueBox.Text = rule.Value;
        ActionBox.SelectedItem = rule.Action;
    }

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        Rule.Type = TypeBox.SelectedItem?.ToString() ?? "domain";
        Rule.Value = ValueBox.Text;
        Rule.Action = ActionBox.SelectedItem?.ToString() ?? "direct";
        DialogResult = true;
    }
}
