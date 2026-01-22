using System;
using System.IO;

namespace DiscordVocalOverlay.Services;

public class RecordingStorageService
{
    private readonly SettingsService _settingsService;
    private string _outputDirectory;

    public RecordingStorageService(SettingsService settingsService)
    {
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        
        // Load recordings folder from settings or use default
        var savedFolder = _settingsService.GetRecordingsFolder();
        _outputDirectory = !string.IsNullOrEmpty(savedFolder) && Directory.Exists(savedFolder)
            ? savedFolder
            : Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "DiscordVocalOverlay",
                "Vocals"
            );
        
        EnsureFolder();
    }

    public string GetOutputDirectory()
    {
        return _outputDirectory;
    }

    public string GetRecordingsFolder()
    {
        return _outputDirectory;
    }

    public void SetRecordingsFolder(string folderPath)
    {
        if (!string.IsNullOrEmpty(folderPath) && Directory.Exists(folderPath))
        {
            _outputDirectory = folderPath;
            _settingsService.SaveRecordingsFolder(folderPath);
            EnsureFolder();
        }
    }

    public void EnsureFolder()
    {
        if (!Directory.Exists(_outputDirectory))
        {
            Directory.CreateDirectory(_outputDirectory);
        }
    }

    public string CreateNewRecordingPath()
    {
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        return Path.Combine(_outputDirectory, $"vocal_{timestamp}.wav");
    }

    public void Delete(string filePath)
    {
        if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
        {
            try
            {
                File.Delete(filePath);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to delete file: {ex.Message}");
            }
        }
    }

    public bool FileExists(string filePath)
    {
        return !string.IsNullOrEmpty(filePath) && File.Exists(filePath);
    }
}
