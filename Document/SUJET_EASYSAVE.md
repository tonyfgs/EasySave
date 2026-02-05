# Projet EasySave - Cahier des Charges Complet

> **Éditeur** : ProSoft
> **Formation** : PGE A3 FISA INFO Génie Logiciel 2526
> **Module** : Programmation Système

---

## 1. Présentation du Projet

### 1.1 Contexte

Votre équipe intègre l'éditeur de logiciels **ProSoft** sous la responsabilité du DSI. Vous êtes chargés de développer **EasySave**, un logiciel de sauvegarde qui évoluera à travers 3 versions majeures.

### 1.2 Politique Tarifaire

| Élément | Tarif |
|---------|-------|
| Prix unitaire | 200 €HT |
| Contrat maintenance annuel (5/7, 8h-17h) | 12% du prix d'achat |

> Contrat à tacite reconduction avec revalorisation basée sur l'indice SYNTEC.

---

## 2. Calendrier des Livrables

### Livrable 1 - Version 1.0

| Jalon | Échéance |
|-------|----------|
| Lancement du projet + Cahier des charges v1.0 | Jour 1 |
| Création environnement de travail + accès tuteur | Jour 3 |
| Livraison diagrammes UML | Veille Livrable 1 |
| Réception Livrable 1 + documentation | Jour Livrable 1 |

### Livrable 2 - Versions 1.1 et 2.0 *(non évalué)*

| Jalon | Échéance |
|-------|----------|
| Mise à disposition CDC v1.1 et v2.0 | Lendemain Livrable 1 |
| Livraison diagrammes UML | Veille Livrable 2 |
| Réception Livrable 2 | Jour Livrable 2 |

### Livrable 3 - Version 3.0

| Jalon | Échéance |
|-------|----------|
| Mise à disposition CDC v3.0 | Lendemain Livrable 2 |
| Livraison diagrammes UML | Avant-veille soutenance |
| Réception Livrable 3 | Veille soutenance |
| Soutenance du projet | Jour soutenance |

---

## 3. Contraintes Techniques

### 3.1 Outils Imposés

| Catégorie | Outil |
|-----------|-------|
| IDE | Visual Studio 2022 ou supérieur |
| Versioning | GitHub |
| UML | ArgoUML (recommandé) |

> Le tuteur/pilote doit être invité sur le dépôt Git.

### 3.2 Technologies

| Élément | Spécification |
|---------|---------------|
| Langage | C# |
| Framework | .NET 8.0 |

### 3.3 Qualité du Code

- Code et commentaires en **anglais** (filiales anglophones)
- Fonctions de taille raisonnable
- **Aucune redondance** de code (pas de copier-coller)
- Respect des conventions de nommage C#

### 3.4 Documentation Requise

| Document | Contrainte |
|----------|------------|
| Manuel utilisateur | 1 page maximum |
| Documentation support | Emplacement logiciel, config minimale, fichiers de config |
| Release notes | Obligatoire à chaque version |

---

## 4. Spécifications Version 1.0

### 4.1 Généralités

- Application **Console** en .NET
- Interface bilingue : **Français** et **Anglais**
- Maximum **5 travaux de sauvegarde**

### 4.2 Définition d'un Travail de Sauvegarde

Un travail de sauvegarde comprend :

| Attribut | Description |
|----------|-------------|
| Nom | Identifiant du travail |
| Répertoire source | Dossier à sauvegarder |
| Répertoire cible | Destination de la sauvegarde |
| Type | Complète ou Différentielle |

### 4.3 Types de Sauvegarde

| Type | Comportement |
|------|--------------|
| **Complète** | Copie tous les fichiers à chaque exécution |
| **Différentielle** | Copie uniquement les fichiers modifiés depuis la dernière sauvegarde complète |

### 4.4 Exécution

#### Via menu interactif
L'utilisateur peut lancer un travail ou l'ensemble des travaux séquentiellement.

#### Via ligne de commande

```bash
# Exécuter les travaux 1 à 3
EasySave.exe 1-3

# Exécuter les travaux 1 et 3
EasySave.exe 1;3
```

### 4.5 Supports de Stockage

Les répertoires source et cible peuvent être sur :
- Disques locaux
- Disques externes
- Lecteurs réseaux

