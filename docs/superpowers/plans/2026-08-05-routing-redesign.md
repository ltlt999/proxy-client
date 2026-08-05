# 路由规则重构实现计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 实现更灵活的路由系统，包括优化绕过大陆规则、新增自定义规则模式、支持规则文件导入。

**Architecture:** 扩展 `RoutingMode` 枚举和 `AppSettings` 数据模型，新增 `RoutingRule` 类；`XrayConfigBuilder` 根据模式生成对应路由配置；`SettingsWindow` 提供自定义规则编辑与导入 UI。

**Tech Stack:** C# 12, .NET 9, WPF, WPF-UI, System.Text.Json, Xray-core

---

## Task 1: 数据模型扩展

**Files:**
- Create: `ProxyClient/Models/RoutingRule.cs`
- Modify: `ProxyClient/Models/ServerItem.cs`
- Modify: `ProxyClient/Storage/ConfigStore.cs`
- Modify: `ProxyClient/Core/XrayConfigBuilder.cs`

- [ ] **Step 1: 创建 RoutingRule 模型**

```csharp
namespace ProxyClient.Models;

public class RoutingRule
{
    public string Type { get; set; } = "domain"; // domain / ip / port
    public string Value { get; set; } = "";
    public string Action { get; set; } = "direct"; // direct / proxy / block
}
```

保存到 `ProxyClient/Models/RoutingRule.cs`。

- [ ] **Step 2: 修改 RoutingMode 枚举**

在 `ProxyClient/Core/XrayConfigBuilder.cs` 中：

```csharp
public enum RoutingMode { Rule = 0, Global = 1, Custom = 2 }
```

- [ ] **Step 3: 扩展 AppSettings**

在 `ProxyClient/Storage/ConfigStore.cs` 中：

```csharp
public List<RoutingRule> CustomRules { get; set; } = new();
```

确保 `using ProxyClient.Models;` 已引入。

- [ ] **Step 4: 提交**

```bash
git add ProxyClient/Models/RoutingRule.cs ProxyClient/Storage/ConfigStore.cs ProxyClient/Core/XrayConfigBuilder.cs
git commit -m "feat: add RoutingRule model and Custom routing mode"
```

---

## Task 2: 优化绕过大陆规则

**Files:**
- Modify: `ProxyClient/Core/XrayConfigBuilder.cs`

- [ ] **Step 1: 更新规则模式配置**

在 `Build` 方法中，替换现有规则模式代码：

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

- [ ] **Step 2: 手动测试配置生成**

运行软件，选择「规则模式 (绕过大陆)」，启动核心，检查生成的 `config.json`：

```bash
cd ProxyClient
dotnet run
```

检查 `bin/Debug/net9.0-windows/config/config.json` 中的 `routing.rules` 是否包含上述三条规则。

- [ ] **Step 3: 提交**

```bash
git add ProxyClient/Core/XrayConfigBuilder.cs
git commit -m "feat: improve bypass-mainland routing rules"
```

---

## Task 3: 实现自定义规则配置生成

**Files:**
- Modify: `ProxyClient/Core/XrayConfigBuilder.cs`

- [ ] **Step 1: 修改 Build 方法签名**

从：

```csharp
public static string Build(ServerItem server, RoutingMode mode)
```

改为：

```csharp
public static string Build(ServerItem server, RoutingMode mode, List<RoutingRule>? customRules = null)
```

- [ ] **Step 2: 添加自定义规则分支**

在 `Build` 方法的 `if (mode == RoutingMode.Rule)` 之后添加：

```csharp
else if (mode == RoutingMode.Custom)
{
    var rules = new JsonArray();
    foreach (var r in customRules ?? new List<RoutingRule>())
    {
        if (string.IsNullOrWhiteSpace(r.Value)) continue;
        var rule = new JsonObject { ["type"] = "field", ["outboundTag"] = r.Action };
        var values = ParseRuleValues(r.Value);
        if (r.Type == "domain") rule["domain"] = values;
        else if (r.Type == "ip") rule["ip"] = values;
        else if (r.Type == "port") rule["port"] = r.Value.Replace(" ", "").Replace("\n", ",").Trim(',');
        rules.Add(rule);
    }
    root["routing"] = new JsonObject
    {
        ["domainStrategy"] = "IPIfNonMatch",
        ["rules"] = rules
    };
}
```

- [ ] **Step 3: 添加 ParseRuleValues 辅助方法**

在 `Build` 方法之后添加：

