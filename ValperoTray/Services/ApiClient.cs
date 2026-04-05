using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using ValperoTray.Models;
using Monitor = ValperoTray.Models.Monitor;

namespace ValperoTray.Services;

public sealed class ApiClient
{
    public static readonly ApiClient Instance = new();
    private ApiClient() { }

    private static readonly HttpClient _http = new(new SocketsHttpHandler
    {
        PooledConnectionLifetime = TimeSpan.FromMinutes(5),
    })
    {
        BaseAddress = new Uri("https://valpero.com"),
        Timeout = TimeSpan.FromSeconds(12),
        DefaultRequestHeaders =
        {
            UserAgent = { new ProductInfoHeaderValue("ValperoTray", "1.0") },
        },
    };

    public string ApiKey { get; set; } = string.Empty;

    private static readonly JsonSerializerOptions _json = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    // ── Core request ─────────────────────────────────────────────────────────

    private async Task<T> GetAsync<T>(string path, string? overrideKey = null)
    {
        var key = overrideKey ?? ApiKey;
        if (string.IsNullOrWhiteSpace(key))
            throw new UnauthorizedAccessException("No API key set.");

        using var req = new HttpRequestMessage(HttpMethod.Get, path);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", key);

        using var resp = await _http.SendAsync(req).ConfigureAwait(false);

        if (resp.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            throw new UnauthorizedAccessException("Invalid API key.");

        resp.EnsureSuccessStatusCode();

        var stream = await resp.Content.ReadAsStreamAsync().ConfigureAwait(false);
        return JsonSerializer.Deserialize<T>(stream, _json)
               ?? throw new InvalidDataException("Empty response.");
    }

    // ── Endpoints ─────────────────────────────────────────────────────────────

    public async Task<List<Monitor>> FetchMonitorsAsync()
    {
        var r = await GetAsync<SitesResponse>("/api/sites").ConfigureAwait(false);
        return r.Sites;
    }

    public async Task<List<Incident>> FetchIncidentsAsync(int limit = 20)
    {
        return await GetAsync<List<Incident>>($"/api/incidents?limit={limit}").ConfigureAwait(false);
    }

    public async Task<List<ServerAgent>> FetchAgentsAsync()
    {
        return await GetAsync<List<ServerAgent>>("/api/agents").ConfigureAwait(false);
    }

    public async Task<List<Heartbeat>> FetchHeartbeatsAsync()
    {
        return await GetAsync<List<Heartbeat>>("/api/heartbeats").ConfigureAwait(false);
    }

    // ── Validate key ──────────────────────────────────────────────────────────

    /// Returns true = valid, false = invalid key, throws on network error.
    public async Task<bool> ValidateKeyAsync(string key)
    {
        try
        {
            await GetAsync<SitesResponse>("/api/sites", overrideKey: key).ConfigureAwait(false);
            return true;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }
}
