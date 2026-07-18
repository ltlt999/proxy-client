using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Net.Sockets;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using ProxyClient.Core;
using ProxyClient.Models;
using ProxyClient.Parsers;
using ProxyClient.Storage;

namespace ProxyClient.ViewModels;

public class MainViewModel : ViewModelBase
{
    private readonly XrayCoreManager _core = new();
    private AppData _data;

    public ObservableCollection<ServerItem> Servers { get; } = new();
    public ObservableCollection<string> CoreLogs { get; } = new();

    private ServerItem? _selectedServer;
    public ServerItem? SelectedServer { get => _selectedServer; set => Set(ref _selectedServer, value); }

    public ServerItem? ActiveServer => Servers.FirstOrDefault(s => s.IsActive);

    public List<string> RoutingModes { get; } = new() { "规则模式 (绕过大陆)", "全局模式" };
    public int RoutingModeIndex
    {
        get => _data.Settings.RoutingMode;
        set
        {
            if (_data.Settings.RoutingMode == value) return;
            _data.Settings.RoutingMode = value;
            Raise();
            Save();
            if (IsCoreRunning && ActiveServer != null)
            {
                _core.WriteConfig(XrayConfigBuilder.Build(ActiveServer, (RoutingMode)value));
                _core.Stop();
                _core.Start();
                IsCoreRunning = _core.IsRunning;
                StatusText = $"已切换为{(value == (int)RoutingMode.Global ? "全局" : "规则")}模式并重载核心";
            }
        }
    }

    private bool _isCoreRunning;
    public bool IsCoreRunning
    {
        get => _isCoreRunning;
        set { Set(ref _isCoreRunning, value); Raise(nameof(CoreState)); Refresh(); StatusChanged?.Invoke(this, value); }
    }

    public event EventHandler<bool>? StatusChanged;

    private bool _systemProxyOn;
    public bool SystemProxyOn { get => _systemProxyOn; set { if (Set(ref _systemProxyOn, value)) { Raise(nameof(ProxyState)); StatusChanged?.Invoke(this, IsCoreRunning); } } }

    public void SyncSettings()
    {
        Save();
        StatusChanged?.Invoke(this, IsCoreRunning);
    }

    private string _statusText = "就绪 — 双击节点设为活动后点击「启动」";
    public string StatusText { get => _statusText; set => Set(ref _statusText, value); }

    public int LocalSocks => XrayConfigBuilder.SocksPort;
    public int LocalHttp => XrayConfigBuilder.HttpPort;
    public string CoreState => IsCoreRunning ? "运行中" : "已停止";
    public string ProxyState => SystemProxyOn ? "已开启" : "未开启";

    public RelayCommand AddByLinkCommand { get; }
    public RelayCommand ImportSubscriptionCommand { get; }
    public RelayCommand AddManualCommand { get; }
    public RelayCommand EditCommand { get; }
    public RelayCommand DeleteCommand { get; }
    public RelayCommand ActivateCommand { get; }
    public RelayCommand StartCommand { get; }
    public RelayCommand StopCommand { get; }
    public RelayCommand ToggleProxyCommand { get; }
    public RelayCommand TestSelectedCommand { get; }
    public RelayCommand TestAllCommand { get; }
    public RelayCommand ClearLogCommand { get; }

    public MainViewModel()
    {
        _data = App.Data;
        foreach (var s in _data.Servers) Servers.Add(s);
        var active = Servers.FirstOrDefault(x => x.Id == _data.Settings.ActiveServerId);
        if (active != null) active.IsActive = true;

        _core.LogReceived += OnCoreLog;

        AddByLinkCommand = new RelayCommand(AddByLink);
        ImportSubscriptionCommand = new RelayCommand(async () => await ImportSubscriptionAsync());
        AddManualCommand = new RelayCommand(AddManual);
        EditCommand = new RelayCommand(Edit, () => SelectedServer != null);
        DeleteCommand = new RelayCommand(Delete, () => SelectedServer != null);
        ActivateCommand = new RelayCommand(Activate, () => SelectedServer != null);
        StartCommand = new RelayCommand(Start, () => !IsCoreRunning && ActiveServer != null);
        StopCommand = new RelayCommand(Stop, () => IsCoreRunning);
        ToggleProxyCommand = new RelayCommand(ToggleProxy);
        TestSelectedCommand = new RelayCommand(async () => await TestAsync(SelectedServer), () => SelectedServer != null);
        TestAllCommand = new RelayCommand(async () => await TestAllAsync(), () => Servers.Count > 0);
        ClearLogCommand = new RelayCommand(() => CoreLogs.Clear());
    }