```csharp
static JsonArray ParseRuleValues(string value)
{
    var array = new JsonArray();
    foreach (var raw in value.Split(',', '\n', '\r'))
    {
        var v = raw.Trim();
        if (!string.IsNullOrEmpty(v)) array.Add(v);
    }
    return array;
}
```

- [ ] **Step 4: 提交**

```bash
git add ProxyClient/Core/XrayConfigBuilder.cs
git commit -m "feat: generate xray config for custom routing rules"
```

---

## Task 4: 主界面路由下拉框更新

**Files:**
- Modify: `ProxyClient/ViewModels/MainViewModel.cs`
- Modify: `ProxyClient/MainWindow.xaml`

- [ ] **Step 1: 更新路由模式列表**

在 `MainViewModel.cs` 中：

```csharp
public List<string> RoutingModes { get; } = new() { "规则模式 (绕过大陆)", "全局模式", "自定义规则" };
```

- [ ] **Step 2: 更新路由切换逻辑**

在 `RoutingModeIndex` 的 setter 中，找到：

```csharp
_core.WriteConfig(XrayConfigBuilder.Build(ActiveServer, (RoutingMode)value));
```

改为：

```csharp
_core.WriteConfig(XrayConfigBuilder.Build(ActiveServer, (RoutingMode)value, _data.Settings.CustomRules));
```

- [ ] **Step 3: 更新 Start 方法**

找到 `Start` 方法中的：

```csharp
var json = XrayConfigBuilder.Build(active, (RoutingMode)RoutingModeIndex);
```

改为：

```csharp
var json = XrayConfigBuilder.Build(active, (RoutingMode)RoutingModeIndex, _data.Settings.CustomRules);
```

- [ ] **Step 4: 提交**

```bash
git add ProxyClient/ViewModels/MainViewModel.cs
git commit -m "feat: add custom routing mode to main UI"
```

---

## Task 5: 设置窗口自定义规则 UI

**Files:**
- Create: `ProxyClient/RoutingRuleEditWindow.xaml`
- Create: `ProxyClient/RoutingRuleEditWindow.xaml.cs`
- Modify: `ProxyClient/SettingsWindow.xaml`
- Modify: `ProxyClient/SettingsWindow.xaml.cs`

- [ ] **Step 1: 创建规则编辑弹窗 XAML**

```xml
<ui:FluentWindow x:Class="ProxyClient.RoutingRuleEditWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:ui="http://schemas.lepo.co/wpfui/2022/xaml"
        Title="编辑规则" Width="420" Height="420"
        WindowStartupLocation="CenterOwner"
        Background="{StaticResource BgBrush}"
        ResizeMode="NoResize" ShowInTaskbar="False">
    <Grid Margin="24">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
            <RowDefinition Height="Auto"/>
        </Grid.RowDefinitions>

        <TextBlock Text="规则类型" Style="{StaticResource FieldLabel}" Margin="0,0,0,6"/>
        <ComboBox Grid.Row="1" x:Name="TypeBox" Margin="0,0,0,16"/>

        <TextBlock Grid.Row="2" Text="规则值" Style="{StaticResource FieldLabel}" Margin="0,0,0,6"/>
        <TextBox Grid.Row="3" x:Name="ValueBox" TextWrapping="Wrap" AcceptsReturn="True"
                 VerticalScrollBarVisibility="Auto" Height="120" Margin="0,0,0,6"/>
        <TextBlock Grid.Row="4" Text="每行一个，支持逗号分隔" FontSize="11"
                   Foreground="{StaticResource MutedBrush}" Margin="0,0,0,16"/>

        <TextBlock Grid.Row="5" Text="动作" Style="{StaticResource FieldLabel}" Margin="0,0,0,6"/>
        <ComboBox Grid.Row="6" x:Name="ActionBox" Margin="0,0,0,16"/>

        <StackPanel Grid.Row="7" Orientation="Horizontal" HorizontalAlignment="Right">
            <Button Width="100" Height="34" Margin="0,0,10,0" IsDefault="True" Click="Ok_Click"
                    Style="{StaticResource AccentButton}">确定</Button>
            <Button Width="100" Height="34" IsCancel="True" Style="{StaticResource SecondaryButton}">取消</Button>
        </StackPanel>
    </Grid>
</ui:FluentWindow>
```

保存到 `ProxyClient/RoutingRuleEditWindow.xaml`。

- [ ] **Step 2: 创建规则编辑弹窗代码**

