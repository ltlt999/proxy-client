using Microsoft.Win32;

namespace ProxyClient.Core;

public static class AutoStartHelper
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "ProxyClient";
    private const string MinimizedArg = "--minimized";

    public static bool IsEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, false);
        return key?.GetValue(ValueName) != null;
    }

    public static void SetEnabled(bool enabled, bool minimized)
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, true);
        if (key == null) return;
        if (enabled)
        {
            var exe = Environment.ProcessPath;
            if (string.IsNullOrEmpty(exe)) return;
            var args = minimized ? $" {MinimizedArg}" : "";
            key.SetValue(ValueName, $"\"{exe}\"{args}", RegistryValueKind.String);
        }
        else
        {
            if (key.GetValue(ValueName) != null) key.DeleteValue(ValueName, false);
        }
    }

    public static bool ShouldStartMinimized()
        => Environment.GetCommandLineArgs().Any(a =>
            string.Equals(a, MinimizedArg, StringComparison.OrdinalIgnoreCase));
}
