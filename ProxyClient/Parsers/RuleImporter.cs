using System.IO;
using System.Text.Json;
using ProxyClient.Models;

namespace ProxyClient.Parsers;

public static class RuleImporter
{
    public static List<RoutingRule> Import(string path)
    {
        var ext = Path.GetExtension(path).ToLowerInvariant();
        var content = File.ReadAllText(path);

        if (ext == ".json")
            return ImportJson(content);

        return ImportDomainList(content);
    }

    static List<RoutingRule> ImportJson(string content)
    {
        var rules = new List<RoutingRule>();
        using var doc = JsonDocument.Parse(content);
        foreach (var el in doc.RootElement.EnumerateArray())
        {
            var rule = new RoutingRule { Action = el.GetProperty("outboundTag").GetString() ?? "direct" };
            if (el.TryGetProperty("domain", out var domain))
            {
                rule.Type = "domain";
                rule.Value = string.Join(Environment.NewLine, domain.EnumerateArray().Select(x => x.GetString() ?? ""));
            }
            else if (el.TryGetProperty("ip", out var ip))
            {
                rule.Type = "ip";
                rule.Value = string.Join(Environment.NewLine, ip.EnumerateArray().Select(x => x.GetString() ?? ""));
            }
            else if (el.TryGetProperty("port", out var port))
            {
                rule.Type = "port";
                rule.Value = port.GetString() ?? "";
            }
            rules.Add(rule);
        }
        return rules;
    }

    static List<RoutingRule> ImportDomainList(string content)
    {
        var domains = content.Split('\n', '\r')
            .Select(x => x.Trim())
            .Where(x => !string.IsNullOrEmpty(x) && !x.StartsWith("#") && !x.StartsWith("//"))
            .ToList();

        if (domains.Count == 0) return new List<RoutingRule>();

        return new List<RoutingRule>
        {
            new RoutingRule { Type = "domain", Value = string.Join(Environment.NewLine, domains), Action = "direct" }
        };
    }
}
