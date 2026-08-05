using System.Windows;
using System.Windows.Controls;
using ProxyClient.Models;
using Wpf.Ui.Controls;

namespace ProxyClient;

public partial class ServerEditWindow : FluentWindow
{
    public ServerEditWindow(ServerItem server)
    {
        InitializeComponent();
        ProtocolBox.ItemsSource = new[] { "vmess", "vless", "trojan", "shadowsocks", "hysteria2" };
        NetworkBox.ItemsSource = new[] { "tcp", "ws", "grpc", "h2", "quic", "kcp" };
        StreamSecurityBox.ItemsSource = new[] { "none", "tls", "reality" };
        SecurityBox.ItemsSource = new[] { "auto", "aes-128-gcm", "chacha20-poly1305", "none", "zero",
            "aes-256-gcm", "chacha20-ietf-poly1305", "2022-aes-128-gcm", "2022-aes-256-gcm",
            "2022-chacha20-poly1305", "rc4-md5", "aes-128-cfb", "aes-256-cfb" };
        FingerprintBox.ItemsSource = new[] { "chrome", "firefox", "safari", "random", "none" };
        Hy2ObfsBox.ItemsSource = new[] { "", "salamander" };
        DataContext = server;

        Loaded += (_, _) => SyncFieldVisibility();
        ProtocolBox.SelectionChanged += (_, _) => SyncFieldVisibility();
    }

    private void SyncFieldVisibility()
    {
        var protocol = ((ServerItem)DataContext).Protocol;
        var isHy2 = protocol == Protocols.Hysteria2;
        var isVmess = protocol == Protocols.VMess;

        if (Hy2Fields != null) Hy2Fields.Visibility = isHy2 ? Visibility.Visible : Visibility.Collapsed;

        SetVisibility(AlterIdRow, !isHy2 && isVmess);
        SetVisibility(SecurityRow, !isHy2);
        SetVisibility(NetworkRow, !isHy2);
        SetVisibility(PathRow, !isHy2);
        SetVisibility(HostRow, !isHy2);
        SetVisibility(FlowRow, !isHy2);
        SetVisibility(FingerprintRow, !isHy2);
        SetVisibility(PublicKeyRow, !isHy2);
        SetVisibility(ShortIdRow, !isHy2);
    }

    private void SetVisibility(UIElement? element, bool visible)
    {
        if (element != null) element.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
    }

    private void Ok_Click(object sender, RoutedEventArgs e) => DialogResult = true;
}
