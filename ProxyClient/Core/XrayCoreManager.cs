using System.Diagnostics;
using System.IO;

namespace ProxyClient.Core;

public class XrayCoreManager
{
    private Process? _process;
    public bool IsRunning => _process != null && !_process.HasExited;

    public string CorePath { get; set; } = "";
    public event EventHandler<string>? LogReceived;

    public string ConfigDir => Path.Combine(AppContext.BaseDirectory, "config");
    public string ConfigPath => Path.Combine(ConfigDir, "config.json");

    public string ResolveCorePath()
    {
        if (File.Exists(CorePath)) return CorePath;
        var baseDir = AppContext.BaseDirectory;
        var candidates = new[]
        {
            Path.Combine(baseDir, "xray", "xray.exe"),
            Path.Combine(baseDir, "xray.exe"),
            Path.Combine(Directory.GetParent(baseDir)?.FullName ?? baseDir, "xray", "xray.exe")
        };
        foreach (var c in candidates)
            if (File.Exists(c)) return c;
        return candidates[0];
    }

    public void WriteConfig(string json)
    {
        Directory.CreateDirectory(ConfigDir);
        File.WriteAllText(ConfigPath, json);
    }

    public bool Start()
    {
        var exe = ResolveCorePath();
        if (!File.Exists(exe))
        {
            LogReceived?.Invoke(this, $"未找到 Xray 核心: {exe}");
            return false;
        }
        Stop();

        LogReceived?.Invoke(this, $"正在启动 Xray: {exe}");
        LogReceived?.Invoke(this, $"工作目录: {Path.GetDirectoryName(exe) ?? AppContext.BaseDirectory}");

        var psi = new ProcessStartInfo
        {
            FileName = exe,
            Arguments = $"run -c \"{ConfigPath}\"",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            WorkingDirectory = Path.GetDirectoryName(exe) ?? AppContext.BaseDirectory
        };

        _process = new Process { StartInfo = psi, EnableRaisingEvents = true };
        _process.OutputDataReceived += (_, e) => { if (e.Data != null) LogReceived?.Invoke(this, e.Data); };
        _process.ErrorDataReceived += (_, e) => { if (e.Data != null) LogReceived?.Invoke(this, e.Data); };
        _process.Exited += (_, _) =>
        {
            var code = _process?.ExitCode ?? -1;
            LogReceived?.Invoke(this, $"Xray 核心已退出 (退出码: {code})");
        };

        try
        {
            _process.Start();
            _process.BeginOutputReadLine();
            _process.BeginErrorReadLine();
            _process.WaitForExit(500);
            if (_process.HasExited)
            {
                LogReceived?.Invoke(this, $"Xray 核心启动后立即退出 (退出码: {_process.ExitCode})");
                return false;
            }
            return true;
        }
        catch (Exception ex)
        {
            LogReceived?.Invoke(this, "启动失败: " + ex.Message);
            _process = null;
            return false;
        }
    }

    public void Stop()
    {
        if (_process == null) return;
        try
        {
            if (!_process.HasExited)
            {
                _process.Kill(entireProcessTree: true);
                _process.WaitForExit(3000);
            }
        }
        catch { }
        try { _process.Dispose(); } catch { }
        _process = null;
    }
}
