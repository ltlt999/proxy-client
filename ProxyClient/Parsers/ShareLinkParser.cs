using System.Text;
using System.Text.Json;
using ProxyClient.Models;

namespace ProxyClient.Parsers;

public static class ShareLinkParser
{
    public static ServerItem? Parse(string link)
    {
        if (string.IsNullOrWhiteSpace(link)) return null;
        link = link.Trim();
        try
        {
            if (link.StartsWith("vmess://")) return ParseVmess(link);
            if (link.StartsWith("vless://")) return ParseVless(link);
            if (link.StartsWith("trojan://")) return ParseTrojan(link);
            if (link.StartsWith("ss://")) return ParseShadowsocks(link);
        }
        catch { return null; }
        return null;
    }

    public static List<ServerItem> ParseMany(string text)
    {
        var list = new List<ServerItem>();
        foreach (var raw in text.Split('\n', '\r'))
        {
            var line = raw.Trim();
            if (string.IsNullOrEmpty(line)) continue;
            var s = Parse(line);
            if (s != null && !string.IsNullOrWhiteSpace(s.Address) && s.Port > 0)
                list.Add(s);
        }
        return list;
    }

    static string Base64Decode(string s)
    {
        s = s.Trim().Replace("-", "+").Replace("_", "/");
        switch (s.Length % 4)
        {
            case 2: s += "=="; break;
            case 3: s += "="; break;
        }
        return Encoding.UTF8.GetString(Convert.FromBase64String(s));
    }

    static Dictionary<string, string> ParseQuery(string query)
    {
        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (query.StartsWith("?")) query = query[1..];
        if (string.IsNullOrEmpty(query)) return dict;
        foreach (var pair in query.Split('&'))
        {
            var eq = pair.IndexOf('=');
            if (eq < 0) { dict[Uri.UnescapeDataString(pair)] = ""; continue; }
            dict[Uri.UnescapeDataString(pair[..eq])] = Uri.UnescapeDataString(pair[(eq + 1)..]);
        }
        return dict;
    }

    static string Str(JsonElement el) => el.ValueKind == JsonValueKind.Null ? "" : (el.GetString() ?? "");

    static ServerItem ParseVmess(string link)
    {
        var json = Base64Decode(link["vmess://".Length..]);
        using var doc = JsonDocument.Parse(json);
        var r = doc.RootElement;
        var s = new ServerItem { Protocol = Protocols.VMess };
        s.Remark = r.TryGetProperty("ps", out var ps) ? Str(ps) : "";
        s.Address = r.TryGetProperty("add", out var add) ? Str(add) : "";
        s.Port = r.TryGetProperty("port", out var port) && int.TryParse(port.GetRawText().Trim('"'), out var p) ? p : 0;
        s.UserId = r.TryGetProperty("id", out var id) ? Str(id) : "";
        s.AlterId = r.TryGetProperty("aid", out var aid) ? aid.GetRawText().Trim('"') : "0";
        s.Network = r.TryGetProperty("net", out var net) ? (Str(net).Length == 0 ? "tcp" : Str(net)) : "tcp";
        var tls = r.TryGetProperty("tls", out var t) ? Str(t) : "";
        s.StreamSecurity = string.IsNullOrEmpty(tls) ? "none" : tls;
        s.Path = r.TryGetProperty("path", out var path) ? Str(path) : "";
        s.Host = r.TryGetProperty("host", out var host) ? Str(host) : "";
        s.Sni = r.TryGetProperty("sni", out var sni) ? Str(sni) : "";
        s.Security = r.TryGetProperty("scy", out var scy) ? (Str(scy).Length == 0 ? "auto" : Str(scy)) : "auto";
        if (string.IsNullOrEmpty(s.Sni)) s.Sni = s.Host;
        if (string.IsNullOrEmpty(s.AlterId)) s.AlterId = "0";
        return s;
    }

