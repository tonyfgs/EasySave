﻿# Exemples d'utilisation de CryptoSoft avec clé automatique

## ✅ Utilisation recommandée - Clé automatique

```csharp
// Dans votre application EasySave
using Infrastructure;

// Créer le service - la clé est générée automatiquement au premier lancement
var cryptoService = new CryptoSoftIntegrationService(
    extensionsToEncrypt: new[] { ".pdf", ".docx", ".xlsx", ".txt" }
);

// Utiliser pendant les sauvegardes
if (cryptoService.ShouldEncrypt("document.pdf"))
{
    var result = cryptoService.EncryptFile("backup/document.pdf");
    if (result.Success)
    {
        Console.WriteLine($"✓ Fichier chiffré : {result.EncryptedFilePath}");
        // Supprimer l'original non chiffré si désiré
        File.Delete("backup/document.pdf");
    }
}
```

## 🔐 Comment ça marche

### 1. Premier lancement
```
✓ Nouvelle clé de chiffrement générée et sauvegardée
📁 Stockée dans : C:\Users\[User]\AppData\Roaming\EasySave\encryption.key
⚠ IMPORTANT : Sauvegardez cette clé dans un endroit sûr !
```

### 2. Lancements suivants
```
✓ Clé de chiffrement chargée depuis le stockage sécurisé
```

### 3. Stockage sécurisé
- **Méthode par défaut** : `[ExeDirectory]/Config/encryption.key` (protégé DPAPI sur Windows)
- **Protection** : Windows DPAPI (LocalMachine) ou permissions fichier sur Linux
- **Sécurité** : Accessible uniquement aux administrateurs système

## 📋 Fonctionnalités avancées

### Choisir la méthode de stockage selon l'environnement

```csharp
// Pour serveur Windows (recommandé)
var cryptoService = new CryptoSoftIntegrationService(
    extensionsToEncrypt: new[] { ".pdf", ".docx" },
    keyStorageMethod: KeyStorageMethod.ProtectedFile  // Config/encryption.key + DPAPI
);

// Pour serveur Linux ou sans DPAPI
var cryptoService = new CryptoSoftIntegrationService(
    extensionsToEncrypt: new[] { ".pdf", ".docx" },
    keyStorageMethod: KeyStorageMethod.LocalFile  // encryption.key à côté de l'exe
);

// Pour environnement multi-utilisateur
var cryptoService = new CryptoSoftIntegrationService(
    extensionsToEncrypt: new[] { ".pdf", ".docx" },
    keyStorageMethod: KeyStorageMethod.ApplicationData  // ProgramData/EasySave/
);
```
```csharp
string key = cryptoService.ExportKey();
Console.WriteLine($"Clé de sauvegarde : {key}");
// Stocker cette clé dans un gestionnaire de mots de passe !
```

### Changer la clé (⚠️ Attention !)
```csharp
// Génère une nouvelle clé aléatoire
cryptoService.ChangeKey();

// Ou utiliser une clé spécifique
cryptoService.ChangeKey("NouvelleCléBase64==");
```

### Utilisation avec stockage local
```csharp
var cryptoService = new CryptoSoftIntegrationService(
    extensionsToEncrypt: new[] { ".pdf", ".docx" },
    keyStorageMethod: KeyStorageMethod.LocalFile  // encryption.key à côté de l'exe
);
```

## 🚫 À éviter

```csharp
// ❌ NE PAS FAIRE - Clé hardcodée
var badService = new CryptoSoftIntegrationService(
    cryptoSoftPath: "path/to/crypto.exe",
    encryptionKey: "MaCléEnDur123456789==",  // DANGER !
    extensionsToEncrypt: extensions
);

// ❌ NE PAS FAIRE - Nouvelle clé à chaque fois
string newKey = AesGcmEncryptor.GenerateKey();  // Les anciens fichiers seront perdus !
```

## 📍 Emplacements des fichiers

### Clé de chiffrement selon l'environnement
- **ProtectedFile** : `[ExeDirectory]/Config/encryption.key` (DPAPI si Windows)
- **LocalFile** : `[ExeDirectory]/encryption.key` (fichier en clair protégé)
- **ApplicationData** : `%ProgramData%\EasySave\encryption.key` (multi-utilisateur)

### Recommandations par type d'installation
| Environnement | Méthode recommandée | Pourquoi |
|---------------|-------------------|-----------|
| **Poste utilisateur** | `ProtectedFile` | Protection DPAPI + isolation |
| **Serveur Windows** | `ProtectedFile` | Protection DPAPI LocalMachine |
| **Serveur Linux** | `LocalFile` | Permissions Unix (600) |
| **Service Windows** | `ApplicationData` | Accès multi-utilisateur |

### CryptoSoft.exe
- **Chemin automatique** : `[ProjectRoot]\CryptoSoft\bin\Release\net8.0\CryptoSoft.exe`
- **En production** : À côté de votre application EasySave

## 🔧 Configuration depuis EasySave

Vous pouvez ajouter un menu de configuration :

```csharp
public class CryptoSettingsCommand
{
    private readonly CryptoSoftIntegrationService _cryptoService;
    
    public void ShowCryptoMenu()
    {
        Console.WriteLine("=== Configuration Chiffrement ===");
        Console.WriteLine("1. Afficher la clé actuelle");
        Console.WriteLine("2. Exporter la clé pour sauvegarde");
        Console.WriteLine("3. Changer la clé (⚠️ Dangereux !)");
        Console.WriteLine("4. Retour");
        
        var choice = Console.ReadLine();
        switch (choice)
        {
            case "1":
                Console.WriteLine($"Clé actuelle : {_cryptoService.ExportKey()}");
                break;
            case "2":
                ExportKeyToFile();
                break;
            case "3":
                ChangeKeyWithConfirmation();
                break;
        }
    }
    
    private void ExportKeyToFile()
    {
        var key = _cryptoService.ExportKey();
        var exportPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), 
                                     "EasySave_EncryptionKey_Backup.txt");
        File.WriteAllText(exportPath, $"Clé de chiffrement EasySave : {key}\nDate : {DateTime.Now}");
        Console.WriteLine($"✓ Clé exportée vers : {exportPath}");
        Console.WriteLine("⚠️ Stockez ce fichier dans un endroit sûr et supprimez-le du bureau !");
    }
    
    private void ChangeKeyWithConfirmation()
    {
        Console.WriteLine("⚠️ ATTENTION : Changer la clé rendra TOUS vos anciens fichiers .crypt indéchiffrables !");
        Console.WriteLine("Êtes-vous sûr ? (tapez 'OUI' pour confirmer) :");
        
        if (Console.ReadLine() == "OUI")
        {
            _cryptoService.ChangeKey();
            Console.WriteLine("✓ Clé changée. Exportez-la immédiatement pour sauvegarde !");
        }
        else
        {
            Console.WriteLine("Opération annulée.");
        }
    }
}
```

## 💡 Bonnes pratiques

1. **Sauvegarde de clé** : Exportez et stockez la clé dans un gestionnaire de mots de passe
2. **Test de récupération** : Testez régulièrement que vous pouvez déchiffrer vos fichiers
3. **Ne jamais hardcoder** : Utilisez toujours la génération automatique
4. **Rotation prudente** : Ne changez la clé que si absolument nécessaire
5. **Documentation** : Informez les utilisateurs de l'importance de la sauvegarde de clé


