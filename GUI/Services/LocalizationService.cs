using System.ComponentModel;
using System.Runtime.CompilerServices;
using Application.Services;
using Shared;

namespace GUI.Services;

/// <summary>
/// Provides localized strings for the GUI with property change notifications.
/// When language changes, all UI elements bound to these properties update automatically.
/// </summary>
public class LocalizationService : INotifyPropertyChanged
{
    private readonly LanguageApplicationService _languageService;
    private readonly Dictionary<string, Dictionary<Language, string>> _translations;

    public event PropertyChangedEventHandler? PropertyChanged;

    public LocalizationService(LanguageApplicationService languageService)
    {
        _languageService = languageService;
        _translations = InitializeTranslations();
    }

    // Main navigation
    public string AppTitle => GetString("app.title");
    public string JobList => GetString("nav.job_list");
    public string CreateJob => GetString("nav.create_job");
    public string Execute => GetString("nav.execute");
    public string Settings => GetString("nav.settings");
    public string LanguageToggle => GetString("nav.language");

    // Job List View
    public string JobListTitle => GetString("joblist.title");
    public string NoJobs => GetString("joblist.no_jobs");
    public string Edit => GetString("common.edit");
    public string Delete => GetString("common.delete");

    // Create Job View
    public string CreateJobTitle => GetString("create.title");
    public string EditJobTitle => GetString("create.edit_title");
    public string JobName => GetString("create.name");
    public string SourcePath => GetString("create.source");
    public string TargetPath => GetString("create.target");
    public string BackupType => GetString("create.type");
    public string Full => GetString("create.type_full");
    public string Differential => GetString("create.type_diff");
    public string CreateButton => GetString("create.button");
    public string UpdateButton => GetString("create.update");
    public string Cancel => GetString("common.cancel");

    // Execute Job View
    public string ExecuteTitle => GetString("execute.title");
    public string ExecuteSelected => GetString("execute.selected");
    public string ExecuteAll => GetString("execute.all");
    public string Executing => GetString("execute.executing");
    public string NoJobsAvailable => GetString("execute.no_jobs");

    // Settings View
    public string SettingsTitle => GetString("settings.title");
    public string LogFormatLabel => GetString("settings.log_format");
    public string LogFormatDescription => GetString("settings.log_format_desc");
    public string JsonDescription => GetString("settings.json_desc");
    public string XmlDescription => GetString("settings.xml_desc");
    public string SettingsInfo => GetString("settings.info");

    // Common
    public string Success => GetString("common.success");
    public string Failed => GetString("common.failed");
    public string Error => GetString("common.error");
    public string Browse => GetString("common.browse");
    public string Refresh => GetString("common.refresh");

    public void ChangeLanguage()
    {
        var currentLang = _languageService.GetCurrentLanguage();
        var newLang = currentLang == Language.EN ? Language.FR : Language.EN;
        _languageService.ChangeLanguage(newLang);

        // Notify all properties changed
        OnPropertyChanged(string.Empty);
    }

    private string GetString(string key)
    {
        if (_translations.TryGetValue(key, out var translations))
        {
            var lang = _languageService.GetCurrentLanguage();
            if (translations.TryGetValue(lang, out var value))
                return value;
        }
        return $"[{key}]";
    }

