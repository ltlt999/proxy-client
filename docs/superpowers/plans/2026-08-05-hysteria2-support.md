# HY2 (Hysteria 2) 支持实现计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 在 ProxyClient 中完整支持 Hysteria 2 协议，包括解析分享链接、编辑节点、生成 Xray 配置。

**Architecture:** 在现有 `ServerItem` 模型上增加 HY2 字段，通过 `ShareLinkParser` 解析 `hysteria2://` 链接，`XrayConfigBuilder` 生成对应的 `hysteria2` outbound，`ServerEditWindow` 根据协议动态显示/隐藏字段。

**Tech Stack:** C# 12, .NET 9, WPF, WPF-UI, Xray-core

---

## Task 1: 更新数据模型

**Files:**
- Modify: `ProxyClient/Models/ServerItem.cs`

- [ ] **Step 1: 添加 HY2 协议常量**

在 `Protocols` 静态类中新增：

```csharp
public const string Hysteria2 = "hysteria2";
```

- [ ] **Step 2: 添加 HY2 字段到 ServerItem**

在 `ServerItem` 类末尾、测试相关字段之前添加：

```csharp
public string Hy2Password { get; set; } = "";
public string Hy2Obfs { get; set; } = "";
public string Hy2ObfsPassword { get; set; } = "";
public string Hy2UpMbps { get; set; } = "";
public string Hy2DownMbps { get; set; } = "";
public bool Hy2DisableUdp { get; set; } = false;
```

- [ ] **Step 3: 提交**

```bash
git add ProxyClient/Models/ServerItem.cs
git commit -m "feat: add HY2 protocol constant and fields"
```

---

## Task 2: 实现 HY2 分享链接解析

**Files:**
- Modify: `ProxyClient/Parsers/ShareLinkParser.cs`

- [ ] **Step 1: 在 Parse 方法入口增加 HY2 分支**

在 `Parse` 方法中，紧跟 `ss://` 判断之后添加：

```csharp
if (link.StartsWith("hysteria2://")) return ParseHysteria2(link);
if (link.StartsWith("hy2://")) return ParseHysteria2(link.Replace("hy2://", "hysteria2://"));
```

- [ ] **Step 2: 实现 ParseHysteria2 方法**

在 `ParseShadowsocks` 方法之后添加：

```csharp
static ServerItem ParseHysteria2(string link)
{
    var uri = new Uri(link);
    var q = ParseQuery(uri.Query);
    var s = new ServerItem { Protocol = Protocols.Hysteria2 };
    s.UserId = Uri.UnescapeDataString(uri.UserInfo);
    s.Address = uri.Host;
    s.Port = uri.Port;
    s.Remark = Uri.UnescapeDataString(uri.Fragment.TrimStart('#'));
    s.Hy2Password = s.UserId;
    s.Hy2Obfs = q.GetValueOrDefault("obfs", "");
    s.Hy2ObfsPassword = q.GetValueOrDefault("obfs-password", "");
    s.Hy2UpMbps = q.GetValueOrDefault("upmbps", "");
    s.Hy2DownMbps = q.GetValueOrDefault("downmbps", "");
    s.Sni = q.GetValueOrDefault("sni", "");
    s.AllowInsecure = q.GetValueOrDefault("insecure", "") == "1";
    s.Network = "udp";
    s.StreamSecurity = "tls";
    if (string.IsNullOrEmpty(s.Sni)) s.Sni = s.Address;
    return s;
}
```

- [ ] **Step 3: 手动测试解析**

运行以下命令启动临时测试：

```bash
cd ProxyClient
dotnet run
```

在软件中点击「从分享链接添加」，粘贴示例链接：

```
hysteria2://password@example.com:443?obfs=salamander&obfs-password=obfs-secret&sni=example.com&insecure=0&upmbps=100&downmbps=100#MyHY2
```

预期：成功添加一个协议为 `hysteria2` 的节点，备注为 `MyHY2`。

- [ ] **Step 4: 提交**

```bash
git add ProxyClient/Parsers/ShareLinkParser.cs
git commit -m "feat: parse hysteria2:// share links"
```

---

## Task 3: 生成 Xray HY2 outbound 配置

**Files:**
- Modify: `ProxyClient/Core/XrayConfigBuilder.cs`

- [ ] **Step 1: 在 BuildProxyOutbound 中增加 HY2 分支**

在 `switch` 语句的 `default` 之前添加：

