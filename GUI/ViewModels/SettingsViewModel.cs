using System.Collections.ObjectModel;
using System.Windows.Input;
using Application.Services;
using GUI.Helpers;
using GUI.Services;
using Infrastructure;
using Shared;

namespace GUI.ViewModels;

public class SettingsViewModel : ObservableObject
{
    private readonly AppConfiguration _appConfig;
    private readonly LanguageApplicationService _languageService;
    private readonly LocalizationService _localization;

    private Language _selectedLanguage;
    private LogFormat _selectedLogFormat;
    private string _encryptionKey = string.Empty;
    private bool _detectionEnabled;
    private string _largeFileSizeThresholdKb = "0";

    public Language SelectedLanguage
    {
        get => _selectedLanguage;
        set
        {
            if (SetProperty(ref _selectedLanguage, value))
            {
                // Apply language change immediately
                _appConfig.SetLanguage(value);
                _appConfig.Save();
                _languageService.ChangeLanguage(value);
                // Refresh all UI translations
                _localization.RefreshTranslations();
            }
        }
    }

    public LogFormat SelectedLogFormat
    {
        get => _selectedLogFormat;
        set => SetProperty(ref _selectedLogFormat, value);
    }

    public string EncryptionKey
    {
        get => _encryptionKey;
        set => SetProperty(ref _encryptionKey, value);
    }

    public bool DetectionEnabled
    {
        get => _detectionEnabled;
        set => SetProperty(ref _detectionEnabled, value);
    }

    public string LargeFileSizeThresholdKb
    {
        get => _largeFileSizeThresholdKb;
        set
        {
            var normalized = string.IsNullOrWhiteSpace(value) ? "0" : value;
            if (long.TryParse(normalized, out var parsed) && parsed >= 0)
                SetProperty(ref _largeFileSizeThresholdKb, parsed.ToString());
        }
    }

    public LocalizationService Localization => _localization;

    // Chip input collections
    public ObservableCollection<string> SelectedExtensions { get; } = new();
    public ObservableCollection<string> SelectedBusinessSoftware { get; } = new();
    public ObservableCollection<string> SelectedPriorityExtensions { get; } = new();

    public List<string> AvailableExtensions { get; } = new()
    {
        ".txt", ".pdf", ".docx", ".xlsx", ".pptx", ".csv",
        ".html", ".xml", ".json", ".zip", ".rar", ".7z",
        ".png", ".jpg", ".mp4"
    };

    public List<string> AvailableBusinessSoftware { get; } = new()
    {
        "Calculator", "Notepad", "Word", "Excel", "PowerPoint",
        "Outlook", "Teams", "Slack", "Chrome", "Firefox"
    };

    // Button colors for language
    public string EnglishButtonColor => _selectedLanguage == Language.EN ? "#4CAF50" : "#9E9E9E";
    public string FrenchButtonColor => _selectedLanguage == Language.FR ? "#4CAF50" : "#9E9E9E";

    // Button colors for log format
    public string JsonButtonColor => _selectedLogFormat == LogFormat.JSON ? "#4CAF50" : "#9E9E9E";
    public string XmlButtonColor => _selectedLogFormat == LogFormat.XML ? "#4CAF50" : "#9E9E9E";

    public string CurrentFormatText => $"✓ {_selectedLogFormat} - {(_selectedLogFormat == LogFormat.JSON ? "JavaScript Object Notation" : "eXtensible Markup Language")}";

    public ICommand SelectEnglishCommand { get; }
    public ICommand SelectFrenchCommand { get; }
    public ICommand SelectJsonCommand { get; }
    public ICommand SelectXmlCommand { get; }
    public ICommand LoadSettingsCommand { get; }
    public ICommand SaveSettingsCommand { get; }

