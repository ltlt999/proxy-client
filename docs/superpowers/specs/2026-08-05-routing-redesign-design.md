# 路由规则重构设计文档

## 背景

ProxyClient 当前仅支持两种路由模式：

1. 规则模式（绕过大陆）
2. 全局模式

用户反馈：

- 现有「绕过大陆」规则不够完整，部分中国流量仍走代理
- 希望支持自定义规则（域名、IP、端口）
- 希望支持导入规则文件

## 目标

实现更灵活、更完整的路由系统：

1. 优化「绕过大陆」规则，提升分流准确性
2. 新增「自定义规则」模式，允许用户手动添加/编辑/删除规则
3. 支持导入规则文件（Xray JSON / 域名列表）
4. 主界面路由下拉支持三种模式切换

> 进程分流不在本次范围内，因为 Xray-core 本身不支持按进程名路由。

## 路由模式

### 模式 0：规则模式（绕过大陆）

使用内置规则，将中国及局域网流量导向 direct，其余默认走 proxy。

优化后的规则：

```json
{
  "domainStrategy": "IPIfNonMatch",
  "rules": [
    { "type": "field", "outboundTag": "block", "domain": ["geosite:category-ads-all"] },
    { "type": "field", "outboundTag": "direct", "domain": ["geosite:cn", "geosite:private", "geosite:tld-cn"] },
    { "type": "field", "outboundTag": "direct", "ip": ["geoip:private", "geoip:cn"] }
  ]
}
```

### 模式 1：全局模式

所有流量走 proxy：

```json
{
  "domainStrategy": "AsIs"
}
```

### 模式 2：自定义规则

按用户在设置中添加的规则生成 `routing.rules`，规则类型包括：

- `domain`：域名规则，支持 `domain:`、`regexp:`、`full:`、`geosite:` 前缀
- `ip`：IP 规则，支持 `geoip:` 前缀和 CIDR
- `port`：端口规则，支持单个端口、逗号分隔、范围

每条规则动作：

- `direct`：直连
- `proxy`：代理
- `block`：拦截

## 数据模型

### RoutingRule

```csharp
public class RoutingRule
{
    public string Type { get; set; } = "domain"; // domain / ip / port
    public string Value { get; set; } = "";      // 支持多行/逗号分隔
    public string Action { get; set; } = "direct"; // direct / proxy / block
}
```

### AppSettings 扩展

```csharp
public List<RoutingRule> CustomRules { get; set; } = new();
public string? ImportedRuleFile { get; set; }
```

### RoutingMode 枚举

```csharp
public enum RoutingMode { Rule = 0, Global = 1, Custom = 2 }
```

## Xray 配置生成

### 规则模式（绕过大陆）

在 `XrayConfigBuilder.Build` 中：

```csharp
if (mode == RoutingMode.Rule)
{
    root["routing"] = new JsonObject
    {
        ["domainStrategy"] = "IPIfNonMatch",
        ["rules"] = new JsonArray(
            new JsonObject { ["type"] = "field", ["outboundTag"] = "block", ["domain"] = new JsonArray("geosite:category-ads-all") },
            new JsonObject { ["type"] = "field", ["outboundTag"] = "direct", ["domain"] = new JsonArray("geosite:cn", "geosite:private", "geosite:tld-cn") },
            new JsonObject { ["type"] = "field", ["outboundTag"] = "direct", ["ip"] = new JsonArray("geoip:private", "geoip:cn") }
        )
    };
}
```

### 全局模式

保持不变。

### 自定义规则

```csharp
else if (mode == RoutingMode.Custom)
{
    var rules = new JsonArray();
    foreach (var r in customRules)
    {
        var rule = new JsonObject { ["type"] = "field", ["outboundTag"] = r.Action };
        if (r.Type == "domain") rule["domain"] = ParseRuleValues(r.Value);
        if (r.Type == "ip") rule["ip"] = ParseRuleValues(r.Value);
        if (r.Type == "port") rule["port"] = r.Value.Replace(" ", "").Replace("\n", ",").Trim(',');
        rules.Add(rule);
    }
    root["routing"] = new JsonObject
    {
        ["domainStrategy"] = "IPIfNonMatch",
        ["rules"] = rules
    };
}
```

`ParseRuleValues` 将用户输入的多行/逗号分隔值转换为 `JsonArray`。

## UI 设计

### 主界面

路由下拉框选项改为：

```csharp
new() { "规则模式 (绕过大陆)", "全局模式", "自定义规则" }
```

### 设置窗口

新增「路由规则」设置卡片，包含：

- 当前规则列表（ListBox / DataGrid）
- 添加、编辑、删除按钮
- 导入规则文件按钮
- 规则编辑器弹窗或内嵌面板

### 规则编辑器

- 类型：ComboBox（domain / ip / port）
- 值：多行 TextBox（支持批量输入）
- 动作：ComboBox（direct / proxy / block）

## 规则文件导入

### 支持格式 1：Xray JSON

文件内容示例：

```json
[
  { "type": "field", "outboundTag": "direct", "domain": ["geosite:cn"] },
  { "type": "field", "outboundTag": "proxy", "domain": ["geosite:geolocation-!cn"] }
]
```

导入后解析为 `List<RoutingRule>`，按条展示在自定义规则列表中。

### 支持格式 2：域名列表文本

每行一个域名，例如：

```
google.com
youtube.com
github.com
```

导入时弹窗让用户选择动作（direct / proxy / block），默认 `direct`。

## 测试计划

1. 切换三种路由模式，验证生成的 `config.json` 正确
2. 在自定义规则中添加 domain / ip / port 规则，验证 JSON 输出
3. 导入 Xray JSON 规则文件，验证列表正确显示
4. 导入域名列表文本，验证生成规则
5. 切换活动时正在运行核心，验证核心重载
6. 持久化测试：关闭软件后重新打开，规则保留

## 兼容性

- `RoutingMode` 枚举新增 `Custom = 2`，旧设置中的 `0` 和 `1` 保持原有行为
- 新增 `CustomRules` 字段默认空列表，旧配置反序列化后为空
- 导入的规则文件路径保存到 `ImportedRuleFile`，文件本身不复制到项目目录

## 备注

- Xray-core 不支持按进程名路由，因此进程分流不在本次实现范围
- 自定义规则按用户添加顺序生效，顺序会影响分流结果
- 建议用户将更具体的规则放在前面