> Tous les éléments (fichiers ET sous-répertoires) doivent être sauvegardés.

### 4.6 Fichier Log Journalier

#### Caractéristiques
- Un fichier par jour (ex: `2024-01-26.json`)
- Écriture en **temps réel**
- Format **JSON** avec retours à la ligne

#### Informations enregistrées

| Champ | Description |
|-------|-------------|
| Horodatage | Date et heure de l'action |
| Nom de sauvegarde | Identifiant du travail |
| Fichier source | Chemin complet (format UNC) |
| Fichier destination | Chemin complet (format UNC) |
| Taille | Taille du fichier en octets |
| Temps de transfert | Durée en ms (négatif si erreur) |

#### Exemple

```json
[
  {
    "Timestamp": "2024-01-26T14:30:00",
    "BackupName": "Sauvegarde Photos",
    "SourcePath": "\\\\PC\\D$\\Photos\\chat.jpg",
    "DestPath": "\\\\PC\\E$\\Backup\\Photos\\chat.jpg",
    "FileSize": 2048576,
    "TransferTimeMs": 150
  }
]
```

#### DLL EasyLog

> **IMPORTANT** : Cette fonctionnalité doit être développée dans une bibliothèque séparée `EasyLog.dll` pour être réutilisable par d'autres projets ProSoft.

### 4.7 Fichier État Temps Réel

#### Caractéristiques
- Fichier **unique** (`state.json`)
- Mise à jour en **temps réel**
- Contient l'état de **tous** les travaux

#### Informations enregistrées

| Champ | Description |
|-------|-------------|
| Nom du travail | Identifiant |
| Horodatage | Dernière action |
| État | Actif / Inactif |
| Nombre total de fichiers | Si actif |
| Taille totale à transférer | Si actif |
| Progression | Pourcentage |
| Fichiers restants | Nombre |
| Taille restante | En octets |
| Fichier source en cours | Chemin complet |
| Fichier destination en cours | Chemin complet |

#### Exemple

```json
[
  {
    "Name": "Sauvegarde Photos",
    "Timestamp": "2024-01-26T14:32:16",
    "State": "ACTIVE",
    "TotalFiles": 150,
    "TotalSize": 524288000,
    "Progress": 45,
    "FilesRemaining": 82,
    "SizeRemaining": 288358400,
    "CurrentSourceFile": "\\\\PC\\D$\\Photos\\vacances.jpg",
    "CurrentDestFile": "\\\\PC\\E$\\Backup\\Photos\\vacances.jpg"
  },
  {
    "Name": "Sauvegarde Documents",
    "State": "INACTIVE"
  }
]
```

### 4.8 Emplacement des Fichiers

