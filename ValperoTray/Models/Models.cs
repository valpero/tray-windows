using System.Text.Json.Serialization;

namespace ValperoTray.Models;

// ── Monitor ──────────────────────────────────────────────────────────────────

public record SitesResponse(
    [property: JsonPropertyName("sites")] List<Monitor> Sites,
    [property: JsonPropertyName("total")]  int Total
);

public record Monitor(
    [property: JsonPropertyName("id")]                 int    Id,
    [property: JsonPropertyName("name")]               string? Name,
    [property: JsonPropertyName("url")]                string  Url,
    [property: JsonPropertyName("check_type")]         string  CheckType,
    [property: JsonPropertyName("is_up")]              bool   IsUp,
    [property: JsonPropertyName("is_active")]          bool   IsActive,
    [property: JsonPropertyName("uptime_30d")]         double? Uptime30d,
    [property: JsonPropertyName("uptime_24h")]         double? Uptime24h,
    [property: JsonPropertyName("last_response_time")] int?   LastResponseTime,
    [property: JsonPropertyName("last_check")]         string? LastCheck
)
{
    public string DisplayName => Name ?? Url;

    public string DashboardUrl => $"https://valpero.com/dashboard/monitors/{Id}";

    public string ResponseTimeLabel => LastResponseTime switch
    {
        null          => "",
        >= 1000       => $"{LastResponseTime / 1000.0:F1}s",
        _             => $"{LastResponseTime}ms"
    };

    public string UptimeLabel => Uptime24h.HasValue ? $"{Uptime24h:F1}%" : "";
}

// ── Incident ─────────────────────────────────────────────────────────────────

public record Incident(
    [property: JsonPropertyName("id")]               int    Id,
    [property: JsonPropertyName("site_id")]          int    SiteId,
    [property: JsonPropertyName("site_name")]        string SiteName,
    [property: JsonPropertyName("site_url")]         string SiteUrl,
    [property: JsonPropertyName("started_at")]       string StartedAt,
    [property: JsonPropertyName("resolved_at")]      string? ResolvedAt,
    [property: JsonPropertyName("duration_seconds")] int?   DurationSeconds,
    [property: JsonPropertyName("cause")]            string? Cause,
    [property: JsonPropertyName("is_resolved")]      bool   IsResolved
)
{
    public string DurationLabel => DurationSeconds switch
    {
        null      => "",
        < 60      => $"{DurationSeconds}s",
        < 3600    => $"{DurationSeconds / 60}m",
        _         => $"{DurationSeconds / 3600}h {DurationSeconds % 3600 / 60}m"
    };
}

// ── Server Agent ──────────────────────────────────────────────────────────────

public record ServerAgent(
    [property: JsonPropertyName("id")]          string  Id,
    [property: JsonPropertyName("name")]        string? Name,
    [property: JsonPropertyName("hostname")]    string? Hostname,
    [property: JsonPropertyName("os")]          string? Os,
    [property: JsonPropertyName("arch")]        string? Arch,
    [property: JsonPropertyName("is_online")]   bool    IsOnline,
    [property: JsonPropertyName("cpu_pct")]     double? CpuPct,
    [property: JsonPropertyName("ram_used_mb")] int?    RamUsedMb,
    [property: JsonPropertyName("ram_total_mb")]int?    RamTotalMb,
    [property: JsonPropertyName("last_seen_at")]string? LastSeenAt
)
{
    public string DisplayName => Name ?? Hostname ?? Id;

    public double? RamPct => (RamUsedMb.HasValue && RamTotalMb is > 0)
        ? (double)RamUsedMb.Value / RamTotalMb.Value * 100
        : null;

    public string DashboardUrl => $"https://valpero.com/dashboard/servers/{Id}";
}

// ── Heartbeat ─────────────────────────────────────────────────────────────────

public record Heartbeat(
    [property: JsonPropertyName("id")]           int    Id,
    [property: JsonPropertyName("name")]         string Name,
    [property: JsonPropertyName("status")]       string Status,
    [property: JsonPropertyName("interval")]     int    Interval,
    [property: JsonPropertyName("grace_period")] int    GracePeriod,
    [property: JsonPropertyName("last_ping_at")] string? LastPingAt,
    [property: JsonPropertyName("is_active")]    bool   IsActive
)
{
    public bool IsUp  => Status == "up";
    public bool IsNew => Status == "new";
}
