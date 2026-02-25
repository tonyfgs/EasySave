using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows.Input;
using Application.Ports;
using Application.Services;
using GUI.Helpers;
using GUI.Services;
using Shared;

namespace GUI.ViewModels;

public class SettingsViewModel : ObservableObject
{
    private readonly Infrastructure.AppConfiguration _appConfig;
    private readonly LanguageApplicationService _languageService;
    private readonly LocalizationService _localization;
    private readonly IProcessValidator _processValidator;
    private readonly IProcessNameResolver _processNameResolver;

    private Language _selectedLanguage;
    private LogFormat _selectedLogFormat;
    private string _encryptionKey = string.Empty;
    private bool _detectionEnabled;
    private string _largeFileSizeThresholdKb = "0";
    private string _processNotRunningWarning = string.Empty;
    private bool _isLoading;

    public Language SelectedLanguage
    {
        get => _selectedLanguage;
        set
        {
            if (SetProperty(ref _selectedLanguage, value))
            {
                _appConfig.SetLanguage(value);
                _appConfig.Save();
                _languageService.ChangeLanguage(value);
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
        set
        {
            if (SetProperty(ref _detectionEnabled, value) && !_isLoading)
            {
                _appConfig.SetDetectionEnabled(value);
                _appConfig.Save();
            }
        }
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

    public string ProcessNotRunningWarning
    {
        get => _processNotRunningWarning;
        private set => SetProperty(ref _processNotRunningWarning, value);
    }

    public LocalizationService Localization => _localization;

    // Chip input collections
    public ObservableCollection<string> SelectedExtensions { get; } = new();
    public ObservableCollection<string> SelectedBusinessSoftware { get; } = new();

    public List<string> AvailableExtensions { get; } = new()
    {
        ".txt", ".pdf", ".docx", ".xlsx", ".pptx", ".csv",
        ".html", ".xml", ".json", ".zip", ".rar", ".7z",
        ".png", ".jpg", ".mp4"
    };

    // Available items are display names only — process name resolution happens on save/load
    public List<string> AvailableBusinessSoftware { get; }

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
        _processValidator = ServiceLocator.ProcessValidator;
        _processNameResolver = ServiceLocator.ProcessNameResolver;

        AvailableBusinessSoftware = _processNameResolver.GetAvailableDisplayNames().ToList();

        SelectEnglishCommand = new RelayCommand(SelectEnglish);
        SelectFrenchCommand = new RelayCommand(SelectFrench);
        SelectJsonCommand = new RelayCommand(SelectJson);
        SelectXmlCommand = new RelayCommand(SelectXml);
        LoadSettingsCommand = new RelayCommand(LoadSettings);
        SaveSettingsCommand = new RelayCommand(SaveSettings);

        SelectedBusinessSoftware.CollectionChanged += OnBusinessSoftwareChanged;

        LoadSettings();
    }

    private void OnBusinessSoftwareChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        var displayName = SelectedBusinessSoftware.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(displayName))
        {
            if (!_isLoading)
            {
                ProcessNotRunningWarning = string.Empty;
                _appConfig.SetBusinessSoftwareName(string.Empty);
                _appConfig.Save();
            }
            return;
        }

        if (!_isLoading)
        {
            var processName = _processNameResolver.GetProcessName(displayName);
            ProcessNotRunningWarning = _processValidator.IsProcessRunning(processName)
                ? string.Empty
                : _localization.ProcessNotRunningWarning;

            _appConfig.SetBusinessSoftwareName(processName);
            _appConfig.Save();
        }
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
        _isLoading = true;
        try
        {
            _selectedLanguage = _appConfig.GetLanguage();
            _selectedLogFormat = _appConfig.GetLogFormat();

            SelectedExtensions.Clear();
            foreach (var ext in _appConfig.GetEncryptedExtensions())
                SelectedExtensions.Add(ext);

            EncryptionKey = _appConfig.GetEncryptionKey();

            // Reverse-map the stored OS process name back to its display name for the chip UI.
            // Also migrate legacy configs that stored the display name instead of the OS process name.
            SelectedBusinessSoftware.Clear();
            var storedProcessName = _appConfig.GetBusinessSoftwareName();
            if (!string.IsNullOrWhiteSpace(storedProcessName))
            {
                var displayName = _processNameResolver.GetDisplayName(storedProcessName);
                SelectedBusinessSoftware.Add(displayName);

                // If the stored value was a display name (not a real OS process name), migrate it now.
                var correctProcessName = _processNameResolver.GetProcessName(displayName);
                if (correctProcessName != storedProcessName)
                {
                    _appConfig.SetBusinessSoftwareName(correctProcessName);
                    _appConfig.Save();
                }
            }

            DetectionEnabled = _appConfig.IsDetectionEnabled();

            _largeFileSizeThresholdKb = _appConfig.GetLargeFileSizeThresholdKb().ToString();
            OnPropertyChanged(nameof(LargeFileSizeThresholdKb));

            UpdateLanguageButtons();
            UpdateLogFormatButtons();
        }
        finally
        {
            _isLoading = false;
        }
    }

    private void SaveSettings()
    {
        _appConfig.SetLanguage(SelectedLanguage);
        _appConfig.SetLogFormat(SelectedLogFormat);
        _appConfig.SetEncryptedExtensions(SelectedExtensions.ToList());
        _appConfig.SetEncryptionKey(EncryptionKey);

        // Translate the display name to the OS-specific process name before persisting
        var displayName = SelectedBusinessSoftware.FirstOrDefault() ?? string.Empty;
        _appConfig.SetBusinessSoftwareName(_processNameResolver.GetProcessName(displayName));
        _appConfig.SetDetectionEnabled(DetectionEnabled);

        if (long.TryParse(_largeFileSizeThresholdKb, out var threshold))
            _appConfig.SetLargeFileSizeThresholdKb(threshold);

        _appConfig.Save();
    }
}
