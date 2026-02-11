namespace CryptoSoft;

class Program
{
    static int Main(string[] args)
    {
        Console.WriteLine("═══════════════════════════════════════════════════════");
        Console.WriteLine("  CryptoSoft v1.0 - AES-256-GCM Encryption Tool");
        Console.WriteLine("  Développé pour EasySave");
        Console.WriteLine("═══════════════════════════════════════════════════════");
        Console.WriteLine();

        if (args.Length == 0)
        {
            ShowUsage();
            return 2;
        }

        string operation = args[0].ToLowerInvariant();

        if (operation == "genkey")
        {
            string newKey = AesGcmEncryptor.GenerateKey();
            Console.WriteLine("✓ Nouvelle clé AES-256 générée :");
            Console.WriteLine();
            Console.WriteLine($"  {newKey}");
            Console.WriteLine();
            Console.WriteLine("⚠ Conservez cette clé en lieu sûr ! Elle est nécessaire pour déchiffrer vos fichiers.");
            return 0;
        }

        if (args.Length < 3)
        {
            Console.Error.WriteLine("Erreur : Nombre d'arguments insuffisant.");
            Console.Error.WriteLine();
            ShowUsage();
            return 2;
        }

        string filePath = args[1];
        string keyBase64 = args[2];

        if (string.IsNullOrWhiteSpace(keyBase64))
        {
            Console.Error.WriteLine("Erreur : La clé de chiffrement ne peut pas être vide.");
            return 2;
        }

        if (string.IsNullOrWhiteSpace(filePath))
        {
            Console.Error.WriteLine("Erreur : Le chemin du fichier ne peut pas être vide.");
            return 2;
        }

        int exitCode;

        switch (operation)
        {
            case "encrypt":
                Console.WriteLine($"🔒 Chiffrement de : {filePath}");
                Console.WriteLine();
                exitCode = AesGcmEncryptor.EncryptFile(filePath, keyBase64);
                break;

            case "decrypt":
                Console.WriteLine($"🔓 Déchiffrement de : {filePath}");
                Console.WriteLine();
                exitCode = AesGcmEncryptor.DecryptFile(filePath, keyBase64);
                break;

            default:
                Console.Error.WriteLine($"Erreur : Opération inconnue '{args[0]}'");
                Console.Error.WriteLine("Opérations supportées : encrypt, decrypt, genkey");
                Console.Error.WriteLine();
                ShowUsage();
                return 2;
        }

        Console.WriteLine();
        Console.WriteLine($"Code de retour : {exitCode} ({GetExitCodeDescription(exitCode)})");

        return exitCode;
    }
    
    private static void ShowUsage()
    {
        Console.WriteLine("UTILISATION :");
        Console.WriteLine();
        Console.WriteLine("  Chiffrement :");
        Console.WriteLine("    CryptoSoft.exe encrypt \"C:\\dossier\\fichier.pdf\" \"cléBase64_32octets==\"");
        Console.WriteLine();
        Console.WriteLine("  Déchiffrement :");
        Console.WriteLine("    CryptoSoft.exe decrypt \"C:\\dossier\\fichier.pdf.crypt\" \"cléBase64_32octets==\"");
        Console.WriteLine();
        Console.WriteLine("  Génération de clé :");
        Console.WriteLine("    CryptoSoft.exe genkey");
        Console.WriteLine();
        Console.WriteLine("CODES DE RETOUR :");
        Console.WriteLine("  0 - Succès");
        Console.WriteLine("  1 - Fichier source introuvable / non lisible");
        Console.WriteLine("  2 - Arguments invalides (mauvais nombre, clé vide)");
        Console.WriteLine("  3 - Erreur entrée/sortie (disque plein, permissions...)");
        Console.WriteLine("  4 - Échec d'authentification GCM (mauvaise clé ou fichier altéré)");
        Console.WriteLine("  5 - Clé invalide (pas exactement 32 octets après base64)");
        Console.WriteLine();
        Console.WriteLine("SÉCURITÉ :");
        Console.WriteLine("  • Algorithme : AES-256-GCM (mode AEAD)");
        Console.WriteLine("  • Clé : 256 bits (32 octets) en Base64");
        Console.WriteLine("  • Nonce : 12 octets aléatoires par fichier");
        Console.WriteLine("  • Tag : 16 octets d'authentification");
        Console.WriteLine("  • Standard : NIST/ANSSI 2026");
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
            _ => "Code inconnu"
        };
    }
}

