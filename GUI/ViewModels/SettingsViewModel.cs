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
    private string _encryptedExtensions = string.Empty;
    private string _encryptionKey = string.Empty;
    private string _businessSoftwareName = string.Empty;
    private bool _detectionEnabled;

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

    public string EncryptedExtensions
    {
        get => _encryptedExtensions;
        set => SetProperty(ref _encryptedExtensions, value);
    }

    public string EncryptionKey
    {
        get => _encryptionKey;
        set => SetProperty(ref _encryptionKey, value);
    }

    public string BusinessSoftwareName
    {
        get => _businessSoftwareName;
        set => SetProperty(ref _businessSoftwareName, value);
    }

    public bool DetectionEnabled
    {
        get => _detectionEnabled;
        set => SetProperty(ref _detectionEnabled, value);
    }

    public LocalizationService Localization => _localization;

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

        var extensions = _appConfig.GetEncryptedExtensions();
        EncryptedExtensions = string.Join(", ", extensions);

        EncryptionKey = _appConfig.GetEncryptionKey();
        BusinessSoftwareName = _appConfig.GetBusinessSoftwareName();
        DetectionEnabled = _appConfig.IsDetectionEnabled();

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

        // Save encrypted extensions
        var extensions = EncryptedExtensions
            .Split(',')
            .Select(e => e.Trim())
            .Where(e => !string.IsNullOrWhiteSpace(e))
            .ToList();
        _appConfig.SetEncryptedExtensions(extensions);

        // Save encryption key
        _appConfig.SetEncryptionKey(EncryptionKey);

        // Save business software settings
        _appConfig.SetBusinessSoftwareName(BusinessSoftwareName);
        _appConfig.SetDetectionEnabled(DetectionEnabled);

        // Persist to file
        _appConfig.Save();
    }
}