    private void OnCoreLog(object? _, string msg)
    {
        Application.Current?.Dispatcher.BeginInvoke(() =>
        {
            CoreLogs.Add(msg);
            while (CoreLogs.Count > 500) CoreLogs.RemoveAt(0);
        });
    }

    private void Refresh() => CommandManager.InvalidateRequerySuggested();

    private void Save()
    {
        _data.Servers = Servers.ToList();
        _data.Settings.ActiveServerId = ActiveServer?.Id ?? "";
        ConfigStore.Save(_data);
    }

    private static Window OwnerWindow => Application.Current.MainWindow;

    private static string ClipboardGet()
    {
        try { return Clipboard.GetText(); } catch { return ""; }
    }

    private void AddByLink()
    {
        var clip = ClipboardGet().Trim();
        var prefill = "";
        if (clip.StartsWith("vmess://") || clip.StartsWith("vless://") || clip.StartsWith("trojan://") || clip.StartsWith("ss://"))
            prefill = clip;

        var dlg = new InputDialog("从分享链接添加", "粘贴 vmess / vless / trojan / ss 链接(可多条,每行一个):", prefill, multiline: true);
        dlg.Owner = OwnerWindow;
        if (dlg.ShowDialog() != true) return;

        var list = ShareLinkParser.ParseMany(dlg.Input);
        if (list.Count == 0) { MessageBox.Show("未能解析出有效节点,请检查链接格式。", "提示"); return; }
        foreach (var s in list) Servers.Add(s);
        Save();
        StatusText = $"已添加 {list.Count} 个节点";
        Refresh();
    }

    private async Task ImportSubscriptionAsync()
    {
        var dlg = new InputDialog("导入订阅", "请输入订阅地址 (http/https):", "");
        dlg.Owner = OwnerWindow;
        if (dlg.ShowDialog() != true || string.IsNullOrWhiteSpace(dlg.Input)) return;
        var url = dlg.Input.Trim();
        StatusText = "正在拉取订阅…";
        try
        {
            var list = await new SubscriptionUpdater().FetchAsync(url);
            if (list.Count == 0) { MessageBox.Show("订阅返回为空或解析失败。", "提示"); StatusText = "订阅为空"; return; }
            foreach (var s in list) Servers.Add(s);
            if (!_data.Settings.Subscriptions.Contains(url)) _data.Settings.Subscriptions.Add(url);
            Save();
            StatusText = $"已从订阅导入 {list.Count} 个节点";
            Refresh();
        }
        catch (Exception ex)
        {
            MessageBox.Show("拉取订阅失败: " + ex.Message, "错误");
            StatusText = "订阅拉取失败";
        }
    }

    private void AddManual()
    {
        var s = new ServerItem();
        var dlg = new ServerEditWindow(s) { Owner = OwnerWindow };
        if (dlg.ShowDialog() == true)
        {
            Servers.Add(s);
            Save();
            StatusText = "已添加节点";
            Refresh();
        }
        dlg = null;
    }

