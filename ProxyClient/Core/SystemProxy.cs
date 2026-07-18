using Microsoft.Win32;

namespace ProxyClient.Core;

public static class SystemProxy
{
    private const string KeyPath = @"Software\Microsoft\Windows\CurrentVersion\Internet Settings";

    public static void Enable(int port)
    {
        using var key = Registry.CurrentUser.OpenSubKey(KeyPath, true);
        if (key == null) return;
        key.SetValue("ProxyServer", $"127.0.0.1:{port}", RegistryValueKind.String);
        key.SetValue("ProxyEnable", 1, RegistryValueKind.DWord);
        key.DeleteValue("AutoConfigURL", false);
        NativeMethods.RefreshProxySettings();
    }

    public static void Disable()
    {
        using var key = Registry.CurrentUser.OpenSubKey(KeyPath, true);
        if (key == null) return;
        key.SetValue("ProxyEnable", 0, RegistryValueKind.DWord);
        NativeMethods.RefreshProxySettings();
    }

    public static bool IsEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(KeyPath, false);
        if (key == null) return false;
        return Convert.ToInt32(key.GetValue("ProxyEnable", 0)) == 1;
    }

    public static string GetProxyServer()
    {
        using var key = Registry.CurrentUser.OpenSubKey(KeyPath, false);
        return key?.GetValue("ProxyServer") as string ?? "";
    }
}
