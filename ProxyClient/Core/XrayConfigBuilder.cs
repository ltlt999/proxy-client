using System.Text.Json;
using System.Text.Json.Nodes;
using ProxyClient.Models;

namespace ProxyClient.Core;

public enum RoutingMode { Rule = 0, Global = 1 }

public static class XrayConfigBuilder
{
    public const int SocksPort = 10808;
    public const int HttpPort = 10809;

    public static string Build(ServerItem server, RoutingMode mode)
    {
        var root = new JsonObject
        {
            ["log"] = new JsonObject { ["loglevel"] = "warning" },
            ["inbounds"] = new JsonArray(
                Inbound("socks-in", SocksPort, "socks", new JsonObject { ["auth"] = "noauth", ["udp"] = true }),
                Inbound("http-in", HttpPort, "http", new JsonObject())
            ),
            ["outbounds"] = new JsonArray(
                BuildProxyOutbound(server),
                new JsonObject { ["tag"] = "direct", ["protocol"] = "freedom" },
                new JsonObject
                {
                    ["tag"] = "block",
                    ["protocol"] = "blackhole",
                    ["settings"] = new JsonObject { ["response"] = new JsonObject { ["type"] = "http" } }
                }
            )
        };

        if (mode == RoutingMode.Rule)
        {
            root["routing"] = new JsonObject
            {
                ["domainStrategy"] = "IPIfNonMatch",
                ["rules"] = new JsonArray(
                    new JsonObject { ["type"] = "field", ["outboundTag"] = "direct", ["domain"] = new JsonArray("geosite:category-ads-all") },
                    new JsonObject { ["type"] = "field", ["outboundTag"] = "direct", ["domain"] = new JsonArray("geosite:cn", "geosite:private") },
                    new JsonObject { ["type"] = "field", ["outboundTag"] = "direct", ["ip"] = new JsonArray("geoip:private", "geoip:cn") }
                )
            };
        }
        else
        {
            root["routing"] = new JsonObject { ["domainStrategy"] = "AsIs" };
        }

        return root.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
    }

    static JsonObject Inbound(string tag, int port, string protocol, JsonObject settings)
        => new() { ["tag"] = tag, ["port"] = port, ["listen"] = "127.0.0.1", ["protocol"] = protocol, ["settings"] = settings };

    static JsonObject BuildProxyOutbound(ServerItem s)
    {
        var ob = new JsonObject { ["tag"] = "proxy" };

        switch (s.Protocol)
        {
            case Protocols.VMess:
                ob["protocol"] = "vmess";
                ob["settings"] = new JsonObject
                {
                    ["vnext"] = new JsonArray(new JsonObject
                    {
                        ["address"] = s.Address,
                        ["port"] = s.Port,
                        ["users"] = new JsonArray(new JsonObject
                        {
                            ["id"] = s.UserId,
                            ["alterId"] = int.TryParse(s.AlterId, out var aid) ? aid : 0,
                            ["security"] = string.IsNullOrEmpty(s.Security) ? "auto" : s.Security
                        })
                    })
                };
                break;

            case Protocols.VLESS:
                ob["protocol"] = "vless";
                var user = new JsonObject { ["id"] = s.UserId, ["encryption"] = "none" };
                if (!string.IsNullOrEmpty(s.Flow)) user["flow"] = s.Flow;
                ob["settings"] = new JsonObject
                {
                    ["vnext"] = new JsonArray(new JsonObject
                    {
                        ["address"] = s.Address,
                        ["port"] = s.Port,
                        ["users"] = new JsonArray(user)
                    })
                };
                break;

            case Protocols.Trojan:
                ob["protocol"] = "trojan";
                ob["settings"] = new JsonObject
                {
                    ["servers"] = new JsonArray(new JsonObject
                    {
                        ["address"] = s.Address,
                        ["port"] = s.Port,
                        ["password"] = s.UserId
                    })
                };
                break;

            case Protocols.Shadowsocks:
                ob["protocol"] = "shadowsocks";
                ob["settings"] = new JsonObject
                {
                    ["servers"] = new JsonArray(new JsonObject
                    {
                        ["address"] = s.Address,
                        ["port"] = s.Port,
                        ["method"] = s.Security,
                        ["password"] = s.UserId
                    })
                };
                break;

            default:
                ob["protocol"] = "freedom";
                break;
        }

        ob["streamSettings"] = BuildStreamSettings(s);
        return ob;
    }

    static JsonObject BuildStreamSettings(ServerItem s)
    {
        var ss = new JsonObject { ["network"] = string.IsNullOrEmpty(s.Network) ? "tcp" : s.Network };

        if (s.StreamSecurity == "tls" || (s.Protocol == Protocols.Trojan && s.StreamSecurity != "reality"))
        {
            ss["security"] = "tls";
            var tls = new JsonObject
            {
                ["serverName"] = string.IsNullOrEmpty(s.Sni) ? s.Address : s.Sni,
                ["allowInsecure"] = s.AllowInsecure
            };
            if (!string.IsNullOrEmpty(s.Fingerprint)) tls["fingerprint"] = s.Fingerprint;
            ss["tlsSettings"] = tls;
        }
        else if (s.StreamSecurity == "reality")
        {
            ss["security"] = "reality";
            var rs = new JsonObject
            {
                ["serverName"] = string.IsNullOrEmpty(s.Sni) ? s.Address : s.Sni,
                ["publicKey"] = s.PublicKey,
                ["shortId"] = s.ShortId,
                ["fingerprint"] = string.IsNullOrEmpty(s.Fingerprint) ? "chrome" : s.Fingerprint
            };
            if (!string.IsNullOrEmpty(s.SpiderX)) rs["spiderX"] = s.SpiderX;
            ss["realitySettings"] = rs;
        }
        else
        {
            ss["security"] = "none";
        }

        switch (s.Network)
        {
            case "ws":
                var ws = new JsonObject { ["path"] = string.IsNullOrEmpty(s.Path) ? "/" : s.Path };
                if (!string.IsNullOrEmpty(s.Host)) ws["headers"] = new JsonObject { ["Host"] = s.Host };
                ss["wsSettings"] = ws;
                break;

            case "grpc":
                ss["grpcSettings"] = new JsonObject { ["serviceName"] = s.Path };
                break;

            case "h2":
                var h2 = new JsonObject { ["path"] = s.Path };
                if (!string.IsNullOrEmpty(s.Host)) h2["host"] = new JsonArray(s.Host);
                ss["httpSettings"] = h2;
                break;
        }

        return ss;
    }
}
