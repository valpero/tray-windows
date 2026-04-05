using System.ComponentModel;
using System.Runtime.CompilerServices;
using ValperoTray.Models;
using Monitor = ValperoTray.Models.Monitor;

namespace ValperoTray.Services;

public sealed class AppState : INotifyPropertyChanged
{
    public static readonly AppState Instance = new();
    private AppState()
    {
        // Load persisted API key from Credential Manager
        var saved = CredentialManager.Load();
        if (!string.IsNullOrWhiteSpace(saved))
        {
            _apiKey = saved;
            ApiClient.Instance.ApiKey = saved;
        }

        _refreshInterval = Settings.Default.RefreshInterval > 0
            ? Settings.Default.RefreshInterval : 60;

        // Start auto-refresh timer
        _timer = new System.Timers.Timer(RefreshInterval * 1000);
        _timer.Elapsed += async (_, _) => await RefreshAsync();
        if (HasKey) { _timer.Start(); _ = RefreshAsync(); }
    }

    // ── Published state ───────────────────────────────────────────────────────

    private List<Monitor> _monitors = [];
    public List<Monitor> Monitors
    {
        get => _monitors;
        set { _monitors = value; OnPropertyChanged(); OnPropertyChanged(nameof(DownCount)); OnPropertyChanged(nameof(StatusLabel)); }
    }

    private List<Incident> _incidents = [];
    public List<Incident> Incidents
    {
        get => _incidents;
        set { _incidents = value; OnPropertyChanged(); OnPropertyChanged(nameof(OpenIncidents)); }
    }

    private List<ServerAgent> _agents = [];
    public List<ServerAgent> Agents { get => _agents; set { _agents = value; OnPropertyChanged(); } }

    private List<Heartbeat> _heartbeats = [];
    public List<Heartbeat> Heartbeats { get => _heartbeats; set { _heartbeats = value; OnPropertyChanged(); } }

    private bool _isLoading;
    public bool IsLoading { get => _isLoading; set { _isLoading = value; OnPropertyChanged(); OnPropertyChanged(nameof(StatusLabel)); } }

    private DateTime? _lastRefresh;
    public DateTime? LastRefresh { get => _lastRefresh; set { _lastRefresh = value; OnPropertyChanged(); } }

    private string? _error;
    public string? Error { get => _error; set { _error = value; OnPropertyChanged(); } }

    private string _apiKey = string.Empty;
    public string ApiKey
    {
        get => _apiKey;
        private set { _apiKey = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasKey)); OnPropertyChanged(nameof(StatusLabel)); }
    }

    // ── Computed ──────────────────────────────────────────────────────────────

    public bool HasKey => !string.IsNullOrWhiteSpace(ApiKey);
    public int DownCount => Monitors.Count(m => !m.IsUp);
    public List<Incident> OpenIncidents => Incidents.Where(i => !i.IsResolved).ToList();

    public string StatusLabel
    {
        get
        {
            if (!HasKey)    return "No API key";
            if (IsLoading && Monitors.Count == 0) return "Loading…";
            if (DownCount > 0) return $"{DownCount} monitor{(DownCount > 1 ? "s" : "")} down";
            return $"All {Monitors.Count} monitors up";
        }
    }

    // ── Settings ──────────────────────────────────────────────────────────────

    private int _refreshInterval = 60;
    public int RefreshInterval
    {
        get => _refreshInterval;
        set
        {
            _refreshInterval = value;
            _timer.Interval = value * 1000;
            Settings.Default.RefreshInterval = value;
            Settings.Default.Save();
            OnPropertyChanged();
        }
    }

    public bool ShowResponseTime
    {
        get => Settings.Default.ShowResponseTime;
        set { Settings.Default.ShowResponseTime = value; Settings.Default.Save(); OnPropertyChanged(); }
    }

    public bool ShowUptime
    {
        get => Settings.Default.ShowUptime;
        set { Settings.Default.ShowUptime = value; Settings.Default.Save(); OnPropertyChanged(); }
    }

    // ── Timer ─────────────────────────────────────────────────────────────────

    private readonly System.Timers.Timer _timer;

    // ── Refresh ───────────────────────────────────────────────────────────────

    public event Action? StateChanged;

    public async Task RefreshAsync()
    {
        if (!HasKey) return;

        await SetAsync(() => IsLoading = true);
        await SetAsync(() => Error = null);

        try
        {
            var monTask  = ApiClient.Instance.FetchMonitorsAsync();
            var incTask  = ApiClient.Instance.FetchIncidentsAsync();
            var agtTask  = ApiClient.Instance.FetchAgentsAsync();
            var hbTask   = ApiClient.Instance.FetchHeartbeatsAsync();

            await Task.WhenAll(monTask, incTask, agtTask, hbTask).ConfigureAwait(false);

            await SetAsync(() =>
            {
                Monitors   = monTask.Result;
                Incidents  = incTask.Result;
                Agents     = agtTask.Result;
                Heartbeats = hbTask.Result;
                LastRefresh = DateTime.Now;
            });
        }
        catch (UnauthorizedAccessException)
        {
            await SetAsync(() => { Error = "Invalid API key"; ApiKey = string.Empty; });
        }
        catch (Exception ex)
        {
            await SetAsync(() => Error = ex.Message);
        }
        finally
        {
            await SetAsync(() => IsLoading = false);
        }
    }

    private Task SetAsync(Action action)
    {
        return App.Current.Dispatcher.InvokeAsync(action).Task;
    }

    // ── Key management ────────────────────────────────────────────────────────

    public async Task<bool> SaveKeyAsync(string key)
    {
        var valid = await ApiClient.Instance.ValidateKeyAsync(key).ConfigureAwait(false);
        if (!valid) return false;

        CredentialManager.Save(key);
        await SetAsync(() =>
        {
            ApiKey = key;
            ApiClient.Instance.ApiKey = key;
        });
        _timer.Start();
        _ = RefreshAsync();
        return true;
    }

    public void ClearKey()
    {
        CredentialManager.Delete();
        ApiKey = string.Empty;
        ApiClient.Instance.ApiKey = string.Empty;
        Monitors = []; Incidents = []; Agents = []; Heartbeats = [];
        _timer.Stop();
    }

    // ── INotifyPropertyChanged ────────────────────────────────────────────────

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        StateChanged?.Invoke();
    }
}
