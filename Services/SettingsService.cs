using System;
using System.IO;
using System.Text.Json;

namespace DiscordVocalOverlay.Services;

public class SettingsService
{
    private const string SettingsFileName = "settings.json";
    private readonly string _settingsFilePath;

    public SettingsService()
    {
        var appDataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DiscordVocalOverlay"
        );
        
        // Ensure folder exists
        if (!Directory.Exists(appDataFolder))
        {
            Directory.CreateDirectory(appDataFolder);
        }

        _settingsFilePath = Path.Combine(appDataFolder, SettingsFileName);
    }

    public string? GetRecordingsFolder()
    {
        if (!File.Exists(_settingsFilePath))
        {
            return null; // Return null if settings file doesn't exist
        }

        try
        {
            var json = File.ReadAllText(_settingsFilePath);
            var settings = JsonSerializer.Deserialize<SettingsData>(json);
            return settings?.RecordingsFolder;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load settings: {ex.Message}");
            return null;
        }
    }

    public void SaveRecordingsFolder(string folderPath)
    {
        if (string.IsNullOrEmpty(folderPath))
        {
            return;
        }

        try
        {
            var settings = new SettingsData
            {
                RecordingsFolder = folderPath
            };

            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_settingsFilePath, json);
            
            System.Diagnostics.Debug.WriteLine($"Settings saved: RecordingsFolder={folderPath}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to save settings: {ex.Message}");
        }
    }

    private class SettingsData
    {
        public string? RecordingsFolder { get; set; }
    }
}