    private void Edit()
    {
        if (SelectedServer == null) return;
        var copy = SelectedServer.Clone();
        var dlg = new ServerEditWindow(copy) { Owner = OwnerWindow };
        if (dlg.ShowDialog() == true)
        {
            SelectedServer.Remark = copy.Remark;
            SelectedServer.Protocol = copy.Protocol;
            SelectedServer.Address = copy.Address;
            SelectedServer.Port = copy.Port;
            SelectedServer.UserId = copy.UserId;
            SelectedServer.AlterId = copy.AlterId;
            SelectedServer.Security = copy.Security;
            SelectedServer.Network = copy.Network;
            SelectedServer.Path = copy.Path;
            SelectedServer.Host = copy.Host;
            SelectedServer.Sni = copy.Sni;
            SelectedServer.StreamSecurity = copy.StreamSecurity;
            SelectedServer.Flow = copy.Flow;
            SelectedServer.Fingerprint = copy.Fingerprint;
            SelectedServer.PublicKey = copy.PublicKey;
            SelectedServer.ShortId = copy.ShortId;
            SelectedServer.SpiderX = copy.SpiderX;
            SelectedServer.AllowInsecure = copy.AllowInsecure;
            Save();
            if (SelectedServer.IsActive && IsCoreRunning) { Stop(); Start(); }
            StatusText = "已保存修改";
        }
    }

    private void Delete()
    {
        if (SelectedServer == null) return;
        if (MessageBox.Show($"确认删除节点 \"{SelectedServer.Display}\"?", "确认删除", MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;
        var wasActive = SelectedServer.IsActive;
        var toRemove = SelectedServer;
        Servers.Remove(toRemove);
        if (wasActive && IsCoreRunning) Stop();
        Save();
        StatusText = "已删除节点";
        Refresh();
    }

    private void Activate()
    {
        if (SelectedServer == null) return;
        foreach (var x in Servers) x.IsActive = false;
        SelectedServer.IsActive = true;
        Save();
        Raise(nameof(ActiveServer));
        if (IsCoreRunning) { Stop(); Start(); }
        else StatusText = $"已设为活动节点: {SelectedServer.Display}";
        Refresh();
    }

    private void Start()
    {
        var active = ActiveServer;
        if (active == null) { StatusText = "请先双击节点将其设为活动节点"; return; }
        var json = XrayConfigBuilder.Build(active, (RoutingMode)RoutingModeIndex);
        _core.WriteConfig(json);
        if (_core.Start())
        {
            IsCoreRunning = true;
            StatusText = $"已连接: {active.Display}   SOCKS {LocalSocks} / HTTP {LocalHttp}";
        }
        else StatusText = "启动失败,请查看下方核心日志";
    }

    private void Stop()
    {
        _core.Stop();
        IsCoreRunning = false;
        StatusText = "已断开";
    }

    private void ToggleProxy()
    {
        if (SystemProxyOn)
        {
            SystemProxy.Disable();
            SystemProxyOn = false;
            StatusText = "系统代理已关闭";
        }
        else
        {
            if (!IsCoreRunning) Start();
            if (!IsCoreRunning) { StatusText = "核心未运行,无法开启系统代理"; return; }
            SystemProxy.Enable(LocalHttp);
            SystemProxyOn = true;
            StatusText = $"系统代理已开启 → 127.0.0.1:{LocalHttp}";
        }
    }

    private async Task TestAsync(ServerItem? s)
    {
        if (s == null) return;
        s.TestResult = "测试中…";
        await Task.Run(async () =>
        {
            try
            {
                using var tcp = new TcpClient();
                var sw = Stopwatch.StartNew();
                using var cts = new CancellationTokenSource(3000);
                await tcp.ConnectAsync(s.Address, s.Port, cts.Token);
                sw.Stop();
                s.TestResult = $"{sw.ElapsedMilliseconds} ms";
            }
            catch { s.TestResult = "超时"; }
        });
    }

    private async Task TestAllAsync()
    {
        StatusText = "正在测速…";
        await Task.WhenAll(Servers.Select(TestAsync).ToList());
        StatusText = "测速完成";
    }

    public void OnWindowClosing()
    {
        try { _core.Stop(); } catch { }
        try { if (SystemProxyOn) SystemProxy.Disable(); } catch { }
        Save();
    }

    public void AutoStartIfNeeded()
    {
        if (!_data.Settings.AutoStartCore) return;
        if (IsCoreRunning) return;
        var active = ActiveServer;
        if (active == null) return;
        Start();
    }
}