```csharp
using System.Windows;
using ProxyClient.Models;
using Wpf.Ui.Controls;

namespace ProxyClient;

public partial class RoutingRuleEditWindow : FluentWindow
{
    public RoutingRule Rule { get; }

    public RoutingRuleEditWindow(RoutingRule rule)
    {
        InitializeComponent();
        Rule = rule;
        TypeBox.ItemsSource = new[] { "domain", "ip", "port" };
        ActionBox.ItemsSource = new[] { "direct", "proxy", "block" };
        TypeBox.SelectedItem = rule.Type;
        ValueBox.Text = rule.Value;
        ActionBox.SelectedItem = rule.Action;
    }

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        Rule.Type = TypeBox.SelectedItem?.ToString() ?? "domain";
        Rule.Value = ValueBox.Text;
        Rule.Action = ActionBox.SelectedItem?.ToString() ?? "direct";
        DialogResult = true;
    }
}
```

保存到 `ProxyClient/RoutingRuleEditWindow.xaml.cs`。

- [ ] **Step 3: 在 SettingsWindow.xaml 中添加规则卡片**

在设置窗口的 `ScrollViewer/StackPanel` 中，在「启动与运行」卡片之后添加：

```xml
<TextBlock Text="路由规则" Style="{StaticResource SectionTitle}" Margin="0,8,0,0"/>
<Border Style="{StaticResource CardBorder}" Margin="0,0,0,16" Padding="18">
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
        </Grid.RowDefinitions>
        <TextBlock Grid.Row="0" Text="自定义规则仅在选择「自定义规则」模式时生效。"
                   FontSize="12" Foreground="{StaticResource MutedBrush}" TextWrapping="Wrap" Margin="0,0,0,12"/>
        <ListBox Grid.Row="1" x:Name="RulesList" Height="160" BorderBrush="{StaticResource BorderBrush}" BorderThickness="1">
            <ListBox.ItemTemplate>
                <DataTemplate DataType="{x:Type models:RoutingRule}">
                    <StackPanel Orientation="Horizontal">
                        <Border Background="{StaticResource AccentSoftBrush}" CornerRadius="6" Padding="6,2" Margin="0,0,8,0">
                            <TextBlock Text="{Binding Type}" FontSize="11" FontWeight="SemiBold" Foreground="{StaticResource AccentSoftFgBrush}"/>
                        </Border>
                        <TextBlock Text="{Binding Action}" FontSize="12" Foreground="{StaticResource MutedBrush}" Width="50"/>
                        <TextBlock Text="{Binding Value}" FontSize="12" Foreground="{StaticResource FgSecondaryBrush}" TextTrimming="CharacterEllipsis" MaxWidth="280"/>
                    </StackPanel>
                </DataTemplate>
            </ListBox.ItemTemplate>
        </ListBox>
        <StackPanel Grid.Row="1" Orientation="Horizontal" HorizontalAlignment="Right" VerticalAlignment="Bottom" Margin="0,12,0,0">
            <Button Style="{StaticResource GhostButton}" Click="AddRule_Click" Margin="0,0,6,0">添加</Button>
            <Button Style="{StaticResource GhostButton}" Click="EditRule_Click" Margin="0,0,6,0">编辑</Button>
            <Button Style="{StaticResource GhostButton}" Click="DeleteRule_Click" Margin="0,0,6,0">删除</Button>
            <Button Style="{StaticResource GhostButton}" Click="ImportRules_Click">导入</Button>
        </StackPanel>
    </Grid>
</Border>
```

- [ ] **Step 4: 在 SettingsWindow.xaml 顶部添加 models 命名空间**

在根元素 `ui:FluentWindow` 中添加：

```xml
xmlns:models="clr-namespace:ProxyClient.Models"
```

- [ ] **Step 5: 在 SettingsWindow.xaml.cs 中实现规则操作**

修改构造函数：

```csharp
public SettingsWindow(AppSettings settings)
{
    InitializeComponent();
    _settings = settings;
    AutoStartChk.IsChecked = AutoStartHelper.IsEnabled();
    MinimizeOnStartChk.IsChecked = settings.MinimizeOnStart;
    MinimizeOnCloseChk.IsChecked = settings.MinimizeOnClose;
    AutoStartCoreChk.IsChecked = settings.AutoStartCore;
    RulesList.ItemsSource = settings.CustomRules;
}
```

添加事件处理方法：

