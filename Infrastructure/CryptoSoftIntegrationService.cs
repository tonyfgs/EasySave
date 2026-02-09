using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using static System.Security.Cryptography.ProtectedData;


namespace Infrastructure;

/// <summary>
/// Méthodes de stockage de clé disponibles
/// </summary>
public enum KeyStorageMethod
{
    /// <summary>Fichier protégé par DPAPI à côté de l'application (recommandé)</summary>
    ProtectedFile,
    /// <summary>Fichier local à côté de l'exécutable (pour serveurs sans DPAPI)</summary>
    LocalFile,
    /// <summary>Dossier partagé de l'application (pour serveurs multi-utilisateur)</summary>
    ApplicationData
}


public class CryptoSoftIntegrationService
{
    private readonly string _cryptoSoftPath;
    private readonly string _encryptionKey;
    private readonly HashSet<string> _extensionsToEncrypt;
    private readonly string _keyFilePath;
    
    private static string GetDefaultCryptoSoftPath()
    {
        var appDir = AppDomain.CurrentDomain.BaseDirectory;
        // Remonter au root du projet puis aller vers CryptoSoft
        var rootDir = Path.GetFullPath(Path.Combine(appDir, "..", "..", "..", ".."));
        return Path.Combine(rootDir, "CryptoSoft", "bin", "Release", "net8.0", "CryptoSoft.exe");
    }

    /// <summary>
    /// Initialise le service avec génération automatique de clé
    /// </summary>
    /// <param name="extensionsToEncrypt">Extensions à chiffrer (ex: [".pdf", ".docx"])</param>
    /// <param name="keyStorageMethod">Méthode de stockage de la clé</param>
    public CryptoSoftIntegrationService(string[] extensionsToEncrypt, KeyStorageMethod keyStorageMethod = KeyStorageMethod.ProtectedFile)
        : this(GetDefaultCryptoSoftPath(), extensionsToEncrypt, keyStorageMethod)
    {
    }

    /// <summary>
    /// Initialise le service avec chemin personnalisé
    /// </summary>
    public CryptoSoftIntegrationService(string cryptoSoftPath, string[] extensionsToEncrypt, KeyStorageMethod keyStorageMethod = KeyStorageMethod.ProtectedFile)
    {
        _cryptoSoftPath = cryptoSoftPath ?? throw new ArgumentNullException(nameof(cryptoSoftPath));
        _extensionsToEncrypt = new HashSet<string>(
            extensionsToEncrypt.Select(ext => ext.ToLowerInvariant()),
            StringComparer.OrdinalIgnoreCase
        );

        // Déterminer le chemin de stockage de la clé
        _keyFilePath = GetKeyFilePath(keyStorageMethod);
        
        // Charger ou générer la clé
        _encryptionKey = LoadOrGenerateKey();

        if (!File.Exists(_cryptoSoftPath))
        {
            throw new FileNotFoundException($"CryptoSoft.exe introuvable : {_cryptoSoftPath}");
        }
    }

