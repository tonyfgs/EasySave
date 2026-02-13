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
    public string LanguageLabel => GetString("settings.language_label");
    public string SelectLanguage => GetString("settings.select_language");
    public string LanguageInfo => GetString("settings.language_info");
    public string LogFormatLabel => GetString("settings.log_format");
    public string SelectLogFormat => GetString("settings.select_log_format");
    public string EncryptionTitle => GetString("settings.encryption_title");
    public string EncryptedExtensionsLabel => GetString("settings.encrypted_extensions_label");
    public string EncryptedExtensionsPlaceholder => GetString("settings.encrypted_extensions_placeholder");
    public string EncryptedExtensionsHelp => GetString("settings.encrypted_extensions_help");
    public string EncryptionKeyLabel => GetString("settings.encryption_key_label");
    public string EncryptionKeyPlaceholder => GetString("settings.encryption_key_placeholder");
    public string BusinessSoftwareTitle => GetString("settings.business_software_title");
    public string EnableDetectionLabel => GetString("settings.enable_detection_label");
    public string BusinessSoftwareNameLabel => GetString("settings.business_software_name_label");
    public string BusinessSoftwareNamePlaceholder => GetString("settings.business_software_name_placeholder");
    public string BusinessSoftwareHelp => GetString("settings.business_software_help");
    public string ExtensionsSearchPlaceholder => GetString("settings.extensions_search_placeholder");
    public string SoftwareSearchPlaceholder => GetString("settings.software_search_placeholder");
    public string SaveButton => GetString("settings.save_button");
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

    public void RefreshTranslations()
    {
        // Notify all properties changed without changing the language
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
            ["settings.language_label"] = new() { [Language.EN] = "Language", [Language.FR] = "Langue" },
            ["settings.select_language"] = new() { [Language.EN] = "Select language", [Language.FR] = "Sélectionnez la langue" },
            ["settings.language_info"] = new() { [Language.EN] = "Language changes apply immediately", [Language.FR] = "Les changements de langue s'appliquent immédiatement" },
            ["settings.log_format"] = new() { [Language.EN] = "Log Format", [Language.FR] = "Format des Logs" },
            ["settings.select_log_format"] = new() { [Language.EN] = "Select log format", [Language.FR] = "Sélectionnez le format de log" },
            ["settings.encryption_title"] = new() { [Language.EN] = "Encryption", [Language.FR] = "Chiffrement" },
            ["settings.encrypted_extensions_label"] = new() { [Language.EN] = "Encrypted File Extensions", [Language.FR] = "Extensions de Fichiers Chiffrées" },
            ["settings.encrypted_extensions_placeholder"] = new() { [Language.EN] = "e.g., .txt, .docx, .pdf", [Language.FR] = "ex: .txt, .docx, .pdf" },
            ["settings.encrypted_extensions_help"] = new() { [Language.EN] = "Comma-separated list of file extensions to encrypt", [Language.FR] = "Liste d'extensions de fichiers à chiffrer, séparées par des virgules" },
            ["settings.encryption_key_label"] = new() { [Language.EN] = "Encryption Key", [Language.FR] = "Clé de Chiffrement" },
            ["settings.encryption_key_placeholder"] = new() { [Language.EN] = "Enter encryption key", [Language.FR] = "Entrez la clé de chiffrement" },
            ["settings.business_software_title"] = new() { [Language.EN] = "Business Software Detection", [Language.FR] = "Détection de Logiciel Métier" },
            ["settings.enable_detection_label"] = new() { [Language.EN] = "Enable business software detection", [Language.FR] = "Activer la détection de logiciel métier" },
            ["settings.business_software_name_label"] = new() { [Language.EN] = "Business Software Name", [Language.FR] = "Nom du Logiciel Métier" },
            ["settings.business_software_name_placeholder"] = new() { [Language.EN] = "e.g., calculator, notepad", [Language.FR] = "ex: calculatrice, bloc-notes" },
            ["settings.business_software_help"] = new() { [Language.EN] = "Backup will be paused while this software is running", [Language.FR] = "La sauvegarde sera mise en pause pendant l'exécution de ce logiciel" },
            ["settings.extensions_search_placeholder"] = new() { [Language.EN] = "Search or add extension...", [Language.FR] = "Rechercher ou ajouter une extension..." },
            ["settings.software_search_placeholder"] = new() { [Language.EN] = "Search or add software...", [Language.FR] = "Rechercher ou ajouter un logiciel..." },
            ["settings.save_button"] = new() { [Language.EN] = "Save Settings", [Language.FR] = "Enregistrer les Paramètres" },
            ["settings.info"] = new() { [Language.EN] = "Settings are saved automatically. Changes take effect immediately.", [Language.FR] = "Les paramètres sont sauvegardés automatiquement. Les modifications prennent effet immédiatement." },

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
