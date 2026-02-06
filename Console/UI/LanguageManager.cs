using Application.Services;
using Shared;

namespace EasySave.UI;

public class LanguageManager
{
    private readonly LanguageApplicationService _languageService;
    private readonly Dictionary<string, Dictionary<Language, string>> _resources;

    public LanguageManager(LanguageApplicationService languageService)
    {
        _languageService = languageService;
        _resources = InitializeResources();
    }

    public Language GetCurrentLanguage()
    {
        return _languageService.GetCurrentLanguage();
    }

    public string GetString(string key)
    {
        if (_resources.TryGetValue(key, out var translations))
        {
            var lang = _languageService.GetCurrentLanguage();
            if (translations.TryGetValue(lang, out var value))
                return value;
        }
        return $"[{key}]";
    }

    private static Dictionary<string, Dictionary<Language, string>> InitializeResources()
    {
        return new Dictionary<string, Dictionary<Language, string>>
        {
            ["menu.title"] = new()
            {
                [Language.EN] = "EasySave - Backup Manager",
                [Language.FR] = "EasySave - Gestionnaire de Sauvegardes"
            },
            ["menu.create"] = new()
            {
                [Language.EN] = "1. Create a backup job",
                [Language.FR] = "1. Creer un travail de sauvegarde"
            },
            ["menu.list"] = new()
            {
                [Language.EN] = "2. List backup jobs",
                [Language.FR] = "2. Lister les travaux de sauvegarde"
            },
            ["menu.modify"] = new()
            {
                [Language.EN] = "3. Modify a backup job",
                [Language.FR] = "3. Modifier un travail de sauvegarde"
            },
            ["menu.delete"] = new()
            {
                [Language.EN] = "4. Delete a backup job",
                [Language.FR] = "4. Supprimer un travail de sauvegarde"
            },
            ["menu.execute"] = new()
            {
                [Language.EN] = "5. Execute backup job(s)",
                [Language.FR] = "5. Executer des travaux de sauvegarde"
            },
            ["menu.language"] = new()
            {
                [Language.EN] = "6. Change language",
                [Language.FR] = "6. Changer la langue"
            },
            ["menu.exit"] = new()
            {
                [Language.EN] = "7. Exit",
                [Language.FR] = "7. Quitter"
            },
            ["prompt.choice"] = new()
            {
                [Language.EN] = "Enter your choice: ",
                [Language.FR] = "Entrez votre choix : "
            },
            ["prompt.name"] = new()
            {
                [Language.EN] = "Enter job name: ",
                [Language.FR] = "Entrez le nom du travail : "
            },
            ["prompt.source"] = new()
            {
                [Language.EN] = "Enter source path: ",
                [Language.FR] = "Entrez le chemin source : "
            },
            ["prompt.target"] = new()
            {
                [Language.EN] = "Enter target path: ",
                [Language.FR] = "Entrez le chemin cible : "
            },
            ["prompt.type"] = new()
            {
                [Language.EN] = "Enter backup type (Full/Differential): ",
                [Language.FR] = "Entrez le type de sauvegarde (Full/Differential) : "
            },
            ["prompt.id"] = new()
            {
                [Language.EN] = "Enter job ID: ",
                [Language.FR] = "Entrez l'ID du travail : "
            },
            ["prompt.language"] = new()
            {
                [Language.EN] = "Enter language (EN/FR): ",
                [Language.FR] = "Entrez la langue (EN/FR) : "
            },
            ["prompt.execute_input"] = new()
            {
                [Language.EN] = "Enter job IDs (e.g., 1-3 or 1;3 or * for all): ",
                [Language.FR] = "Entrez les IDs des travaux (ex: 1-3 ou 1;3 ou * pour tous) : "
            },
            ["error.invalid_choice"] = new()
            {
                [Language.EN] = "Invalid choice. Please try again.",
                [Language.FR] = "Choix invalide. Veuillez reessayer."
            },
            ["error.generic"] = new()
            {
                [Language.EN] = "Error: ",
                [Language.FR] = "Erreur : "
            },
            ["success.job_created"] = new()
            {
                [Language.EN] = "Job created successfully.",
                [Language.FR] = "Travail cree avec succes."
            },
            ["success.job_deleted"] = new()
            {
                [Language.EN] = "Job deleted successfully.",
                [Language.FR] = "Travail supprime avec succes."
            },
            ["success.job_modified"] = new()
            {
                [Language.EN] = "Job modified successfully.",
                [Language.FR] = "Travail modifie avec succes."
            },
            ["success.language_changed"] = new()
            {
                [Language.EN] = "Language changed successfully.",
                [Language.FR] = "Langue changee avec succes."
            },
            ["info.no_jobs"] = new()
            {
                [Language.EN] = "No backup jobs found.",
                [Language.FR] = "Aucun travail de sauvegarde trouve."
            },
            ["info.goodbye"] = new()
            {
                [Language.EN] = "Goodbye!",
                [Language.FR] = "Au revoir !"
            }
        };
    }
}