```csharp
case Protocols.Hysteria2:
    ob["protocol"] = "hysteria2";
    ob["settings"] = BuildHysteria2Settings(s);
    break;
```

- [ ] **Step 2: 实现 BuildHysteria2Settings 方法**

在 `BuildProxyOutbound` 方法之后添加：

```csharp
static JsonObject BuildHysteria2Settings(ServerItem s)
{
    var settings = new JsonObject
    {
        ["servers"] = new JsonArray(new JsonObject
        {
            ["address"] = s.Address,
            ["port"] = s.Port
        }),
        ["auth"] = s.Hy2Password
    };

    if (!string.IsNullOrEmpty(s.Hy2Obfs))
    {
        settings["obfs"] = s.Hy2Obfs;
        if (!string.IsNullOrEmpty(s.Hy2ObfsPassword))
            settings["obfsPassword"] = s.Hy2ObfsPassword;
    }

    if (!string.IsNullOrEmpty(s.Hy2UpMbps) || !string.IsNullOrEmpty(s.Hy2DownMbps))
    {
        settings["bandwidth"] = new JsonObject
        {
            ["up"] = FormatBandwidth(s.Hy2UpMbps),
            ["down"] = FormatBandwidth(s.Hy2DownMbps)
        };
    }

    if (s.Hy2DisableUdp)
        settings["disableUdp"] = true;

    return settings;
}

static string FormatBandwidth(string value)
{
    if (string.IsNullOrWhiteSpace(value)) return "0 Mbps";
    var trimmed = value.Trim();
    if (trimmed.EndsWith("Mbps", StringComparison.OrdinalIgnoreCase)) return trimmed;
    if (trimmed.EndsWith("Mb", StringComparison.OrdinalIgnoreCase)) return trimmed + "ps";
    if (int.TryParse(trimmed, out _)) return trimmed + " Mbps";
    return trimmed;
}
```

- [ ] **Step 3: 在 streamSettings 中添加 HY2 TLS 处理**

在 `BuildProxyOutbound` 中，当前 `ob["streamSettings"] = BuildStreamSettings(s);` 的逻辑保持通用，但需要确保 HY2 的 TLS 能正确生成。

在 `BuildStreamSettings` 方法中，将现有的 TLS 条件：

```csharp
if (s.StreamSecurity == "tls" || (s.Protocol == Protocols.Trojan && s.StreamSecurity != "reality"))
```

改为：

```csharp
if (s.StreamSecurity == "tls" || (s.Protocol == Protocols.Trojan && s.StreamSecurity != "reality") || s.Protocol == Protocols.Hysteria2)
```

- [ ] **Step 4: 手动测试配置生成**

添加一个 HY2 节点后点击电源按钮启动，检查生成的 `ProxyClient/bin/Debug/net9.0-windows/config/config.json` 是否包含 `hysteria2` outbound。

预期 JSON 结构示例：

```json
{
  "tag": "proxy",
  "protocol": "hysteria2",
  "settings": {
    "servers": [{ "address": "example.com", "port": 443 }],
    "auth": "password",
    "obfs": "salamander",
    "obfsPassword": "obfs-secret",
    "bandwidth": { "up": "100 Mbps", "down": "100 Mbps" }
  },
  "streamSettings": {
    "security": "tls",
    "tlsSettings": {
      "serverName": "example.com",
      "allowInsecure": false
    }
  }
}
```

- [ ] **Step 5: 提交**

```bash
git add ProxyClient/Core/XrayConfigBuilder.cs
git commit -m "feat: generate xray hysteria2 outbound config"
```

---

## Task 4: 更新节点编辑界面

**Files:**
- Modify: `ProxyClient/ServerEditWindow.xaml`
- Modify: `ProxyClient/ServerEditWindow.xaml.cs`

- [ ] **Step 1: 修改协议下拉框初始化**

在 `ServerEditWindow.xaml.cs` 构造函数中，确保协议下拉框包含 HY2：

```csharp
var protocols = new[] { Protocols.VMess, Protocols.VLESS, Protocols.Trojan, Protocols.Shadowsocks, Protocols.Hysteria2 };
ProtocolBox.ItemsSource = protocols;
```

- [ ] **Step 2: 在 XAML 中添加 HY2 专用字段区域**

在 `ui:CardExpander` 内的 `StackPanel` 末尾，在 `CheckBox` 之前添加 HY2 字段：

