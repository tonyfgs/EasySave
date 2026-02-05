using System.Text.Json;
using Application.Ports;
using Shared;

namespace Infrastructure;

public class AppConfiguration : ILanguageConfig
{
    private readonly string _configPath;
    private readonly string _logDirectory;
    private Language _language;
    private LogFormat _logFormat;

    public AppConfiguration(string configPath, string logDirectory)
    {
        _configPath = configPath;
        _logDirectory = logDirectory;
        _language = Language.EN;
        _logFormat = LogFormat.JSON;
        Load();
    }

    public Language GetLanguage() => _language;

    public void SetLanguage(Language lang) => _language = lang;

    public LogFormat GetLogFormat() => _logFormat;

    public void SetLogFormat(LogFormat format) => _logFormat = format;

    public string GetLogDirectory() => _logDirectory;

    public void Save()
    {
        var directory = Path.GetDirectoryName(_configPath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        var data = new ConfigData
        {
            Language = _language,
            LogFormat = _logFormat
        };

        var options = new JsonSerializerOptions { WriteIndented = true };
        var json = JsonSerializer.Serialize(data, options);
        File.WriteAllText(_configPath, json);
    }

    private void Load()
    {
        if (!File.Exists(_configPath))
            return;

        var json = File.ReadAllText(_configPath);
        if (string.IsNullOrWhiteSpace(json))
            return;

        var data = JsonSerializer.Deserialize<ConfigData>(json);
        if (data is null)
            return;

        _language = data.Language;
        _logFormat = data.LogFormat;
    }

    private class ConfigData
    {
        public Language Language { get; set; } = Language.EN;
        public LogFormat LogFormat { get; set; } = LogFormat.JSON;
    }
}
