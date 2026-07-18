using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace ProxyClient.Models;

public static class Protocols
{
    public const string VMess = "vmess";
    public const string VLESS = "vless";
    public const string Trojan = "trojan";
    public const string Shadowsocks = "shadowsocks";
}

public class ServerItem : INotifyPropertyChanged
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public string Remark { get; set; } = "";
    public string Protocol { get; set; } = Protocols.VMess;
    public string Address { get; set; } = "";
    public int Port { get; set; } = 443;

    public string UserId { get; set; } = "";
    public string AlterId { get; set; } = "0";
    public string Security { get; set; } = "auto";

    public string Network { get; set; } = "tcp";
    public string Path { get; set; } = "";
    public string Host { get; set; } = "";
    public string Sni { get; set; } = "";

    public string StreamSecurity { get; set; } = "none";
    public string Flow { get; set; } = "";
    public string Fingerprint { get; set; } = "";
    public string PublicKey { get; set; } = "";
    public string ShortId { get; set; } = "";
    public string SpiderX { get; set; } = "";
    public bool AllowInsecure { get; set; } = false;

    [JsonIgnore]
    private string _testResult = "";
    [JsonIgnore]
    public string TestResult
    {
        get => _testResult;
        set { _testResult = value; OnPropertyChanged(); }
    }

    [JsonIgnore]
    private bool _isActive;
    [JsonIgnore]
    public bool IsActive
    {
        get => _isActive;
        set { _isActive = value; OnPropertyChanged(); }
    }

    public string Display => string.IsNullOrWhiteSpace(Remark)
        ? $"{Address}:{Port}"
        : Remark;

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    public ServerItem Clone()
    {
        var clone = (ServerItem)MemberwiseClone();
        clone.Id = Guid.NewGuid().ToString("N");
        clone._isActive = false;
        clone._testResult = "";
        return clone;
    }
}
