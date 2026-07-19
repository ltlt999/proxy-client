using System.Windows;
using ProxyClient.Models;
using Wpf.Ui.Controls;

namespace ProxyClient;

public partial class ServerEditWindow : FluentWindow
{
    public ServerEditWindow(ServerItem server)
    {
        InitializeComponent();
        ProtocolBox.ItemsSource = new[] { "vmess", "vless", "trojan", "shadowsocks" };
        NetworkBox.ItemsSource = new[] { "tcp", "ws", "grpc", "h2", "quic", "kcp" };
        StreamSecurityBox.ItemsSource = new[] { "none", "tls", "reality" };
        SecurityBox.ItemsSource = new[] { "auto", "aes-128-gcm", "chacha20-poly1305", "none", "zero",
            "aes-256-gcm", "chacha20-ietf-poly1305", "2022-aes-128-gcm", "2022-aes-256-gcm",
            "2022-chacha20-poly1305", "rc4-md5", "aes-128-cfb", "aes-256-cfb" };
        FingerprintBox.ItemsSource = new[] { "chrome", "firefox", "safari", "random", "none" };
        DataContext = server;
    }

    private void Ok_Click(object sender, RoutedEventArgs e) => DialogResult = true;
}
