using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows;
using Forms = System.Windows.Forms;
using DrawingColor = System.Drawing.Color;

namespace ProxyClient.Core;

public class TrayIconManager : IDisposable
{
    private readonly Forms.NotifyIcon _icon;
    private bool _disposed;

    public event EventHandler? ShowRequested;
    public event EventHandler? ExitRequested;
    public event EventHandler? ToggleProxyRequested;

    public TrayIconManager()
    {
        _icon = new Forms.NotifyIcon
        {
            Icon = CreateIcon(),
            Visible = false,
            Text = "ProxyClient"
        };
        _icon.MouseDoubleClick += (_, _) => ShowRequested?.Invoke(this, EventArgs.Empty);
        _icon.ContextMenuStrip = BuildMenu();
    }

    Forms.ContextMenuStrip BuildMenu()
    {
        var menu = new Forms.ContextMenuStrip();
        menu.Items.Add("显示主窗口", null, (_, _) => ShowRequested?.Invoke(this, EventArgs.Empty));
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add("切换系统代理", null, (_, _) => ToggleProxyRequested?.Invoke(this, EventArgs.Empty));
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add("退出", null, (_, _) => ExitRequested?.Invoke(this, EventArgs.Empty));
        return menu;
    }

    public void Show() => _icon.Visible = true;

    public void SetStatus(bool coreRunning, bool proxyOn)
    {
        _icon.Text = $"ProxyClient — {(coreRunning ? (proxyOn ? "代理运行中" : "核心运行中(系统代理未开)") : "未连接")}";
        _icon.Icon = CreateIcon(coreRunning);
    }

    static Icon CreateIcon(bool active = false)
    {
        var bmp = new Bitmap(32, 32);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(DrawingColor.Transparent);

        var fillColor = active ? DrawingColor.FromArgb(94, 201, 140) : DrawingColor.FromArgb(126, 128, 156);
        var accentColor = active ? DrawingColor.FromArgb(70, 180, 120) : DrawingColor.FromArgb(100, 102, 130);
        var bgColor = DrawingColor.FromArgb(26, 27, 38);

        using var outerBrush = new SolidBrush(fillColor);
        using var innerBrush = new SolidBrush(bgColor);
        using var arrowBrush = new SolidBrush(accentColor);
        using var arrowPen = new Pen(accentColor, 2.5f) { StartCap = LineCap.Round, EndCap = LineCap.Round };

        g.FillEllipse(outerBrush, 2, 2, 27, 27);
        g.FillEllipse(innerBrush, 6, 6, 19, 19);

        var points = new[] { new PointF(15, 12), new PointF(12, 17), new PointF(18, 17) };
        g.FillPolygon(arrowBrush, points);

        float cx = 15, cy = 17;
        float len = 6;
        g.DrawLine(arrowPen, cx, cy, cx, cy + len);

        var handle = bmp.GetHicon();
        return Icon.FromHandle(handle);
    }

    public void ShowBalloon(string title, string message, int timeout = 2000)
    {
        _icon.ShowBalloonTip(timeout, title, message, Forms.ToolTipIcon.Info);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _icon.Visible = false;
        _icon.Dispose();
    }
}