> **INTERDIT** : Chemins en dur type `C:\temp\`

Les fichiers (log, état, configuration) doivent être placés dans des emplacements compatibles avec les serveurs clients (ex: `%APPDATA%`, dossier de l'application).

---

## 5. Spécifications Version 1.1

Mise à jour mineure demandée par un client important.

### Modification unique

| Fonctionnalité | Description |
|----------------|-------------|
| Format du fichier Log | L'utilisateur peut choisir entre **JSON** ou **XML** |

> Cette version doit sortir au plus tard en même temps que la version 2.0.

---

## 6. Spécifications Version 2.0

### 6.1 Évolutions Majeures

| Fonctionnalité | Description |
|----------------|-------------|
| Interface graphique | Abandon du mode Console → **WPF** ou Avalonia |
| Travaux illimités | Plus de limite à 5 |
| Cryptage | Via le logiciel externe **CryptoSoft** |
| Logiciel métier | Blocage si détecté |
| Format Log | JSON ou XML (comme v1.1) |

### 6.2 Cryptage CryptoSoft

- Seuls les fichiers avec extensions définies par l'utilisateur sont cryptés
- Extensions configurables dans les paramètres généraux

### 6.3 Évolution du Fichier Log

Nouveau champ ajouté :

| Champ | Valeur |
|-------|--------|
| Temps de cryptage | `0` = pas de cryptage |
| | `> 0` = temps en ms |
| | `< 0` = code erreur |

### 6.4 Détection Logiciel Métier

- Si un logiciel métier est en cours d'exécution → **impossible de lancer** un travail
- En mode séquentiel : termine le fichier en cours puis s'arrête
- Logiciel métier configurable dans les paramètres
- Arrêt consigné dans le fichier log

> Pour les démos, l'application Calculatrice peut servir de logiciel métier.

---

## 7. Spécifications Version 3.0

### 7.1 Évolutions Majeures

| Fonctionnalité | Description |
|----------------|-------------|
| Sauvegarde parallèle | Abandon du mode séquentiel |
| Fichiers prioritaires | Extensions prioritaires traitées en premier |
| Limite bande passante | Un seul gros fichier (> n Ko) à la fois |
| Contrôles temps réel | Play / Pause / Stop par travail |
| Logiciel métier | Pause automatique (au lieu de blocage) |
| CryptoSoft mono-instance | Une seule instance simultanée |
| Centralisation logs | Service Docker |

### 7.2 Gestion des Fichiers Prioritaires

- L'utilisateur définit une liste d'extensions prioritaires
- Aucun fichier non-prioritaire ne peut être transféré tant qu'il reste des fichiers prioritaires **sur n'importe quel travail**

### 7.3 Limite de Bande Passante

- Seuil configurable : **n Ko**
- Un seul fichier > n Ko peut être transféré à la fois
- Les fichiers < n Ko peuvent continuer en parallèle
- Respect de la règle des fichiers prioritaires

### 7.4 Contrôles Temps Réel

Pour chaque travail (ou tous les travaux) :

| Action | Comportement |
|--------|--------------|
| **Play** | Démarre ou reprend |
| **Pause** | Pause après le fichier en cours |
| **Stop** | Arrêt immédiat |

L'utilisateur doit voir la progression en temps réel (minimum : pourcentage).

### 7.5 Logiciel Métier - Comportement v3.0

- Détection → **Pause automatique** de tous les travaux
- Fermeture du logiciel métier → **Reprise automatique**

### 7.6 CryptoSoft Mono-Instance

- Modifier CryptoSoft pour qu'il ne puisse s'exécuter qu'une seule fois simultanément
- Gérer les conflits dans EasySave

### 7.7 Centralisation des Logs (Docker)

Service de centralisation en temps réel avec 3 modes :

| Mode | Description |
|------|-------------|
| Centralisé uniquement | Logs sur le serveur Docker seulement |
| Local uniquement | Logs sur chaque PC utilisateur |
| Les deux | Local + Docker |

> Un fichier journalier unique quel que soit le nombre d'utilisateurs/machines.

---

## 8. Tableau Comparatif des Versions

| Fonctionnalité | v1.0 | v1.1 | v2.0 | v3.0 |
|----------------|------|------|------|------|
| Interface | Console | Console | GUI | GUI |
| Multi-langues | FR/EN | FR/EN | FR/EN | FR/EN |
| Travaux max | 5 | 5 | Illimité | Illimité |
| Format Log | JSON | JSON/XML | JSON/XML | JSON/XML |
| DLL EasyLog | Oui | Oui | Oui | Oui |
| Fichier État | Oui | Oui | Oui | Oui |
| Exécution | Séquentielle | Séquentielle | Séquentielle | **Parallèle** |
| Play/Pause/Stop | Non | Non | Non | **Oui** |
| Logiciel métier | Non | Non | Bloque | **Pause auto** |
| CryptoSoft | Non | Non | Oui | Oui (mono) |
| Fichiers prioritaires | Non | Non | Non | **Oui** |
| Limite gros fichiers | Non | Non | Non | **Oui** |
| Centralisation logs | Non | Non | Non | **Docker** |
| Ligne de commande | Oui | Oui | Oui | Oui |

---

## 9. Livrables Attendus

### Par version

- Code source complet sur GitHub
- Diagrammes UML (la veille de chaque livrable)
- Release notes
- Manuel utilisateur (1 page)
- Documentation support technique

### Évaluation

Points de vigilance :
- Gestion Git (versioning, travail en équipe)
- Qualité du code (pas de redondance)
- Architecture du code
- Respect des contraintes

### Soutenance v3.0

Présenter :
- Les évolutions envisagées pour une v4.0
- Analyse bénéfice client / temps de développement
- Pertinence des sauvegardes parallèles
