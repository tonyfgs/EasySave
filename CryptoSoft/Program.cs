namespace CryptoSoft;

class Program
{
    static int Main(string[] args)
    {
        Console.WriteLine("═══════════════════════════════════════════════════════");
        Console.WriteLine("  CryptoSoft v1.1 - AES-256-GCM Encryption Tool");
        Console.WriteLine("  Développé pour EasySave (Mode Client-Serveur)");
        Console.WriteLine("═══════════════════════════════════════════════════════");
        Console.WriteLine();

        if (args.Length == 0)
        {
            ShowUsage();
            return 2;
        }

        string operation = args[0].ToLowerInvariant();

        return operation switch
        {
            "server" => StartServer(),
            "genkey" => HandleGenerateKey(),
            "help" => ShowHelp(),
            "encrypt" => HandleEncryptClient(args),
            "decrypt" => HandleDecryptClient(args),
            _ => HandleUnknownOperation(operation)
        };
    }

    private static int StartServer()
    {
        var server = new CryptoServer();
        return server.Run();
    }

    private static int HandleEncryptClient(string[] args)
    {
        if (args.Length < 3)
        {
            Console.Error.WriteLine("Erreur : Nombre d'arguments insuffisant.");
            ShowUsage();
            return 2;
        }

        string filePath = args[1];
        string keyBase64 = args[2];

        if (string.IsNullOrWhiteSpace(keyBase64) || string.IsNullOrWhiteSpace(filePath))
        {
            Console.Error.WriteLine("Erreur : Le chemin ou la clé ne peut pas être vide.");
            return 2;
        }

        // Vérifier si le serveur est en cours d'exécution
        if (CryptoClient.IsServerRunning())
        {
            Console.WriteLine("📡 Connexion au serveur CryptoSoft...");
            var client = new CryptoClient();
            var response = client.Encrypt(filePath, keyBase64);

            if (response.Success)
            {
                Console.WriteLine($"✓ Chiffrement réussi en {response.DurationMs}ms");
                return 0;
            }
            else
            {
                Console.Error.WriteLine($"✗ Échec: {response.ErrorMessage}");
                return response.ExitCode;
            }
        }
        else
        {
            // Mode standalone (pas de serveur)
            Console.WriteLine($"🔒 Chiffrement direct de : {filePath}");
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            int exitCode = AesGcmEncryptor.EncryptFile(filePath, keyBase64);
            stopwatch.Stop();

            Console.WriteLine($"⏱ Temps : {stopwatch.ElapsedMilliseconds} ms");
            Console.WriteLine($"Code de retour : {exitCode} ({GetExitCodeDescription(exitCode)})");
            return exitCode;
        }
    }

    private static int HandleDecryptClient(string[] args)
    {
        if (args.Length < 3)
        {
            Console.Error.WriteLine("Erreur : Nombre d'arguments insuffisant.");
            ShowUsage();
            return 2;
        }

        string filePath = args[1];
        string keyBase64 = args[2];

        if (string.IsNullOrWhiteSpace(keyBase64) || string.IsNullOrWhiteSpace(filePath))
        {
            Console.Error.WriteLine("Erreur : Le chemin ou la clé ne peut pas être vide.");
            return 2;
        }

        // Vérifier si le serveur est en cours d'exécution
        if (CryptoClient.IsServerRunning())
        {
            Console.WriteLine("📡 Connexion au serveur CryptoSoft...");
            var client = new CryptoClient();
            var response = client.Decrypt(filePath, keyBase64);

            if (response.Success)
            {
                Console.WriteLine($"✓ Déchiffrement réussi en {response.DurationMs}ms");
                return 0;
            }
            else
            {
                Console.Error.WriteLine($"✗ Échec: {response.ErrorMessage}");
                return response.ExitCode;
            }
        }
        else
        {
            // Mode standalone (pas de serveur)
            Console.WriteLine($"🔓 Déchiffrement direct de : {filePath}");
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            int exitCode = AesGcmEncryptor.DecryptFile(filePath, keyBase64);
            stopwatch.Stop();

            Console.WriteLine($"⏱ Temps : {stopwatch.ElapsedMilliseconds} ms");
            Console.WriteLine($"Code de retour : {exitCode} ({GetExitCodeDescription(exitCode)})");
            return exitCode;
        }
    }

