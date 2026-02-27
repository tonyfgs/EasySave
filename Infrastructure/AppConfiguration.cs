using System.Text.Json;
using Application.Ports;
using Shared;

namespace Infrastructure;

public class AppConfiguration : ILanguageConfig, IEncryptionConfig, IBusinessSoftwareConfig, ILargeFileConfig, IPriorityFileConfig, ILogModeConfig
{
    private readonly string _configPath;
    private readonly string _logDirectory;
    private Language _language;
    private LogFormat _logFormat;
    private List<string> _encryptedExtensions;
    private string _encryptionKey;
    private string _businessSoftwareName;
    private bool _detectionEnabled;
    private long _largeFileSizeThresholdKb;
    private List<string> _priorityExtensions;
    private LogMode _logMode;
    private string _centralizedServerUrl;

    public AppConfiguration(string configPath, string logDirectory)
    {
        _configPath = configPath;
        _logDirectory = logDirectory;
        _language = Language.EN;
        _logFormat = LogFormat.JSON;
        _encryptedExtensions = new List<string>();
        _encryptionKey = string.Empty;
        _businessSoftwareName = string.Empty;
        _detectionEnabled = false;
        _largeFileSizeThresholdKb = 0;
        _priorityExtensions = new List<string>();
        _logMode = LogMode.LocalOnly;
        _centralizedServerUrl = "http://localhost:5050";
        Load();
    }

    public Language GetLanguage() => _language;

    public void SetLanguage(Language lang) => _language = lang;

    public LogFormat GetLogFormat() => _logFormat;

    public void SetLogFormat(LogFormat format) => _logFormat = format;

    public string GetLogDirectory() => _logDirectory;

    public IReadOnlyList<string> GetEncryptedExtensions() => _encryptedExtensions.AsReadOnly();

    public void SetEncryptedExtensions(IReadOnlyList<string> extensions) =>
        _encryptedExtensions = new List<string>(extensions);

    public string GetEncryptionKey() => _encryptionKey;

    public void SetEncryptionKey(string key) => _encryptionKey = key;

    public string GetBusinessSoftwareName() => _businessSoftwareName;

    public void SetBusinessSoftwareName(string name) => _businessSoftwareName = name;

    public bool IsDetectionEnabled() => _detectionEnabled;

    public void SetDetectionEnabled(bool enabled) => _detectionEnabled = enabled;

    public long GetLargeFileSizeThresholdKb() => _largeFileSizeThresholdKb;

    public void SetLargeFileSizeThresholdKb(long thresholdKb) => _largeFileSizeThresholdKb = thresholdKb;

    public IReadOnlyList<string> GetPriorityExtensions() => _priorityExtensions.AsReadOnly();

    public void SetPriorityExtensions(IReadOnlyList<string> extensions) =>
        _priorityExtensions = new List<string>(extensions);

    public LogMode GetLogMode() => _logMode;

    public void SetLogMode(LogMode mode) => _logMode = mode;

    public string GetCentralizedServerUrl() => _centralizedServerUrl;

    public void SetCentralizedServerUrl(string url) => _centralizedServerUrl = url;

    public void Save()
    {
        var directory = Path.GetDirectoryName(_configPath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        var data = new ConfigData
        {
            Language = _language,
            LogFormat = _logFormat,
            EncryptedExtensions = _encryptedExtensions,
            EncryptionKey = _encryptionKey,
            BusinessSoftwareName = _businessSoftwareName,
            DetectionEnabled = _detectionEnabled,
            LargeFileSizeThresholdKb = _largeFileSizeThresholdKb,
            PriorityExtensions = _priorityExtensions,
            LogMode = _logMode,
            CentralizedServerUrl = _centralizedServerUrl
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
        _encryptedExtensions = new List<string>(data.EncryptedExtensions);
        _encryptionKey = data.EncryptionKey;
        _businessSoftwareName = data.BusinessSoftwareName;
        _detectionEnabled = data.DetectionEnabled;
        _largeFileSizeThresholdKb = data.LargeFileSizeThresholdKb;
        _priorityExtensions = new List<string>(data.PriorityExtensions);
        _logMode = data.LogMode;
        _centralizedServerUrl = data.CentralizedServerUrl;
    }

    private class ConfigData
    {
        public Language Language { get; set; } = Language.EN;
        public LogFormat LogFormat { get; set; } = LogFormat.JSON;
        public List<string> EncryptedExtensions { get; set; } = new();
        public string EncryptionKey { get; set; } = string.Empty;
        public string BusinessSoftwareName { get; set; } = string.Empty;
        public bool DetectionEnabled { get; set; }
        public long LargeFileSizeThresholdKb { get; set; }
        public List<string> PriorityExtensions { get; set; } = new();
        public LogMode LogMode { get; set; } = LogMode.LocalOnly;
        public string CentralizedServerUrl { get; set; } = "http://localhost:5050";
    }
}