```csharp
private void AddRule_Click(object sender, RoutedEventArgs e)
{
    var rule = new RoutingRule();
    var dlg = new RoutingRuleEditWindow(rule) { Owner = this };
    if (dlg.ShowDialog() == true)
    {
        _settings.CustomRules.Add(rule);
        RulesList.Items.Refresh();
    }
}

private void EditRule_Click(object sender, RoutedEventArgs e)
{
    if (RulesList.SelectedItem is not RoutingRule rule) return;
    var copy = new RoutingRule { Type = rule.Type, Value = rule.Value, Action = rule.Action };
    var dlg = new RoutingRuleEditWindow(copy) { Owner = this };
    if (dlg.ShowDialog() == true)
    {
        rule.Type = copy.Type;
        rule.Value = copy.Value;
        rule.Action = copy.Action;
        RulesList.Items.Refresh();
    }
}

private void DeleteRule_Click(object sender, RoutedEventArgs e)
{
    if (RulesList.SelectedItem is not RoutingRule rule) return;
    _settings.CustomRules.Remove(rule);
    RulesList.Items.Refresh();
}

private void ImportRules_Click(object sender, RoutedEventArgs e)
{
    var dlg = new Microsoft.Win32.OpenFileDialog
    {
        Filter = "规则文件 (*.json;*.txt)|*.json;*.txt|所有文件 (*.*)|*.*",
        Title = "导入路由规则"
    };
    if (dlg.ShowDialog() != true) return;

    try
    {
        var imported = RuleImporter.Import(dlg.FileName);
        foreach (var r in imported) _settings.CustomRules.Add(r);
        RulesList.Items.Refresh();
    }
    catch (Exception ex)
    {
        MessageBox.Show("导入失败: " + ex.Message, "错误");
    }
}
```

- [ ] **Step 6: 提交**

```bash
git add ProxyClient/RoutingRuleEditWindow.xaml ProxyClient/RoutingRuleEditWindow.xaml.cs ProxyClient/SettingsWindow.xaml ProxyClient/SettingsWindow.xaml.cs
git commit -m "feat: add custom routing rules editor in settings"
```

---

## Task 6: 规则文件导入

**Files:**
- Create: `ProxyClient/Parsers/RuleImporter.cs`

- [ ] **Step 1: 创建 RuleImporter**

```csharp
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
```

保存到 `ProxyClient/Parsers/RuleImporter.cs`。

- [ ] **Step 2: 手动测试导入**

创建一个测试文件 `test-rules.json`：

```json
[
  { "type": "field", "outboundTag": "direct", "domain": ["geosite:cn"] },
  { "type": "field", "outboundTag": "proxy", "domain": ["geosite:google"] }
]
```

在设置窗口点击「导入」，选择该文件，验证列表新增两条规则。

- [ ] **Step 3: 提交**

```bash
git add ProxyClient/Parsers/RuleImporter.cs
git commit -m "feat: support importing routing rules from json and domain lists"
```

---

## Task 7: 集成测试

**Files:**
- Modify: `ProxyClient/ViewModels/MainViewModel.cs`（如需调整）

- [ ] **Step 1: 测试三种路由模式**

运行软件：

```bash
cd ProxyClient
dotnet run
```

依次选择三种路由模式并启动核心，检查 `config/config.json`：

1. 规则模式：应包含 `geosite:cn` / `geoip:cn` direct 规则
2. 全局模式：`routing` 应仅含 `domainStrategy: AsIs`
3. 自定义规则：`routing.rules` 应包含用户在设置中添加的规则

- [ ] **Step 2: 测试规则持久化**

添加几条自定义规则，关闭设置窗口，关闭软件。重新打开软件，进入设置，验证规则仍然存在。

- [ ] **Step 3: 测试切换活动时重载**

在核心运行状态下切换路由模式，验证核心停止并重新启动，且状态栏显示正常。

- [ ] **Step 4: 提交**

无代码改动则无需提交。

---

## Task 8: 版本更新与发布

**Files:**
- Modify: `ProxyClient/ProxyClient.csproj`

- [ ] **Step 1: 升级版本号**

```xml
<Version>1.0.3</Version>
```

- [ ] **Step 2: 构建并发布**

```bash
cd ProxyClient
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false -o ../dist
Compress-Archive -Path "../dist/*" -DestinationPath "ProxyClient-v1.0.3.zip" -Force
```

- [ ] **Step 3: 提交并推送**

```bash
git add ProxyClient/ProxyClient.csproj
git commit -m "chore: bump version to 1.0.3"
git push
```

- [ ] **Step 4: 上传 Release**

使用 GitHub API 创建 Release v1.0.3 并上传 `ProxyClient-v1.0.3.zip`。

---

## 自我检查

- [x] Spec 覆盖：优化规则、自定义规则、规则导入、UI 编辑均有任务
- [x] 无占位符：所有步骤包含具体代码和命令
- [x] 类型一致性：`RoutingRule` / `CustomRules` / `RoutingMode.Custom` 命名一致