    /// <summary>
    /// Charge la clé existante ou en génère une nouvelle
    /// </summary>
    private string LoadOrGenerateKey()
    {
        if (File.Exists(_keyFilePath))
        {
            try
            {
                // Tenter de charger selon le type de fichier
                if (_keyFilePath.Contains("Config") && OperatingSystem.IsWindows())
                {
                    try
                    {
                        // Tenter DPAPI d'abord
                        var protectedKey = File.ReadAllBytes(_keyFilePath);
                        var keyBytes = Unprotect(protectedKey, null, DataProtectionScope.LocalMachine);
                        var key = Encoding.UTF8.GetString(keyBytes.AsSpan());
                        
                        Console.WriteLine("✓ Clé de chiffrement chargée depuis le stockage DPAPI");
                        return key;
                    }
                    catch
                    {
                        // Fallback vers lecture en clair
                        return LoadKeyPlainText();
                    }
                }
                else
                {
                    // Lecture en clair
                    return LoadKeyPlainText();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠ Impossible de charger la clé existante : {ex.Message}");
                Console.WriteLine("Génération d'une nouvelle clé...");
            }
        }

        // Générer une nouvelle clé
        var newKey = GenerateNewKey();
        SaveKey(newKey);
        
        Console.WriteLine("✓ Nouvelle clé de chiffrement générée et sauvegardée");
        Console.WriteLine($"📁 Stockée dans : {_keyFilePath}");
        Console.WriteLine("⚠ IMPORTANT : Sauvegardez cette clé dans un endroit sûr !");
        
        return newKey;
    }

    private string LoadKeyPlainText()
    {
        var content = File.ReadAllText(_keyFilePath);
        var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        
        // Trouver la ligne qui ne commence pas par #
        var keyLine = lines.FirstOrDefault(line => !line.TrimStart().StartsWith("#"));
        if (string.IsNullOrWhiteSpace(keyLine))
        {
            throw new InvalidOperationException("Clé introuvable dans le fichier");
        }

        Console.WriteLine("✓ Clé de chiffrement chargée depuis le fichier");
        return keyLine.Trim();
    }

    /// <summary>
    /// Génère une nouvelle clé AES-256
    /// </summary>
    private string GenerateNewKey()
    {
        using var rng = RandomNumberGenerator.Create();
        var keyBytes = new byte[32]; // 256 bits
        rng.GetBytes(keyBytes);
        return Convert.ToBase64String(keyBytes);
    }

    /// <summary>
    /// Sauvegarde la clé de manière sécurisée selon la méthode choisie
    /// </summary>
    private void SaveKey(string key)
    {
        try
        {
            var keyBytes = Encoding.UTF8.GetBytes(key);
            
            // Créer le dossier si nécessaire
            var directory = Path.GetDirectoryName(_keyFilePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Choisir la méthode de protection selon le type de stockage
            if (_keyFilePath.Contains("Config") && OperatingSystem.IsWindows())
            {
                try
                {
                    // Utiliser DPAPI si disponible (Windows uniquement)
                    var protectedKey = Protect(keyBytes, null, DataProtectionScope.LocalMachine);
                    File.WriteAllBytes(_keyFilePath, protectedKey);
                }
                catch (PlatformNotSupportedException)
                {
                    // Fallback : stockage en clair avec avertissement
                    SaveKeyPlainText(key, keyBytes);
                }
            }
            else
            {
                // Stockage en clair pour les autres méthodes
                SaveKeyPlainText(key, keyBytes);
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Impossible de sauvegarder la clé : {ex.Message}", ex);
        }
    }

    private void SaveKeyPlainText(string key, byte[] _)
    {
        // Créer un fichier avec un en-tête explicatif
        var content = $@"# Clé de chiffrement EasySave
# ATTENTION : Ce fichier contient la clé de chiffrement en clair
# Protégez-le avec des permissions système appropriées !
# Date de génération : {DateTime.Now:yyyy-MM-dd HH:mm:ss}
{key}";

        File.WriteAllText(_keyFilePath, content);
        
        try
        {
            // Définir des permissions restrictives (Windows/Unix compatible)
            if (OperatingSystem.IsWindows())
            {
                var fileInfo = new FileInfo(_keyFilePath);
                var fileSecurity = fileInfo.GetAccessControl();
                // Réservé aux administrateurs uniquement
                fileSecurity.SetAccessRuleProtection(true, false);
                fileInfo.SetAccessControl(fileSecurity);
            }
            else
            {
                // Unix : rw------- (600)
                File.SetUnixFileMode(_keyFilePath, UnixFileMode.UserRead | UnixFileMode.UserWrite);
            }
        }
        catch
        {
            // Permissions non définies, mais fichier créé
        }

        Console.WriteLine($"⚠️  Clé stockée en clair dans : {_keyFilePath}");
        Console.WriteLine("⚠️  Assurez-vous que seuls les administrateurs peuvent lire ce fichier !");
    }

    /// <summary>
    /// Détermine le chemin de stockage selon la méthode choisie
    /// </summary>
    private string GetKeyFilePath(KeyStorageMethod method)
    {
        return method switch
        {
            KeyStorageMethod.ProtectedFile => Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "Config", "encryption.key"
            ),
            KeyStorageMethod.LocalFile => Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "encryption.key"
            ),
            KeyStorageMethod.ApplicationData => Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "EasySave", "encryption.key"
            ),
            _ => throw new ArgumentException("Méthode de stockage non supportée", nameof(method))
        };
    }

    /// <summary>
    /// Exporte la clé pour sauvegarde manuelle
    /// </summary>
    public string ExportKey()
    {
        return _encryptionKey;
    }

    /// <summary>
    /// Change la clé de chiffrement (attention : les anciens fichiers ne seront plus déchiffrables !)
    /// </summary>
    public void ChangeKey(string newKey = null)
    {
        if (newKey == null)
        {
            newKey = GenerateNewKey();
        }
        
        // Valider la nouvelle clé
        try
        {
            var keyBytes = Convert.FromBase64String(newKey);
            if (keyBytes.Length != 32)
            {
                throw new ArgumentException("La clé doit faire 32 octets (256 bits)");
            }
        }
        catch (FormatException)
        {
            throw new ArgumentException("La clé doit être en Base64 valide");
        }
        
        SaveKey(newKey);
        Console.WriteLine("⚠ Clé de chiffrement changée ! Les anciens fichiers .crypt ne seront plus déchiffrables.");
    }