```xml
<TextBlock Text="Hysteria 2 参数" Style="{StaticResource SectionTitle}" Margin="0,16,0,10"/>

<Grid x:Name="Hy2Fields" Margin="0,0,0,12">
    <Grid.RowDefinitions>
        <RowDefinition Height="Auto"/>
        <RowDefinition Height="Auto"/>
        <RowDefinition Height="Auto"/>
        <RowDefinition Height="Auto"/>
        <RowDefinition Height="Auto"/>
        <RowDefinition Height="Auto"/>
    </Grid.RowDefinitions>
    <Grid.ColumnDefinitions>
        <ColumnDefinition Width="120"/>
        <ColumnDefinition Width="*"/>
    </Grid.ColumnDefinitions>

    <TextBlock Grid.Row="0" Grid.Column="0" Text="密码" Style="{StaticResource FieldLabel}" Margin="0,0,0,12"/>
    <TextBox Grid.Row="0" Grid.Column="1" Text="{Binding Hy2Password, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}" Margin="0,0,0,12"/>

    <TextBlock Grid.Row="1" Grid.Column="0" Text="混淆类型" Style="{StaticResource FieldLabel}" Margin="0,0,0,12"/>
    <ComboBox Grid.Row="1" Grid.Column="1" x:Name="Hy2ObfsBox" IsEditable="True"
              Text="{Binding Hy2Obfs, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}" Margin="0,0,0,12"/>

    <TextBlock Grid.Row="2" Grid.Column="0" Text="混淆密码" Style="{StaticResource FieldLabel}" Margin="0,0,0,12"/>
    <TextBox Grid.Row="2" Grid.Column="1" Text="{Binding Hy2ObfsPassword, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}" Margin="0,0,0,12"/>

    <TextBlock Grid.Row="3" Grid.Column="0" Text="上传带宽" Style="{StaticResource FieldLabel}" Margin="0,0,0,12"/>
    <TextBox Grid.Row="3" Grid.Column="1" Text="{Binding Hy2UpMbps, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}" Margin="0,0,0,12"/>

    <TextBlock Grid.Row="4" Grid.Column="0" Text="下载带宽" Style="{StaticResource FieldLabel}" Margin="0,0,0,12"/>
    <TextBox Grid.Row="4" Grid.Column="1" Text="{Binding Hy2DownMbps, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}" Margin="0,0,0,12"/>

    <CheckBox Grid.Row="5" Grid.Column="1" Content="禁用 UDP" IsChecked="{Binding Hy2DisableUdp, Mode=TwoWay}"
              FontSize="12.5" Foreground="{StaticResource FgSecondaryBrush}"/>
</Grid>
```

- [ ] **Step 3: 根据协议动态显示/隐藏字段**

在 `ServerEditWindow.xaml.cs` 中，订阅 `ProtocolBox` 的 `SelectionChanged` 事件：

```csharp
ProtocolBox.SelectionChanged += (_, _) => SyncFieldVisibility();
```

在 `SyncFieldVisibility()` 方法中实现：

```csharp
private void SyncFieldVisibility()
{
    var isHy2 = _settings.Protocol == Protocols.Hysteria2;
    var isVmess = _settings.Protocol == Protocols.VMess;
    var isSs = _settings.Protocol == Protocols.Shadowsocks;

    var hy2Fields = FindName("Hy2Fields") as Grid;
    if (hy2Fields != null) hy2Fields.Visibility = isHy2 ? Visibility.Visible : Visibility.Collapsed;

    // 非 HY2 时隐藏这些字段
    SetVisibility("AlterIdRow", !isHy2 && isVmess);
    SetVisibility("SecurityRow", !isHy2);
    SetVisibility("NetworkRow", !isHy2);
    SetVisibility("PathRow", !isHy2);
    SetVisibility("HostRow", !isHy2);
    SetVisibility("FlowRow", !isHy2);
    SetVisibility("FingerprintRow", !isHy2);
    SetVisibility("PublicKeyRow", !isHy2);
    SetVisibility("ShortIdRow", !isHy2);
}

private void SetVisibility(string name, bool visible)
{
    var el = FindName(name) as UIElement;
    if (el != null) el.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
}
```

- [ ] **Step 4: 给需要控制可见性的行添加 x:Name**

在 `ServerEditWindow.xaml` 中，给每个 Grid 行添加 `x:Name`：