    public SettingsViewModel()
    {
        _appConfig = ServiceLocator.AppConfiguration;
        _languageService = ServiceLocator.LanguageApplicationService;
        _localization = ServiceLocator.LocalizationService;

        SelectEnglishCommand = new RelayCommand(SelectEnglish);
        SelectFrenchCommand = new RelayCommand(SelectFrench);
        SelectJsonCommand = new RelayCommand(SelectJson);
        SelectXmlCommand = new RelayCommand(SelectXml);
        LoadSettingsCommand = new RelayCommand(LoadSettings);
        SaveSettingsCommand = new RelayCommand(SaveSettings);

        LoadSettings();
    }

    private void SelectEnglish()
    {
        SelectedLanguage = Language.EN;
        UpdateLanguageButtons();
    }

    private void SelectFrench()
    {
        SelectedLanguage = Language.FR;
        UpdateLanguageButtons();
    }

    private void SelectJson()
    {
        SelectedLogFormat = LogFormat.JSON;
        _appConfig.SetLogFormat(LogFormat.JSON);
        _appConfig.Save();
        UpdateLogFormatButtons();
    }

    private void SelectXml()
    {
        SelectedLogFormat = LogFormat.XML;
        _appConfig.SetLogFormat(LogFormat.XML);
        _appConfig.Save();
        UpdateLogFormatButtons();
    }

    private void UpdateLanguageButtons()
    {
        OnPropertyChanged(nameof(EnglishButtonColor));
        OnPropertyChanged(nameof(FrenchButtonColor));
    }

    private void UpdateLogFormatButtons()
    {
        OnPropertyChanged(nameof(JsonButtonColor));
        OnPropertyChanged(nameof(XmlButtonColor));
        OnPropertyChanged(nameof(CurrentFormatText));
    }

    private void LoadSettings()
    {
        _selectedLanguage = _appConfig.GetLanguage();
        _selectedLogFormat = _appConfig.GetLogFormat();

        // Load extensions into ObservableCollection
        SelectedExtensions.Clear();
        foreach (var ext in _appConfig.GetEncryptedExtensions())
            SelectedExtensions.Add(ext);

        EncryptionKey = _appConfig.GetEncryptionKey();

        // Load priority extensions into ObservableCollection
        SelectedPriorityExtensions.Clear();
        foreach (var ext in _appConfig.GetPriorityExtensions())
            SelectedPriorityExtensions.Add(ext);

        // Load business software name into ObservableCollection
        SelectedBusinessSoftware.Clear();
        var softwareName = _appConfig.GetBusinessSoftwareName();
        if (!string.IsNullOrWhiteSpace(softwareName))
            SelectedBusinessSoftware.Add(softwareName);

        DetectionEnabled = _appConfig.IsDetectionEnabled();

        _largeFileSizeThresholdKb = _appConfig.GetLargeFileSizeThresholdKb().ToString();
        OnPropertyChanged(nameof(LargeFileSizeThresholdKb));

        // Update UI
        UpdateLanguageButtons();
        UpdateLogFormatButtons();
    }

    private void SaveSettings()
    {
        // Save language
        _appConfig.SetLanguage(SelectedLanguage);

        // Save log format
        _appConfig.SetLogFormat(SelectedLogFormat);

        // Save encrypted extensions from ObservableCollection
        _appConfig.SetEncryptedExtensions(SelectedExtensions.ToList());

        // Save encryption key
        _appConfig.SetEncryptionKey(EncryptionKey);

        // Save priority extensions from ObservableCollection
        _appConfig.SetPriorityExtensions(SelectedPriorityExtensions.ToList());

        // Save business software settings (single name from collection)
        var softwareName = SelectedBusinessSoftware.FirstOrDefault() ?? string.Empty;
        _appConfig.SetBusinessSoftwareName(softwareName);
        _appConfig.SetDetectionEnabled(DetectionEnabled);

        // Save large file threshold
        if (long.TryParse(_largeFileSizeThresholdKb, out var threshold))
            _appConfig.SetLargeFileSizeThresholdKb(threshold);

        // Persist to file
        _appConfig.Save();
    }
}
