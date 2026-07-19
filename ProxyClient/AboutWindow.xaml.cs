using System.Windows;
using System.Reflection;
using Wpf.Ui.Controls;

namespace ProxyClient;

public partial class AboutWindow : FluentWindow
{
    public AboutWindow()
    {
        InitializeComponent();
        var version = Assembly.GetExecutingAssembly().GetName().Version;
        VersionText.Text = $"v{version?.Major}.{version?.Minor}.{version?.Build}";
    }

    private void Ok_Click(object sender, RoutedEventArgs e) => Close();
}