```xml
<Grid x:Name="AlterIdRow" Margin="0,0,0,12">...</Grid>
<Grid x:Name="SecurityRow" Margin="0,0,0,12">...</Grid>
<Grid x:Name="NetworkRow" Margin="0,0,0,12">...</Grid>
<Grid x:Name="PathRow" Margin="0,0,0,12">...</Grid>
<Grid x:Name="HostRow" Margin="0,0,0,12">...</Grid>
<Grid x:Name="SniRow" Margin="0,0,0,12">...</Grid>
<Grid x:Name="StreamSecurityRow" Margin="0,0,0,12">...</Grid>
<Grid x:Name="FlowRow" Margin="0,0,0,12">...</Grid>
<Grid x:Name="FingerprintRow" Margin="0,0,0,12">...</Grid>
<Grid x:Name="PublicKeyRow" Margin="0,0,0,12">...</Grid>
<Grid x:Name="ShortIdRow" Margin="0,0,0,4">...</Grid>
```

SNI 和传输层安全两行对 HY2 保持可见（因为 HY2 需要 TLS 的 SNI 和 allowInsecure）。

- [ ] **Step 5: 在窗口加载时初始化控件数据**

在构造函数中增加：

```csharp
Hy2ObfsBox.ItemsSource = new[] { "", "salamander" };
```

- [ ] **Step 6: 手动测试 UI**

运行软件：

```bash
cd ProxyClient
dotnet run
```

添加一个节点，双击编辑，切换协议为 `hysteria2`：

- 预期：显示 HY2 字段（密码、混淆类型、混淆密码、上传/下载带宽、禁用 UDP）
- 预期：隐藏 alterId、Security、Network、Path、Host、Flow、Fingerprint、PublicKey、ShortId
- SNI 和 allowInsecure 保持可见

- [ ] **Step 7: 提交**

```bash
git add ProxyClient/ServerEditWindow.xaml ProxyClient/ServerEditWindow.xaml.cs
git commit -m "feat: add HY2 fields to server edit UI"
```

---

## Task 5: 主界面协议标签和默认值

**Files:**
- Modify: `ProxyClient/MainWindow.xaml`

- [ ] **Step 1: 确认协议标签显示**

`ServerRow` 数据模板中的协议标签直接绑定 `Protocol`，新增 HY2 后会自动显示 `hysteria2`。无需额外修改。

- [ ] **Step 2: 提交**

无需修改，跳过提交。

---

## Task 6: 集成测试

**Files:**
- Modify: `ProxyClient/MainWindow.xaml.cs`（如需调试）

- [ ] **Step 1: 完整启动测试**

```bash
cd ProxyClient
dotnet run
```

执行以下操作：

1. 从分享链接添加 HY2 节点
2. 双击节点设为活动
3. 点击电源按钮启动
4. 检查日志：不应出现 `Xray 核心已退出` 或配置错误
5. 检查 `config.json` 包含 `protocol: "hysteria2"`

- [ ] **Step 2: 异常处理验证**

尝试启动一个地址为无效域名的 HY2 节点，观察日志应显示 Xray 启动失败原因，程序不应崩溃。

- [ ] **Step 3: 提交**

如无代码改动，仅测试通过即可。

---

## Task 7: 更新版本并发布

**Files:**
- Modify: `ProxyClient/ProxyClient.csproj`
- Modify: `ProxyClient/AboutWindow.xaml.cs`（版本自动读取，无需修改）

- [ ] **Step 1: 升级版本号**

将 `ProxyClient.csproj` 中的版本从 `1.0.1` 改为 `1.0.2`：

```xml
<Version>1.0.2</Version>
```

- [ ] **Step 2: 构建发布包**

```bash
cd ProxyClient
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false -o ../dist
```

- [ ] **Step 3: 打包并上传 Release**

```bash
Compress-Archive -Path "../dist/*" -DestinationPath "ProxyClient-v1.0.2.zip" -Force
```

将 `ProxyClient-v1.0.2.zip` 上传到 GitHub Release v1.0.2（需要用户提供 GitHub Token）。

- [ ] **Step 4: 提交并推送**

```bash
git add ProxyClient/ProxyClient.csproj
git commit -m "chore: bump version to 1.0.2"
git push
```

---

## 自我检查

- [x] Spec 覆盖：所有需求（解析、生成配置、UI、测试）均有对应任务
- [x] 无占位符：所有步骤包含具体代码和命令
- [x] 类型一致性：`Hy2Password`、`Hy2Obfs` 等字段名在模型、解析器、配置生成器、UI 中一致
