using System.Drawing;
using System.Windows;
using System.Windows.Forms;
using ValperoTray.Services;
using ValperoTray.Views;
using Application = System.Windows.Application;

namespace ValperoTray;

public partial class App : Application
{
    private NotifyIcon  _tray    = null!;
    private PopupWindow _popup   = null!;
    private readonly AppState _state = AppState.Instance;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Only one instance
        var mutex = new System.Threading.Mutex(true, "ValperoTray_SingleInstance", out bool isNew);
        if (!isNew) { Shutdown(); return; }

        _popup = new PopupWindow();

        // ── NotifyIcon setup ─────────────────────────────────────────────────
        _tray = new NotifyIcon
        {
            Icon    = LoadIconColor("green"),
            Text    = "Valpero",
            Visible = true,
        };
        _tray.MouseClick += OnTrayClick;

        // Context menu (right-click)
        _tray.ContextMenuStrip = BuildContextMenu();

        // ── Subscribe to state changes → update icon/tooltip ─────────────────
        _state.StateChanged += UpdateTray;
        UpdateTray();
    }

    private void OnTrayClick(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
            _popup.Toggle(_tray);
    }

    // ── Context menu ─────────────────────────────────────────────────────────

    private ContextMenuStrip BuildContextMenu()
    {
        var menu = new ContextMenuStrip { BackColor = Color.FromArgb(22, 27, 38), ForeColor = Color.FromArgb(241, 245, 249) };

        var refresh = new ToolStripMenuItem("⟳  Refresh now") { Font = new Font("Segoe UI", 9) };
        refresh.Click += async (_, _) => await AppState.Instance.RefreshAsync();

        var dashboard = new ToolStripMenuItem("🌐  Open Dashboard") { Font = new Font("Segoe UI", 9) };
        dashboard.Click += (_, _) => OpenUrl("https://valpero.com/dashboard");

        var settings = new ToolStripMenuItem("⚙  Settings") { Font = new Font("Segoe UI", 9) };
        settings.Click += (_, _) => OpenSettings();

        var quit = new ToolStripMenuItem("✕  Quit") { Font = new Font("Segoe UI", 9) };
        quit.Click += (_, _) => { _tray.Visible = false; Shutdown(); };

        menu.Items.AddRange([refresh, dashboard, new ToolStripSeparator(), settings, new ToolStripSeparator(), quit]);
        return menu;
    }

    // ── Tray icon update ─────────────────────────────────────────────────────

    private void UpdateTray()
    {
        Dispatcher.Invoke(() =>
        {
            _tray.Text = $"Valpero — {_state.StatusLabel}";
            _tray.Icon = _state.DownCount > 0 ? LoadIconColor("red") : LoadIconColor("green");
        });
    }

    // ── Icon helpers ──────────────────────────────────────────────────────────

    // Draw a colored circle programmatically — no external .ico needed
    private static Icon LoadIconColor(string color)
    {
        var bmp = new Bitmap(16, 16);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        g.Clear(Color.Transparent);

        var fill = color == "red"   ? Color.FromArgb(239, 68, 68)   // red
                 : color == "orange"? Color.FromArgb(245, 158, 11)  // orange
                                    : Color.FromArgb(34,  197, 94);  // green

        using var brush = new SolidBrush(fill);
        g.FillEllipse(brush, 2, 2, 11, 11);

        return Icon.FromHandle(bmp.GetHicon());
    }

    // ── Settings window ───────────────────────────────────────────────────────

    public void OpenSettings()
    {
        if (_popup.IsVisible) _popup.Hide();
        var win = new SettingsWindow();
        win.Show();
        win.Activate();
    }

    public static void OpenUrl(string url)
    {
        try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(url) { UseShellExecute = true }); }
        catch { }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _tray.Visible = false;
        _tray.Dispose();
        base.OnExit(e);
    }
}
