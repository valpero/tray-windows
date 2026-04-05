using System.IO;
using System.Text.Json;

namespace ValperoTray.Services;

/// <summary>Lightweight app settings stored in %APPDATA%\Valpero\settings.json</summary>
public sealed class Settings
{
    private static Settings? _instance;
    public static Settings Default => _instance ??= Load();

    private static readonly string _dir  = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Valpero");
    private static readonly string _file = Path.Combine(_dir, "settings.json");

    public int  RefreshInterval  { get; set; } = 60;
    public bool ShowResponseTime { get; set; } = true;
    public bool ShowUptime       { get; set; } = true;
    public bool LaunchAtStartup  { get; set; } = false;

    public void Save()
    {
        Directory.CreateDirectory(_dir);
        File.WriteAllText(_file, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
    }

    private static Settings Load()
    {
        try
        {
            if (File.Exists(_file))
                return JsonSerializer.Deserialize<Settings>(File.ReadAllText(_file)) ?? new Settings();
        }
        catch { /* use defaults */ }
        return new Settings();
    }
}
