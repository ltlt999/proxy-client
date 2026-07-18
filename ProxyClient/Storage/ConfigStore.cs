using System.IO;
using System.Text.Json;
using ProxyClient.Models;

namespace ProxyClient.Storage;

public class AppSettings
{
    public string ActiveServerId { get; set; } = "";
    public bool AutoStartCore { get; set; }
    public int RoutingMode { get; set; } = (int)ProxyClient.Core.RoutingMode.Rule;
    public List<string> Subscriptions { get; set; } = new();
    public bool AutoStartWithWindows { get; set; }
    public bool MinimizeOnStart { get; set; }
    public bool MinimizeOnClose { get; set; } = true;
}

public class AppData
{
    public List<ServerItem> Servers { get; set; } = new();
    public AppSettings Settings { get; set; } = new();
}

public static class ConfigStore
{
    private static readonly string Dir = Path.Combine(AppContext.BaseDirectory, "data");
    private static readonly string FilePath = Path.Combine(Dir, "appdata.json");
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    public static AppData Load()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                var json = File.ReadAllText(FilePath);
                var data = JsonSerializer.Deserialize<AppData>(json);
                if (data != null) return data;
            }
        }
        catch { }
        return new AppData();
    }

    public static void Save(AppData data)
    {
        try
        {
            Directory.CreateDirectory(Dir);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(data, Options));
        }
        catch { }
    }
}
