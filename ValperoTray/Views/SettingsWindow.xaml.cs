using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;
using ValperoTray.Services;
using Color = System.Windows.Media.Color;

namespace ValperoTray.Views;

public partial class SettingsWindow : Window
{
    private readonly AppState _state = AppState.Instance;
    private bool _showingKey = false;

    public SettingsWindow()
    {
        InitializeComponent();
        LoadCurrentValues();
    }

    private void LoadCurrentValues()
    {
        // Pre-fill key fields if key exists
        if (_state.HasKey)
        {
            KeyBox.Password = _state.ApiKey;
            DisconnectBtn.Visibility = Visibility.Visible;
        }

        // Refresh interval
        var interval = _state.RefreshInterval;
        foreach (ComboBoxItem item in RefreshCombo.Items)
            if (int.Parse((string)item.Tag!) == interval)
                item.IsSelected = true;

        ShowRtCheck.IsChecked  = _state.ShowResponseTime;
        ShowUptCheck.IsChecked = _state.ShowUptime;
        StartupCheck.IsChecked = IsStartupEnabled();
    }

    // ── Show/hide key ─────────────────────────────────────────────────────────

    private void ShowHide_Click(object sender, RoutedEventArgs e)
    {
        _showingKey = !_showingKey;
        if (_showingKey)
        {
            KeyBoxVisible.Text = KeyBox.Password;
            KeyBox.Visibility        = Visibility.Collapsed;
            KeyBoxVisible.Visibility = Visibility.Visible;
            ShowHideBtn.Content      = "Hide";
        }
        else
        {
            KeyBox.Password          = KeyBoxVisible.Text;
            KeyBoxVisible.Visibility = Visibility.Collapsed;
            KeyBox.Visibility        = Visibility.Visible;
            ShowHideBtn.Content      = "Show";
        }
    }

    private string CurrentKey => _showingKey ? KeyBoxVisible.Text.Trim() : KeyBox.Password.Trim();

    // ── Validate ──────────────────────────────────────────────────────────────

    private async void Validate_Click(object sender, RoutedEventArgs e)
    {
        var key = CurrentKey;
        if (string.IsNullOrWhiteSpace(key)) return;

        SetValidating(true);
        try
        {
            var valid = await ApiClient.Instance.ValidateKeyAsync(key);
            ShowValidation(valid ? "✓ Valid" : "✗ Invalid key", valid ? Colors.LimeGreen : Colors.OrangeRed);
        }
        catch (Exception ex)
        {
            ShowValidation($"⚠ {ex.Message}", Colors.Orange);
        }
        finally
        {
            SetValidating(false);
        }
    }

    // ── Save ─────────────────────────────────────────────────────────────────

    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        var key = CurrentKey;
        if (string.IsNullOrWhiteSpace(key)) return;

        SaveBtn.IsEnabled = false;
        SaveError.Visibility = Visibility.Collapsed;

        try
        {
            var ok = await _state.SaveKeyAsync(key);
            if (ok)
            {
                ShowValidation("✓ Saved & connected!", Colors.LimeGreen);
                DisconnectBtn.Visibility = Visibility.Visible;
            }
            else
            {
                ShowSaveError("Invalid API key — please check and try again.");
            }
        }
        catch (Exception ex)
        {
            ShowSaveError(ex.Message);
        }
        finally
        {
            SaveBtn.IsEnabled = true;
        }
    }

    // ── Disconnect ────────────────────────────────────────────────────────────

    private void Disconnect_Click(object sender, RoutedEventArgs e)
    {
        _state.ClearKey();
        KeyBox.Password          = string.Empty;
        KeyBoxVisible.Text       = string.Empty;
        DisconnectBtn.Visibility = Visibility.Collapsed;
        ValidationResult.Visibility = Visibility.Collapsed;
    }

    // ── Preferences ───────────────────────────────────────────────────────────

    private void RefreshCombo_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (RefreshCombo.SelectedItem is ComboBoxItem item)
            _state.RefreshInterval = int.Parse((string)item.Tag!);
    }

    private void Prefs_Changed(object sender, RoutedEventArgs e)
    {
        _state.ShowResponseTime = ShowRtCheck.IsChecked == true;
        _state.ShowUptime       = ShowUptCheck.IsChecked == true;
    }

    // ── Startup ───────────────────────────────────────────────────────────────

    private void Startup_Changed(object sender, RoutedEventArgs e)
    {
        var enable = StartupCheck.IsChecked == true;
        SetStartup(enable);
    }

    private static bool IsStartupEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run");
        return key?.GetValue("Valpero") != null;
    }

    private static void SetStartup(bool enable)
    {
        using var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
        if (key == null) return;
        if (enable)
        {
            var exe = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName ?? "";
            key.SetValue("Valpero", $"\"{exe}\"");
        }
        else
        {
            key.DeleteValue("Valpero", throwOnMissingValue: false);
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void SetValidating(bool busy)
    {
        ValidationProgress.Visibility = busy ? Visibility.Visible : Visibility.Collapsed;
        ValidationResult.Visibility   = Visibility.Collapsed;
        ValidateBtn.IsEnabled         = !busy;
    }

    private void ShowValidation(string text, Color color)
    {
        ValidationResult.Text       = text;
        ValidationResult.Foreground = new SolidColorBrush(color);
        ValidationResult.Visibility = Visibility.Visible;
    }

    private void ShowSaveError(string text)
    {
        SaveError.Text       = text;
        SaveError.Visibility = Visibility.Visible;
    }

    private void GetKey_Click(object sender, RoutedEventArgs e) =>
        App.OpenUrl("https://valpero.com/dashboard/settings");

    private void Website_Click(object sender, RoutedEventArgs e) =>
        App.OpenUrl("https://valpero.com");

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
