namespace ProxyClient.Models;

public class RoutingRule
{
    public string Type { get; set; } = "domain"; // domain / ip / port
    public string Value { get; set; } = "";
    public string Action { get; set; } = "direct"; // direct / proxy / block
}