    private static int HandleGenerateKey()
    {
        string newKey = AesGcmEncryptor.GenerateKey();
        Console.WriteLine("✓ Nouvelle clé AES-256 générée :");
        Console.WriteLine();
        Console.WriteLine($"  {newKey}");
        Console.WriteLine();
        Console.WriteLine("⚠ IMPORTANT : Sauvegardez cette clé dans un endroit sûr !");
        Console.WriteLine("  Cette clé sera nécessaire pour déchiffrer vos fichiers.");
        Console.WriteLine("  CryptoSoft ne stocke aucune clé - c'est votre responsabilité.");
        return 0;
    }

    private static int ShowHelp()
    {
        ShowUsage();
        return 0;
    }

    private static int HandleUnknownOperation(string operation)
    {
        Console.Error.WriteLine($"Erreur : Opération inconnue '{operation}'");
        Console.Error.WriteLine();
        ShowUsage();
        return 2;
    }

    private static void ShowUsage()
    {
        Console.WriteLine("UTILISATION :");
        Console.WriteLine();
        Console.WriteLine("  Démarrer le serveur (mono-instance) :");
        Console.WriteLine("    CryptoSoft.exe server");
        Console.WriteLine("    → Lance le serveur qui accepte les requêtes de plusieurs jobs");
        Console.WriteLine();
        Console.WriteLine("  Génération de clé :");
        Console.WriteLine("    CryptoSoft.exe genkey");
        Console.WriteLine("    → Génère une nouvelle clé AES-256. Sauvegardez-la vous-même !");
        Console.WriteLine();
        Console.WriteLine("  Chiffrement :");
        Console.WriteLine("    CryptoSoft.exe encrypt \"C:\\dossier\\fichier.pdf\" \"cléBase64==\"");
        Console.WriteLine("    → Si serveur actif: envoie la requête au serveur");
        Console.WriteLine("    → Sinon: chiffre directement");
        Console.WriteLine();
        Console.WriteLine("  Déchiffrement :");
        Console.WriteLine("    CryptoSoft.exe decrypt \"C:\\dossier\\fichier.pdf.crypt\" \"cléBase64==\"");
        Console.WriteLine("    → Même comportement que encrypt");
        Console.WriteLine();
        Console.WriteLine("  Aide :");
        Console.WriteLine("    CryptoSoft.exe help");
        Console.WriteLine();
        Console.WriteLine("CODES DE RETOUR :");
        Console.WriteLine("  0 - Succès");
        Console.WriteLine("  1 - Fichier source introuvable / non lisible");
        Console.WriteLine("  2 - Arguments invalides");
        Console.WriteLine("  3 - Erreur entrée/sortie");
        Console.WriteLine("  4 - Échec d'authentification GCM");
        Console.WriteLine("  5 - Clé invalide");
        Console.WriteLine("  6 - Serveur déjà en cours / Timeout connexion");
        Console.WriteLine();
        Console.WriteLine("ARCHITECTURE :");
        Console.WriteLine("  • Mode Serveur : Un seul serveur, plusieurs clients simultanés");
        Console.WriteLine("  • Mode Standalone : Si pas de serveur, chiffrement direct");
        Console.WriteLine("  • Named Pipe : Communication inter-processus Windows");
    }

    private static string GetExitCodeDescription(int code)
    {
        return code switch
        {
            0 => "Succès",
            1 => "Fichier introuvable",
            2 => "Arguments invalides",
            3 => "Erreur I/O",
            4 => "Échec authentification GCM",
            5 => "Clé invalide",
            6 => "Instance déjà en cours / Timeout",
            _ => "Code inconnu"
        };
    }
}
