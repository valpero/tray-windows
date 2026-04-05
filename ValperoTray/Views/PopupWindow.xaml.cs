using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using ValperoTray.Models;
using ValperoTray.Services;
using Monitor = ValperoTray.Models.Monitor;
using Color = System.Windows.Media.Color;
using Button = System.Windows.Controls.Button;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;
using FontFamily = System.Windows.Media.FontFamily;
using Rectangle = System.Windows.Shapes.Rectangle;
using Cursors = System.Windows.Input.Cursors;

namespace ValperoTray.Views;

public partial class PopupWindow : Window
{
    private readonly AppState _state = AppState.Instance;

    public PopupWindow()
    {
        InitializeComponent();
        _state.PropertyChanged += (_, _) => Dispatcher.Invoke(Rebuild);
        Deactivated += (_, _) => Hide();
    }

    // ── Toggle ────────────────────────────────────────────────────────────────

    public void Toggle(System.Windows.Forms.NotifyIcon tray)
    {
        if (IsVisible) { Hide(); return; }
        PositionNearTray(tray);
        Rebuild();
        Show();
        Activate();
    }

    private void PositionNearTray(System.Windows.Forms.NotifyIcon tray)
    {
        var screen = System.Windows.Forms.Screen.PrimaryScreen!.WorkingArea;
        var dpi    = PresentationSource.FromVisual(this)?.CompositionTarget?.TransformToDevice.M11 ?? 1.0;

        // Position bottom-right (above tray area)
        Left = (screen.Right / dpi) - ActualWidth - 12;
        Top  = (screen.Bottom / dpi) - 540 - 12;
        if (Top < 0) Top = 8;
    }

    // ── Rebuild content ────────────────────────────────────────────────────────

    private void Rebuild()
    {
        ContentPanel.Children.Clear();
        UpdateStatusDot();
        UpdateFooter();

        if (!_state.HasKey)
        {
            AddNoKeyState();
            return;
        }
        if (_state.IsLoading && _state.Monitors.Count == 0)
        {
            AddLoadingSkeleton();
            return;
        }

        if (_state.OpenIncidents.Count > 0)
            AddIncidentsSection();

        AddMonitorsSection();

        if (_state.Heartbeats.Count > 0)
            AddHeartbeatsSection();

        if (_state.Agents.Count > 0)
            AddServersSection();
    }

    // ── Status dot ────────────────────────────────────────────────────────────

    private void UpdateStatusDot()
    {
        if (!_state.HasKey)
            StatusDot.Fill = new SolidColorBrush(Color.FromRgb(100, 116, 139));
        else if (_state.DownCount > 0)
            StatusDot.Fill = new SolidColorBrush(Color.FromRgb(239, 68, 68));
        else
            StatusDot.Fill = new SolidColorBrush(Color.FromRgb(34, 197, 94));
    }

    private void UpdateFooter()
    {
        LastRefreshLabel.Text = _state.LastRefresh.HasValue
            ? $"Updated {_state.LastRefresh:HH:mm}  ·  {_state.Monitors.Count} monitors"
            : _state.Error != null ? $"⚠  {_state.Error}" : "";
    }

    // ── No key state ──────────────────────────────────────────────────────────

