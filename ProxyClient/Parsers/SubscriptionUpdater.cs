using System.Net.Http;
using System.Text;
using ProxyClient.Models;

namespace ProxyClient.Parsers;

public class SubscriptionUpdater
{
    public async Task<List<ServerItem>> FetchAsync(string url, CancellationToken ct = default)
    {
        using var handler = new HttpClientHandler { AllowAutoRedirect = true };
        using var http = new HttpClient(handler);
        http.DefaultRequestHeaders.UserAgent.ParseAdd("ProxyClient/1.0");
        http.Timeout = TimeSpan.FromSeconds(30);

        var content = await http.GetStringAsync(url, ct);
        var text = Decode(content);
        return ShareLinkParser.ParseMany(text);
    }

    static string Decode(string content)
    {
        var s = content?.Trim() ?? "";
        if (s.Contains("://")) return s;
        try
        {
            var b64 = s.Replace("-", "+").Replace("_", "/").Replace("\n", "").Replace("\r", "").Replace(" ", "");
            switch (b64.Length % 4)
            {
                case 2: b64 += "=="; break;
                case 3: b64 += "="; break;
            }
            return Encoding.UTF8.GetString(Convert.FromBase64String(b64));
        }
        catch { return s; }
    }
}
