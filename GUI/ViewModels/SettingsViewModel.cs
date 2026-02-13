using System.Windows.Input;
using GUI.Helpers;
using GUI.Services;
using Infrastructure;
using Shared;

namespace GUI.ViewModels;

public class SettingsViewModel : ObservableObject
{
    private readonly AppConfiguration _appConfig;
    private LogFormat _currentFormat;
    public LocalizationService Localization { get; }

    public ICommand SelectJsonCommand { get; }
    public ICommand SelectXmlCommand { get; }

    public string JsonButtonColor => _currentFormat == LogFormat.JSON ? "#4CAF50" : "#9E9E9E";
    public string XmlButtonColor => _currentFormat == LogFormat.XML ? "#4CAF50" : "#9E9E9E";

    public string CurrentSelectionText
    {
        get
        {
            var format = _currentFormat == LogFormat.JSON ? "JSON" : "XML";
            var desc = _currentFormat == LogFormat.JSON
                ? Localization.JsonDescription
                : Localization.XmlDescription;
            return $"✓ {format} - {desc}";
        }
    }

    public SettingsViewModel()
    {
        _appConfig = ServiceLocator.AppConfiguration;
        Localization = ServiceLocator.LocalizationService;

        _currentFormat = _appConfig.GetLogFormat();

        SelectJsonCommand = new RelayCommand(SelectJson);
        SelectXmlCommand = new RelayCommand(SelectXml);
    }

    private void SelectJson()
    {
        _currentFormat = LogFormat.JSON;
        _appConfig.SetLogFormat(LogFormat.JSON);
        _appConfig.Save();
        UpdateButtonColors();
    }

    private void SelectXml()
    {
        _currentFormat = LogFormat.XML;
        _appConfig.SetLogFormat(LogFormat.XML);
        _appConfig.Save();
        UpdateButtonColors();
    }

    private void UpdateButtonColors()
    {
        OnPropertyChanged(nameof(JsonButtonColor));
        OnPropertyChanged(nameof(XmlButtonColor));
        OnPropertyChanged(nameof(CurrentSelectionText));
    }
}
