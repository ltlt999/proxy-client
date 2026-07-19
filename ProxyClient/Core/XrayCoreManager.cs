using System.Diagnostics;
using System.IO;
using System.Net.NetworkInformation;

namespace ProxyClient.Core;

public class XrayCoreManager
{
    private Process? _process;
    public bool IsRunning => _process != null && !_process.HasExited;

    public string CorePath { get; set; } = "";
    public event EventHandler<string>? LogReceived;

    public string ConfigDir => Path.Combine(AppContext.BaseDirectory, "config");
    public string ConfigPath => Path.Combine(ConfigDir, "config.json");

    public static void CleanupPorts()
    {
        var ports = new[] { XrayConfigBuilder.SocksPort, XrayConfigBuilder.HttpPort };
        foreach (var p in ports)
        {
            try
            {
                var procs = GetProcessesUsingPort(p);
                foreach (var pid in procs)
                {
                    try
                    {
                        var proc = Process.GetProcessById(pid);
                        if (proc.ProcessName.Equals("xray", StringComparison.OrdinalIgnoreCase))
                        {
                            proc.Kill();
                            proc.WaitForExit(2000);
                        }
                    }
                    catch { }
                }
            }
            catch { }
        }
    }

    static List<int> GetProcessesUsingPort(int port)
    {
        var result = new List<int>();
        try
        {
            var props = IPGlobalProperties.GetIPGlobalProperties();
            var listeners = props.GetActiveTcpListeners();
            foreach (var ep in listeners)
            {
                if (ep.Port == port)
                {
                    result.AddRange(FindPidsForEndpoint(ep));
                }
            }
        }
        catch { }
        return result;
    }

    static List<int> FindPidsForEndpoint(System.Net.IPEndPoint endpoint)
    {
        var pids = new List<int>();
        try
        {
            var output = ExecuteNetstat();
            foreach (var line in output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 5) continue;
                var local = parts[1];
                var state = parts[3];
                var pidStr = parts[4];
                if (local.Contains($":{endpoint.Port}") && state == "LISTENING" && int.TryParse(pidStr, out var pid))
                {
                    pids.Add(pid);
                }
            }
        }
        catch { }
        return pids;
    }

    static string ExecuteNetstat()
    {
        var psi = new ProcessStartInfo
        {
            FileName = "netstat.exe",
            Arguments = "-ano",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            CreateNoWindow = true
        };
        using var proc = Process.Start(psi);
        if (proc == null) return "";
        var output = proc.StandardOutput.ReadToEnd();
        proc.WaitForExit(3000);
        return output;
    }

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
        CleanupPorts();
        Thread.Sleep(300);

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