    private void AddNoKeyState()
    {
        var panel = new StackPanel { HorizontalAlignment = System.Windows.HorizontalAlignment.Center, Margin = new Thickness(16, 24, 16, 24) };
        panel.Children.Add(new TextBlock
        {
            Text = "🔑", FontSize = 28, HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
            Margin = new Thickness(0, 0, 0, 12),
        });
        panel.Children.Add(new TextBlock
        {
            Text = "No API key set", FontSize = 13, FontWeight = FontWeights.SemiBold,
            HorizontalAlignment = System.Windows.HorizontalAlignment.Center, Margin = new Thickness(0, 0, 0, 6),
        });
        panel.Children.Add(new TextBlock
        {
            Text = "Open Settings to connect your account.", FontSize = 11,
            Foreground = (Brush)App.Current.Resources["Text3Brush"],
            HorizontalAlignment = System.Windows.HorizontalAlignment.Center, TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 16),
        });
        var btn = MakePrimaryButton("Open Settings", () => (App.Current as App)!.OpenSettings());
        btn.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
        panel.Children.Add(btn);
        ContentPanel.Children.Add(panel);
    }

    // ── Loading skeleton ──────────────────────────────────────────────────────

    private void AddLoadingSkeleton()
    {
        for (int i = 0; i < 3; i++)
        {
            var row = new Grid { Margin = new Thickness(14, 5, 14, 5) };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            var dot = new Ellipse { Width = 7, Height = 7, Fill = (Brush)App.Current.Resources["BgSurfaceBrush"], Margin = new Thickness(0, 0, 8, 0) };
            var bar = new Border { Height = 10, CornerRadius = new CornerRadius(3), Background = (Brush)App.Current.Resources["BgSurfaceBrush"] };
            Grid.SetColumn(dot, 0); Grid.SetColumn(bar, 1);
            row.Children.Add(dot); row.Children.Add(bar);
            ContentPanel.Children.Add(row);
        }
    }

    // ── Incidents ─────────────────────────────────────────────────────────────

    private void AddIncidentsSection()
    {
        AddSectionHeader("🚨  Open Incidents", _state.OpenIncidents.Count, isAlert: true);
        foreach (var inc in _state.OpenIncidents)
        {
            var row = MakeRow(
                dotColor: Color.FromRgb(239, 68, 68),
                mainText: inc.SiteName,
                subText:  inc.Cause,
                badge:    inc.DurationLabel,
                badgeColor: Color.FromRgb(239, 68, 68)
            );
            ContentPanel.Children.Add(row);
        }
        AddDivider();
    }

    // ── Monitors ──────────────────────────────────────────────────────────────

    private void AddMonitorsSection()
    {
        AddSectionHeader("Monitors", _state.Monitors.Count, isAlert: _state.DownCount > 0);

        if (_state.Monitors.Count == 0)
        {
            AddEmptyRow("No monitors yet");
            return;
        }

        // Down first
        foreach (var m in _state.Monitors.Where(m => !m.IsUp))
            ContentPanel.Children.Add(MakeMonitorRow(m));
        foreach (var m in _state.Monitors.Where(m => m.IsUp))
            ContentPanel.Children.Add(MakeMonitorRow(m));
    }

    private UIElement MakeMonitorRow(Monitor m)
    {
        var badge = _state.ShowResponseTime ? m.ResponseTimeLabel : null;
        Color? badgeColor = m.LastResponseTime switch
        {
            < 300  => Color.FromRgb(34,  197, 94),
            < 1000 => Color.FromRgb(245, 158, 11),
            _      => Color.FromRgb(239, 68,  68),
        };
        var sub = _state.ShowUptime ? m.UptimeLabel : null;

        var row = MakeRow(
            dotColor:   m.IsUp ? Color.FromRgb(34, 197, 94) : Color.FromRgb(239, 68, 68),
            mainText:   m.DisplayName,
            subText:    sub,
            badge:      badge,
            badgeColor: m.LastResponseTime.HasValue ? badgeColor : null
        );
        row.Cursor = Cursors.Hand;
        row.MouseLeftButtonUp += (_, _) => App.OpenUrl(m.DashboardUrl);
        return row;
    }

    // ── Heartbeats ────────────────────────────────────────────────────────────

    private void AddHeartbeatsSection()
    {
        AddDivider();
        var anyDown = _state.Heartbeats.Any(h => !h.IsUp && !h.IsNew);
        AddSectionHeader("Heartbeats", _state.Heartbeats.Count, isAlert: anyDown);

        foreach (var hb in _state.Heartbeats)
        {
            var dot = hb.IsUp  ? Color.FromRgb(34, 197, 94)
                    : hb.IsNew ? Color.FromRgb(100, 116, 139)
                               : Color.FromRgb(239, 68, 68);
            ContentPanel.Children.Add(MakeRow(dot, hb.Name, badge: hb.Status.ToUpper(),
                badgeColor: hb.IsUp ? (Color?)Color.FromRgb(34, 197, 94) : Color.FromRgb(239, 68, 68)));
        }
    }

    // ── Servers ───────────────────────────────────────────────────────────────

    private void AddServersSection()
    {
        AddDivider();
        var anyOffline = _state.Agents.Any(a => !a.IsOnline);
        AddSectionHeader("Servers", _state.Agents.Count, isAlert: anyOffline);

        foreach (var agent in _state.Agents)
        {
            var dot  = agent.IsOnline ? Color.FromRgb(34, 197, 94) : Color.FromRgb(100, 116, 139);
            var sub  = agent.CpuPct.HasValue ? $"CPU {agent.CpuPct:F0}%  RAM {agent.RamPct:F0}%" : null;
            var row  = MakeRow(dot, agent.DisplayName, subText: sub);
            row.Cursor = Cursors.Hand;
            row.MouseLeftButtonUp += (_, _) => App.OpenUrl(agent.DashboardUrl);
            ContentPanel.Children.Add(row);
        }
    }

    // ── UI builders ───────────────────────────────────────────────────────────

    private void AddSectionHeader(string title, int count, bool isAlert = false)
    {
        var panel = new Grid { Margin = new Thickness(14, 8, 14, 4) };
        panel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        panel.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var label = new TextBlock
        {
            Text = title.ToUpper(), FontSize = 10, FontWeight = FontWeights.SemiBold,
            Foreground = (Brush)App.Current.Resources["Text3Brush"], VerticalAlignment = VerticalAlignment.Center,
        };
        Grid.SetColumn(label, 0);
        panel.Children.Add(label);

        if (count > 0)
        {
            var badge = new Border
            {
                Background = isAlert ? new SolidColorBrush(Color.FromArgb(30, 239, 68, 68))
                                     : new SolidColorBrush(Color.FromArgb(30, 100, 116, 139)),
                CornerRadius = new CornerRadius(99), Padding = new Thickness(6, 1, 6, 1),
            };
            badge.Child = new TextBlock
            {
                Text = count.ToString(), FontSize = 10, FontWeight = FontWeights.Bold,
                Foreground = isAlert ? new SolidColorBrush(Color.FromRgb(239, 68, 68))
                                     : (Brush)App.Current.Resources["Text3Brush"],
            };
            Grid.SetColumn(badge, 1);
            panel.Children.Add(badge);
        }
        ContentPanel.Children.Add(panel);
    }

    private Border MakeRow(Color dotColor, string mainText, string? subText = null,
                           string? badge = null, Color? badgeColor = null)
    {
        var grid = new Grid { Margin = new Thickness(14, 3, 14, 3) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        // Dot
        var dot = new Ellipse { Width = 7, Height = 7, Fill = new SolidColorBrush(dotColor), Margin = new Thickness(0, 0, 8, 0), VerticalAlignment = VerticalAlignment.Center };
        Grid.SetColumn(dot, 0);
        grid.Children.Add(dot);

        // Main text + sub
        var textPanel = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        textPanel.Children.Add(new TextBlock { Text = mainText, FontSize = 12, TextTrimming = TextTrimming.CharacterEllipsis });
        if (!string.IsNullOrWhiteSpace(subText))
            textPanel.Children.Add(new TextBlock { Text = subText, FontSize = 10, Foreground = (Brush)App.Current.Resources["Text3Brush"] });
        Grid.SetColumn(textPanel, 1);
        grid.Children.Add(textPanel);

        // Badge
        if (!string.IsNullOrWhiteSpace(badge))
        {
            var col = badgeColor ?? Color.FromRgb(100, 116, 139);
            var badgeBorder = new Border
            {
                Background   = new SolidColorBrush(Color.FromArgb(25, col.R, col.G, col.B)),
                CornerRadius = new CornerRadius(99),
                Padding      = new Thickness(6, 2, 6, 2),
            };
            badgeBorder.Child = new TextBlock { Text = badge, FontSize = 10, FontFamily = new FontFamily("Consolas"), Foreground = new SolidColorBrush(col) };
            Grid.SetColumn(badgeBorder, 2);
            grid.Children.Add(badgeBorder);
        }

        var wrapper = new Border { Background = Brushes.Transparent, Child = grid };
        wrapper.MouseEnter += (_, _) => wrapper.Background = new SolidColorBrush(Color.FromArgb(20, 255, 255, 255));
        wrapper.MouseLeave += (_, _) => wrapper.Background = Brushes.Transparent;
        return wrapper;
    }

    private void AddDivider() =>
        ContentPanel.Children.Add(new Rectangle { Height = 1, Fill = (Brush)App.Current.Resources["BorderBrush"], Margin = new Thickness(0, 4, 0, 4) });

    private void AddEmptyRow(string text) =>
        ContentPanel.Children.Add(new TextBlock { Text = text, FontSize = 11, Foreground = (Brush)App.Current.Resources["Text3Brush"], HorizontalAlignment = System.Windows.HorizontalAlignment.Center, Margin = new Thickness(0, 8, 0, 8) });

    private static Button MakePrimaryButton(string label, Action onClick)
    {
        var btn = new Button { Content = label, Style = (Style)App.Current.Resources["PrimaryButton"] };
        btn.Click += (_, _) => onClick();
        return btn;
    }

    // ── Button handlers ───────────────────────────────────────────────────────

    private async void RefreshBtn_Click(object sender, RoutedEventArgs e) =>
        await AppState.Instance.RefreshAsync();

    private void SettingsBtn_Click(object sender, RoutedEventArgs e) =>
        (App.Current as App)!.OpenSettings();

    private void DashboardBtn_Click(object sender, RoutedEventArgs e) =>
        App.OpenUrl("https://valpero.com/dashboard");
}
