# Manuel Utilisateur EasySave v1.0 — Windows

## 1. Introduction

EasySave est un logiciel de sauvegarde developpe par ProSoft. Il permet de creer et gerer jusqu'a 5 travaux de sauvegarde, avec prise en charge des sauvegardes completes et differentielles.

Fonctionnalites principales :
- Creation, modification, suppression de travaux de sauvegarde
- Sauvegarde complete (tous les fichiers) ou differentielle (fichiers modifies uniquement)
- Interface bilingue francais/anglais
- Journalisation en temps reel (JSON ou XML)
- Suivi de progression en temps reel
- Mode interactif (menu) et mode ligne de commande

---

## 2. Prerequis

- **Systeme** : Windows 10 ou superieur
- **Runtime** : [.NET 8.0 Runtime](https://dotnet.microsoft.com/download/dotnet/8.0)

Pour verifier l'installation :
```powershell
dotnet --version
```

---

## 3. Installation et lancement

### Compilation depuis les sources

```powershell
cd C:\chemin\vers\EasySave
dotnet build
```

### Lancement en mode interactif

```powershell
dotnet run --project Console\Console.csproj
```

### Lancement en mode ligne de commande

```powershell
dotnet run --project Console\Console.csproj -- 1
dotnet run --project Console\Console.csproj -- 1-3
dotnet run --project Console\Console.csproj -- "1;3"
```

---

## 4. Emplacement des fichiers

EasySave utilise `%APPDATA%\EasySave\` pour stocker ses donnees. Ce chemin est resolu dynamiquement par Windows via la variable d'environnement `%APPDATA%` (aucun chemin en dur type `C:\temp\`).

### Resolution du chemin selon l'environnement

| Environnement | Chemin resolu | Remarque |
|----------------|---------------|----------|
| Poste Windows standard | `C:\Users\Jean\AppData\Roaming\EasySave\` | Chemin local par defaut |
| Windows Server (session utilisateur) | `C:\Users\Jean\AppData\Roaming\EasySave\` | Identique, resolu par l'utilisateur connecte |
| Profil itinerant (roaming profile) | `C:\Users\Jean\AppData\Roaming\EasySave\` | Chemin local, le contenu du dossier `Roaming` est synchronise automatiquement avec le serveur au login/logout |
| Redirection de dossier (GPO) | `\\serveur\partages$\Jean\AppData\Roaming\EasySave\` | Uniquement si l'administrateur a configure une redirection via strategie de groupe (Group Policy) |

> **Note** : avec un profil itinerant standard, le chemin reste local. C'est Windows qui synchronise le contenu vers le serveur. Seule la redirection de dossier (GPO) modifie le chemin resolu.

Pour ouvrir ce dossier :
```powershell
explorer %APPDATA%\EasySave
```

### Contenu du dossier

| Fichier | Description |
|---------|-------------|
| `config.json` | Configuration (langue, format de log) |
| `jobs.json` | Definitions des travaux de sauvegarde |
| `state.json` | Etat en temps reel pendant l'execution |
| `logs\` | Dossier des journaux quotidiens |

Ces fichiers et dossiers sont crees automatiquement au premier lancement.

---

## 5. Mode interactif

Au lancement sans arguments, l'application affiche le menu principal :

```
EasySave - Gestionnaire de Sauvegardes
----------------------------------------
1. Creer un travail de sauvegarde
2. Lister les travaux de sauvegarde
3. Modifier un travail de sauvegarde
4. Supprimer un travail de sauvegarde
5. Executer des travaux de sauvegarde
6. Changer la langue
7. Quitter

Entrez votre choix :
```

Saisissez le numero de l'option souhaitee puis appuyez sur Entree.

En cas de choix invalide : `Choix invalide. Veuillez reessayer.`

---

## 6. Commandes detaillees

### 6.1 Creer un travail de sauvegarde (option 1)

L'application demande successivement :

```
Entrez le nom du travail : MesDocuments
Entrez le chemin source : C:\Users\Jean\Documents
Entrez le chemin cible : E:\Sauvegardes\Documents
Entrez le type de sauvegarde (Full/Differential) : Full
```

**Resultat** : `Job 'MesDocuments' created with ID 1.`

**Limites** : Maximum 5 travaux. Au-dela, une erreur est affichee.

### 6.2 Lister les travaux de sauvegarde (option 2)

Affiche tous les travaux configures :

```
[1] MesDocuments | C:\Users\Jean\Documents -> E:\Sauvegardes\Documents | Full
[2] Photos | \\serveur\partage\photos -> D:\Backup\Photos | Differential
```

Si aucun travail n'existe : `No backup jobs found.`

### 6.3 Modifier un travail de sauvegarde (option 3)

```
Entrez l'ID du travail : 1
Entrez le nom du travail : MesDocuments_v2
Entrez le chemin source : C:\Users\Jean\Documents
Entrez le chemin cible : F:\NouveauDisque\Documents
Entrez le type de sauvegarde (Full/Differential) : Differential
```

**Resultat** : `Job 1 modified.`

> Tous les champs doivent etre saisis a nouveau (pas de modification partielle).

### 6.4 Supprimer un travail de sauvegarde (option 4)

```
Entrez l'ID du travail : 2
```

**Resultat** : `Job 2 deleted.`

> La suppression est immediate, sans confirmation.

### 6.5 Executer des travaux de sauvegarde (option 5)

```
Entrez les IDs des travaux (ex: 1-3 ou 1;3 ou * pour tous) :
```

Formats acceptes :

| Format | Exemple | Effet |
|--------|---------|-------|
| Un seul | `1` | Execute le travail 1 |
| Plage | `1-3` | Execute les travaux 1, 2 et 3 |
| Liste | `1;3` | Execute les travaux 1 et 3 |
| Tous | `*` | Execute tous les travaux |

**Resultat par travail** :
- Succes : `Job 1: OK (145 files, 2567890 bytes)`
- Echec : `Job 1: FAILED - [message d'erreur]`

### 6.6 Changer la langue (option 6)

```
Entrez la langue (EN/FR) : FR
```

**Resultat** : `Language changed to FR.`

Le menu s'affiche immediatement dans la nouvelle langue. Le choix est sauvegarde et persiste entre les sessions.

### 6.7 Quitter (option 7)

Affiche `Au revoir !` et ferme l'application.

---

## 7. Mode ligne de commande

Executez des travaux directement sans passer par le menu interactif :

```powershell
# Executer le travail 1
dotnet run --project Console\Console.csproj -- 1

# Executer les travaux 1 a 3
dotnet run --project Console\Console.csproj -- 1-3

# Executer les travaux 1 et 3
dotnet run --project Console\Console.csproj -- "1;3"
```

> Le point-virgule doit etre entre guillemets pour eviter qu'il soit interprete par PowerShell.

Les resultats s'affichent dans le terminal puis l'application se ferme automatiquement.

---

## 8. Types de sauvegarde

### Sauvegarde complete (Full)

Copie **tous les fichiers** du dossier source vers le dossier cible a chaque execution.

- Avantage : restauration simple, un seul jeu de fichiers
- Inconvenient : plus lent, plus d'espace disque necessaire

### Sauvegarde differentielle (Differential)

Copie uniquement les fichiers **modifies depuis la derniere sauvegarde complete**.

- Avantage : plus rapide, moins d'espace disque
- Inconvenient : necessite d'avoir effectue une sauvegarde complete au prealable
- Comparaison basee sur la date de modification des fichiers

---

## 9. Fichiers generes

### Journal de transfert (`logs\AAAA-MM-JJ.json`)

Un fichier par jour, au format JSON. Chaque entree contient :

```json
[
  {
    "Timestamp": "2026-02-06T10:15:23.456789Z",
    "BackupName": "MesDocuments",
    "SourcePath": "\\\\DESKTOP-ABC\\Users\\Jean\\Documents\\rapport.pdf",
    "DestPath": "\\\\DESKTOP-ABC\\E$\\Sauvegardes\\Documents\\rapport.pdf",
    "FileSize": 1048576,
    "TransferTimeMs": 250
  }
]
```

| Champ | Description |
|-------|-------------|
| `Timestamp` | Date et heure ISO 8601 |
| `BackupName` | Nom du travail de sauvegarde |
| `SourcePath` | Chemin source au format UNC |
| `DestPath` | Chemin destination au format UNC |
| `FileSize` | Taille du fichier en octets |
| `TransferTimeMs` | Temps de transfert en ms (-1 si erreur) |

### Fichier d'etat (`state.json`)

Mis a jour en temps reel pendant l'execution :

```json
[
  {
    "Name": "MesDocuments",
    "Timestamp": "2026-02-06T10:15:23.456789Z",
    "State": "ACTIVE",
    "TotalFiles": 250,
    "TotalSize": 5368709120,
    "Progress": 35,
    "FilesRemaining": 162,
    "SizeRemaining": 3489259520,
    "CurrentSourceFile": "C:\\Users\\Jean\\Documents\\dossier\\fichier.docx",
    "CurrentDestFile": "E:\\Sauvegardes\\Documents\\dossier\\fichier.docx"
  }
]
```

Etats possibles :

| Etat | Signification |
|------|---------------|
| `INACTIVE` | Travail non demarre |
| `ACTIVE` | Travail en cours d'execution |
| `END` | Travail termine avec succes |
| `ERROR` | Travail termine avec erreur |

---

## 10. Limites de la version 1.0

- Maximum **5 travaux** de sauvegarde
- Execution **sequentielle** (un travail a la fois)
- Pas de chiffrement des fichiers (disponible en v2.0)
- Pas d'interface graphique (disponible en v2.0)
- Pas de controle pause/reprise (disponible en v3.0)

---

## 11. Depannage

| Probleme | Solution |
|----------|----------|
| `dotnet` non reconnu | Installer le [.NET 8.0 Runtime](https://dotnet.microsoft.com/download/dotnet/8.0) |
| Erreur d'acces au dossier cible | Verifier les permissions sur le dossier de destination |
| Chemin reseau inaccessible | Verifier la connexion reseau et les droits d'acces au partage |
| Maximum 5 travaux atteint | Supprimer un travail existant avant d'en creer un nouveau |
| Sauvegarde differentielle vide | Effectuer d'abord une sauvegarde complete (Full) |
| Fichiers de config corrompus | Supprimer `%APPDATA%\EasySave\config.json` (recree avec valeurs par defaut) |