    private static Dictionary<string, Dictionary<Language, string>> InitializeTranslations()
    {
        return new Dictionary<string, Dictionary<Language, string>>
        {
            ["app.title"] = new() { [Language.EN] = "EasySave", [Language.FR] = "EasySave" },

            // Navigation
            ["nav.job_list"] = new() { [Language.EN] = "📋 Job List", [Language.FR] = "📋 Liste des Tâches" },
            ["nav.create_job"] = new() { [Language.EN] = "➕ Create Job", [Language.FR] = "➕ Créer une Tâche" },
            ["nav.execute"] = new() { [Language.EN] = "▶️ Execute", [Language.FR] = "▶️ Exécuter" },
            ["nav.settings"] = new() { [Language.EN] = "⚙️ Settings", [Language.FR] = "⚙️ Paramètres" },
            ["nav.language"] = new() { [Language.EN] = "🌐 EN / FR", [Language.FR] = "🌐 EN / FR" },

            // Job List
            ["joblist.title"] = new() { [Language.EN] = "Backup Jobs", [Language.FR] = "Tâches de Sauvegarde" },
            ["joblist.no_jobs"] = new() { [Language.EN] = "No jobs found. Create one to get started.", [Language.FR] = "Aucune tâche trouvée. Créez-en une pour commencer." },

            // Create/Edit Job
            ["create.title"] = new() { [Language.EN] = "Create Backup Job", [Language.FR] = "Créer une Tâche de Sauvegarde" },
            ["create.edit_title"] = new() { [Language.EN] = "Edit Backup Job", [Language.FR] = "Modifier la Tâche de Sauvegarde" },
            ["create.name"] = new() { [Language.EN] = "Job Name", [Language.FR] = "Nom de la Tâche" },
            ["create.source"] = new() { [Language.EN] = "Source Path", [Language.FR] = "Chemin Source" },
            ["create.target"] = new() { [Language.EN] = "Target Path", [Language.FR] = "Chemin Cible" },
            ["create.type"] = new() { [Language.EN] = "Backup Type", [Language.FR] = "Type de Sauvegarde" },
            ["create.type_full"] = new() { [Language.EN] = "Full", [Language.FR] = "Complète" },
            ["create.type_diff"] = new() { [Language.EN] = "Differential", [Language.FR] = "Différentielle" },
            ["create.button"] = new() { [Language.EN] = "Create Job", [Language.FR] = "Créer la Tâche" },
            ["create.update"] = new() { [Language.EN] = "Update Job", [Language.FR] = "Mettre à Jour" },

            // Execute
            ["execute.title"] = new() { [Language.EN] = "Execute Backups", [Language.FR] = "Exécuter les Sauvegardes" },
            ["execute.selected"] = new() { [Language.EN] = "Execute Selected", [Language.FR] = "Exécuter la Sélection" },
            ["execute.all"] = new() { [Language.EN] = "Execute All", [Language.FR] = "Tout Exécuter" },
            ["execute.executing"] = new() { [Language.EN] = "Executing backups...", [Language.FR] = "Exécution des sauvegardes..." },
            ["execute.no_jobs"] = new() { [Language.EN] = "No jobs available to execute.", [Language.FR] = "Aucune tâche disponible à exécuter." },

            // Settings
            ["settings.title"] = new() { [Language.EN] = "Settings", [Language.FR] = "Paramètres" },
            ["settings.log_format"] = new() { [Language.EN] = "Log Format", [Language.FR] = "Format des Logs" },
            ["settings.log_format_desc"] = new() { [Language.EN] = "Choose the format for backup transfer logs", [Language.FR] = "Choisissez le format pour les logs de transfert de sauvegarde" },
            ["settings.json_desc"] = new() { [Language.EN] = "JSON format (default)", [Language.FR] = "Format JSON (par défaut)" },
            ["settings.xml_desc"] = new() { [Language.EN] = "XML format", [Language.FR] = "Format XML" },
            ["settings.info"] = new() { [Language.EN] = "Logs are automatically saved when backups are executed. Changes take effect immediately.", [Language.FR] = "Les logs sont automatiquement sauvegardés lors de l'exécution des sauvegardes. Les modifications prennent effet immédiatement." },

            // Common
            ["common.edit"] = new() { [Language.EN] = "Edit", [Language.FR] = "Modifier" },
            ["common.delete"] = new() { [Language.EN] = "Delete", [Language.FR] = "Supprimer" },
            ["common.cancel"] = new() { [Language.EN] = "Cancel", [Language.FR] = "Annuler" },
            ["common.success"] = new() { [Language.EN] = "Success", [Language.FR] = "Succès" },
            ["common.failed"] = new() { [Language.EN] = "Failed", [Language.FR] = "Échoué" },
            ["common.error"] = new() { [Language.EN] = "Error", [Language.FR] = "Erreur" },
            ["common.browse"] = new() { [Language.EN] = "Browse", [Language.FR] = "Parcourir" },
            ["common.refresh"] = new() { [Language.EN] = "Refresh", [Language.FR] = "Actualiser" }
        };
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