    static ServerItem ParseVless(string link)
    {
        var uri = new Uri(link);
        var q = ParseQuery(uri.Query);
        var s = new ServerItem { Protocol = Protocols.VLESS };
        s.UserId = Uri.UnescapeDataString(uri.UserInfo);
        s.Address = uri.Host;
        s.Port = uri.Port;
        s.Remark = Uri.UnescapeDataString(uri.Fragment.TrimStart('#'));
        s.Network = q.GetValueOrDefault("type", "tcp");
        s.StreamSecurity = q.GetValueOrDefault("security", "none");
        s.Path = q.GetValueOrDefault("path", "");
        s.Host = q.GetValueOrDefault("host", "");
        s.Sni = q.GetValueOrDefault("sni", "");
        s.Flow = q.GetValueOrDefault("flow", "");
        s.Fingerprint = q.GetValueOrDefault("fp", "");
        s.PublicKey = q.GetValueOrDefault("pbk", "");
        s.ShortId = q.GetValueOrDefault("sid", "");
        s.SpiderX = q.GetValueOrDefault("spx", "");
        if (string.IsNullOrEmpty(s.Sni)) s.Sni = s.Host;
        if (s.StreamSecurity == "reality" && string.IsNullOrEmpty(s.Fingerprint)) s.Fingerprint = "chrome";
        return s;
    }

    static ServerItem ParseTrojan(string link)
    {
        var uri = new Uri(link);
        var q = ParseQuery(uri.Query);
        var s = new ServerItem { Protocol = Protocols.Trojan };
        s.UserId = Uri.UnescapeDataString(uri.UserInfo);
        s.Address = uri.Host;
        s.Port = uri.Port;
        s.Remark = Uri.UnescapeDataString(uri.Fragment.TrimStart('#'));
        s.Network = q.GetValueOrDefault("type", "tcp");
        var sec = q.GetValueOrDefault("security", "tls");
        s.StreamSecurity = string.IsNullOrEmpty(sec) || sec == "none" ? "tls" : sec;
        s.Path = q.GetValueOrDefault("path", "");
        s.Host = q.GetValueOrDefault("host", "");
        s.Sni = q.GetValueOrDefault("sni", "");
        s.Flow = q.GetValueOrDefault("flow", "");
        s.Fingerprint = q.GetValueOrDefault("fp", "");
        s.PublicKey = q.GetValueOrDefault("pbk", "");
        s.ShortId = q.GetValueOrDefault("sid", "");
        if (string.IsNullOrEmpty(s.Sni)) s.Sni = s.Host;
        return s;
    }

    static ServerItem ParseShadowsocks(string link)
    {
        var body = link["ss://".Length..];
        var tag = "";
        var hash = body.IndexOf('#');
        if (hash >= 0) { tag = Uri.UnescapeDataString(body[(hash + 1)..]); body = body[..hash]; }
        var q = body.IndexOf('?');
        if (q >= 0) body = body[..q];

        string method = "", password = "", host = "";
        int port = 0;

        if (body.Contains('@'))
        {
            var at = body.LastIndexOf('@');
            var userinfo = body[..at];
            var hostport = body[(at + 1)..];
            if (!userinfo.Contains(':')) userinfo = Base64Decode(userinfo);
            var cp = userinfo.Split(':', 2);
            method = cp[0]; password = cp.Length > 1 ? cp[1] : "";
            var hp = hostport.Split(':', 2);
            host = hp[0]; port = hp.Length > 1 && int.TryParse(hp[1], out var pp) ? pp : 0;
        }
        else
        {
            var decoded = Base64Decode(body);
            var at = decoded.LastIndexOf('@');
            if (at < 0) return new ServerItem();
            var cp = decoded[..at].Split(':', 2);
            method = cp[0]; password = cp.Length > 1 ? cp[1] : "";
            var hp = decoded[(at + 1)..].Split(':', 2);
            host = hp[0]; port = hp.Length > 1 && int.TryParse(hp[1], out var pp) ? pp : 0;
        }

        return new ServerItem
        {
            Protocol = Protocols.Shadowsocks,
            Remark = tag,
            Address = host,
            Port = port,
            Security = method,
            UserId = password,
            Network = "tcp",
            StreamSecurity = "none"
        };
    }
}
