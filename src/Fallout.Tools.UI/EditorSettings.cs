using System.Text.Json;

namespace Fallout.Tools.UI;

internal sealed class EditorSettings
{
    private const string SettingsDirectoryName = "Fallout.Tools.UI";
    private const string SettingsFileName = "editor-settings.json";

    public double Zoom { get; set; } = 1.0;
    public Dictionary<string, string> LastDirectories { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    private static JsonSerializerOptions JsonOptions { get; } = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public static EditorSettings Load()
    {
        try
        {
            string path = GetSettingsPath();
            if (!File.Exists(path))
            {
                return new EditorSettings();
            }

            string json = File.ReadAllText(path);
            EditorSettings? settings = JsonSerializer.Deserialize<EditorSettings>(json, JsonOptions);
            if (settings is null)
            {
                return new EditorSettings();
            }

            settings.Zoom = ClampZoom(settings.Zoom);
            settings.LastDirectories ??= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            return settings;
        }
        catch
        {
            return new EditorSettings();
        }
    }

    public void Save()
    {
        string path = GetSettingsPath();
        Directory.CreateDirectory(Path.GetDirectoryName(path) ?? Directory.GetCurrentDirectory());
        string json = JsonSerializer.Serialize(this, JsonOptions);
        File.WriteAllText(path, json);
    }

    public string? GetLastDirectory(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return null;
        }

        return LastDirectories.TryGetValue(key, out string? directory) ? directory : null;
    }

    public void SetLastDirectory(string key, string directory)
    {
        if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(directory))
        {
            return;
        }

        LastDirectories[key] = directory;
    }

    private static string GetSettingsPath()
    {
        string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        if (string.IsNullOrWhiteSpace(appData))
        {
            appData = Directory.GetCurrentDirectory();
        }

        return Path.Combine(appData, SettingsDirectoryName, SettingsFileName);
    }

    private static double ClampZoom(double zoom)
    {
        if (double.IsNaN(zoom) || double.IsInfinity(zoom))
        {
            return 1.0;
        }

        return Math.Clamp(zoom, 0.25, 8.0);
    }
}