    public bool ShouldEncrypt(string filePath)
    {
        string extension = Path.GetExtension(filePath);
        return _extensionsToEncrypt.Contains(extension);
    }

    public CryptoResult EncryptFile(string filePath)
    {
        var stopwatch = Stopwatch.StartNew();
        
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = _cryptoSoftPath,
                Arguments = $"encrypt \"{filePath}\" \"{_encryptionKey}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            }
        };

        try
        {
            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();
            process.WaitForExit();
            
            stopwatch.Stop();

            return new CryptoResult
            {
                ExitCode = process.ExitCode,
                Success = process.ExitCode == 0,
                Output = output,
                Error = error,
                Duration = stopwatch.Elapsed,
                EncryptedFilePath = process.ExitCode == 0 ? filePath + ".crypt" : null
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return new CryptoResult
            {
                ExitCode = -1,
                Success = false,
                Error = $"Exception lors du chiffrement : {ex.Message}",
                Duration = stopwatch.Elapsed
            };
        }
    }

    public CryptoResult DecryptFile(string encryptedFilePath)
    {
        var stopwatch = Stopwatch.StartNew();
        
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = _cryptoSoftPath,
                Arguments = $"decrypt \"{encryptedFilePath}\" \"{_encryptionKey}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            }
        };

        try
        {
            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();
            process.WaitForExit();
            
            stopwatch.Stop();

            return new CryptoResult
            {
                ExitCode = process.ExitCode,
                Success = process.ExitCode == 0,
                Output = output,
                Error = error,
                Duration = stopwatch.Elapsed,
                DecryptedFilePath = process.ExitCode == 0 
                    ? encryptedFilePath.Replace(".crypt", "") 
                    : null
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return new CryptoResult
            {
                ExitCode = -1,
                Success = false,
                Error = $"Exception lors du déchiffrement : {ex.Message}",
                Duration = stopwatch.Elapsed
            };
        }
    }

    public BackupFileProcessResult ProcessBackupFile(string sourcePath, string destinationPath)
    {
        try
        {
            // Copier d'abord le fichier vers la destination
            File.Copy(sourcePath, destinationPath, overwrite: true);

            // Si l'extension nécessite un chiffrement
            if (ShouldEncrypt(sourcePath))
            {
                var encryptResult = EncryptFile(destinationPath);
                
                if (encryptResult.Success)
                {
                    // Supprimer le fichier non chiffré, garder uniquement le .crypt
                    File.Delete(destinationPath);
                    
                    return new BackupFileProcessResult
                    {
                        Success = true,
                        WasEncrypted = true,
                        FinalPath = encryptResult.EncryptedFilePath,
                        Message = "Fichier copié et chiffré avec succès"
                    };
                }
                else
                {
                    // Échec du chiffrement, mais le fichier non chiffré existe toujours
                    return new BackupFileProcessResult
                    {
                        Success = false,
                        WasEncrypted = false,
                        FinalPath = destinationPath,
                        Message = $"Échec du chiffrement (code {encryptResult.ExitCode}): {encryptResult.Error}"
                    };
                }
            }
            else
            {
                // Pas de chiffrement nécessaire
                return new BackupFileProcessResult
                {
                    Success = true,
                    WasEncrypted = false,
                    FinalPath = destinationPath,
                    Message = "Fichier copié sans chiffrement"
                };
            }
        }
        catch (Exception ex)
        {
            return new BackupFileProcessResult
            {
                Success = false,
                WasEncrypted = false,
                Message = $"Erreur lors du traitement : {ex.Message}"
            };
        }
    }
}

/// <summary>
/// Résultat d'une opération de chiffrement/déchiffrement CryptoSoft
/// </summary>
public class CryptoResult
{
    public int ExitCode { get; set; }
    public bool Success { get; set; }
    public string? Output { get; set; }
    public string? Error { get; set; }
    public TimeSpan Duration { get; set; }
    public string? EncryptedFilePath { get; set; }
    public string? DecryptedFilePath { get; set; }
}

/// <summary>
/// Résultat du traitement d'un fichier de sauvegarde (avec ou sans chiffrement)
/// </summary>
public class BackupFileProcessResult
{
    public bool Success { get; set; }
    public bool WasEncrypted { get; set; }
    public string? FinalPath { get; set; }
    public string Message { get; set; } = string.Empty;
}
