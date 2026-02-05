# EasySave v1.0 - Documentation de Conception

## Notre Approche Méthodologique

### 1. Identification des Fonctionnalités

Nous avons débuté notre conception par l'identification collaborative des fonctionnalités métier. Cette approche, plus simple et moins contraignante qu'un diagramme de cas d'utilisation UML traditionnel, nous a permis de capturer rapidement les besoins tout en restant agile face aux évolutions.

**Extrait de nos fonctionnalités identifiées :**

```
- L'utilisateur peut créer jusqu'à 5 travaux de sauvegarde
- L'utilisateur peut exécuter un ou plusieurs travaux (séquentiellement)
- L'utilisateur peut lister tous les travaux configurés
- L'utilisateur peut modifier les paramètres d'un travail existant
- L'utilisateur peut supprimer un travail de sauvegarde
- Le système journalise chaque transfert de fichier (horodatage, taille, durée)
- Le système maintient un fichier d'état en temps réel
```

Cette liste, maintenue en équipe dans un simple fichier markdown, est plus facile à modifier et à comprendre qu'un diagramme formel. Elle sert de base contractuelle entre les développeurs et reste toujours synchronisée avec le code.

### 2. Des Fonctionnalités aux Diagrammes de Séquence Système (DSS)

Pour chaque fonctionnalité métier identifiée, nous avons élaboré un **Diagramme de Séquence Système** (DSS). Le DSS représente le système comme une **boîte noire** : seules les interactions entre l'acteur externe (Utilisateur) et le système global (`:EasySave`) sont modélisées.

Cette étape nous a permis d'identifier :

- Les **opérations système** : les méthodes que le système doit exposer (ex: `créerTravail()`, `exécuterSauvegarde()`)
- Les **données d'entrée/sortie** : ce que l'utilisateur fournit et ce que le système retourne
- Les **flux alternatifs** : gestion des erreurs, cas limites (ex: limite de 5 travaux atteinte)
- Les **contraintes métier** : règles à respecter durant l'exécution

Les DSS disponibles dans ce dossier :
- `DSS_UC01_Gerer_Travaux.puml` - Création, modification, suppression et liste des travaux
- `DSS_UC02_Executer_Sauvegarde.puml` - Exécution d'une sauvegarde (Full ou Différentielle)
- `DSS_UC03_Changer_Langue.puml` - Changement de la langue de l'interface

### 3. Des Opérations Système au Diagramme de Classes

Les DSS ne montrent pas les classes internes - c'est leur rôle de rester au niveau système. Pour concevoir l'architecture interne, nous avons suivi une démarche complémentaire :

1. **Extraction des opérations système** : À partir des DSS, nous avons listé toutes les opérations que le système doit implémenter (`créerTravail`, `exécuterSauvegarde`, `journaliserTransfert`, etc.)

2. **Analyse du domaine métier** : Nous avons identifié les concepts métier clés :
   - **Entités** : `BackupJob` (travail de sauvegarde avec identité)
   - **Value Objects** : `FileDescriptor`, `TransferInfo`, `StateInfo`, `BackupResult`
   - **Événements domaine** : `TransferCompletedEvent`, `StateChangedEvent`

3. **Conception des services** : Pour orchestrer les opérations système :
   - `BackupApplicationService` : coordination multi-jobs (façade applicative)
   - `BackupOrchestrator` : exécution d'un job unique

4. **Définition des ports et adapters** : Pour isoler le domaine des détails techniques :
   - **Ports** (interfaces) : `IJobRepository`, `ILogger`, `IFileSystemGateway`
   - **Adapters** (implémentations) : `FileJobRepository`, `JsonLogger`, `LocalFileSystemGateway`

Le diagramme de classes final est disponible : `ClassDiagram.puml`

---

## Architecture : Hexagonale + Couches

Notre architecture est un **mix entre l'architecture hexagonale (Ports & Adapters) et l'architecture en couches**. Ce choix nous permet de bénéficier des avantages des deux approches :

```
┌─────────────────────────────────────────────────────────────┐
│                    Presentation (CLI)                        │
├─────────────────────────────────────────────────────────────┤
│                    Application Layer                         │
│         (Use Cases, Orchestration, Event Handlers)          │
├─────────────────────────────────────────────────────────────┤
│                      Domain Layer                            │
│        (Entities, Value Objects, Domain Events)             │
├─────────────────────────────────────────────────────────────┤
│                   Infrastructure Layer                       │
│     (Repositories, File System, Loggers, Adapters)          │
└─────────────────────────────────────────────────────────────┘
```

Les **Ports** (interfaces) sont définis dans la couche Application et implémentés par des **Adapters** dans la couche Infrastructure. Cette inversion de dépendance garantit que le domaine métier reste indépendant des détails techniques.

---

## Respect des Principes SOLID

Notre conception applique rigoureusement les principes SOLID :

### Single Responsibility Principle (SRP)
Chaque classe a une responsabilité unique et clairement définie :
- `BackupOrchestrator` : orchestre l'exécution d'une sauvegarde
- `ProgressTracker` : gère uniquement le suivi de progression
- `FileJobRepository` : persiste uniquement les jobs

### Open/Closed Principle (OCP)
Le système est ouvert à l'extension mais fermé à la modification :
- Ajout d'un nouveau type de sauvegarde → nouvelle implémentation de `IBackupStrategy`
- Ajout d'un nouveau format de log → nouvelle implémentation de `ILogger`
- Aucune modification du code existant requise

### Liskov Substitution Principle (LSP)
Toutes les implémentations sont substituables à leurs interfaces :
- `FullBackupStrategy` et `DifferentialBackupStrategy` sont interchangeables via `IBackupStrategy`
- `JsonLogger` et `XmlLogger` sont substituables via `ILogger`

### Interface Segregation Principle (ISP)
Les interfaces sont spécifiques et non "grasses" :
- `IJobRepository` : uniquement les opérations CRUD sur les jobs
- `IStateStore` : uniquement la gestion de l'état
- `ILogger` : uniquement la journalisation

### Dependency Inversion Principle (DIP)
Les modules de haut niveau dépendent d'abstractions :
- `BackupOrchestrator` dépend de `IFileSystemGateway`, pas de `LocalFileSystemGateway`
- Les handlers dépendent de `ILogger` et `IStateStore`, pas des implémentations concrètes

---

## Design Patterns Utilisés

Notre conception exploite plusieurs patterns du Gang of Four (GoF) pour résoudre des problématiques spécifiques. Chaque pattern répond à un besoin architectural précis.

### Strategy Pattern (Comportemental)

| Aspect | Description |
|--------|-------------|
| **Problématique** | Comment permettre à l'algorithme de sélection des fichiers de varier indépendamment des clients qui l'utilisent ? Sans ce pattern, le code serait pollué par des conditionnelles (if/else) à chaque endroit où l'on doit sélectionner les fichiers. |
| **Solution** | Définir une famille d'algorithmes (Full, Différentiel), encapsuler chacun dans sa propre classe, et les rendre interchangeables via l'interface `IBackupStrategy`. |
| **Bénéfice EasySave** | L'ajout d'un nouveau type de sauvegarde (incrémentale, miroir) ne nécessite aucune modification du code existant - il suffit d'ajouter une nouvelle stratégie. |

### Observer Pattern (Comportemental)

| Aspect | Description |
|--------|-------------|
| **Problématique** | Comment notifier plusieurs composants (logger, state store, future GUI) lorsqu'un événement métier se produit, sans coupler fortement l'émetteur aux récepteurs ? Le service de backup ne devrait pas connaître tous ses consommateurs. |
| **Solution** | Définir une relation un-vers-plusieurs entre l'éditeur d'événements et les handlers. Quand un événement est publié, tous les abonnés sont notifiés automatiquement. |
| **Bénéfice EasySave** | L'ajout de nouveaux comportements en réaction aux événements (notifications, métriques, GUI temps réel) ne requiert que l'ajout d'un nouveau handler, sans toucher au code métier. |

### Command Pattern (Comportemental)

| Aspect | Description |
|--------|-------------|
| **Problématique** | Comment découpler l'interface utilisateur (CLI) de la logique métier ? Comment permettre l'ajout de nouvelles commandes sans modifier le parseur ? Comment préparer une future migration vers une GUI ? |
| **Solution** | Encapsuler chaque requête utilisateur dans un objet Command autonome, avec une interface uniforme `Execute()`. Le parseur délègue l'exécution sans connaître les détails. |
| **Bénéfice EasySave** | Ajout de nouvelles commandes par simple création d'une classe. La même architecture Command sera réutilisée pour la GUI WPF de la v2. |

### Factory Method Pattern (Créationnel)

| Aspect | Description |
|--------|-------------|
| **Problématique** | Comment créer des objets (stratégies, loggers) sans que les couches hautes ne connaissent les classes concrètes ? La couche Application ne doit pas dépendre de l'Infrastructure. |
| **Solution** | Définir une interface de création dans la couche Application, implémentée dans l'Infrastructure. Le client demande un objet via l'interface, sans savoir quelle classe concrète est instanciée. |
| **Bénéfice EasySave** | Respect strict de l'inversion de dépendance. Le changement d'implémentation (ex: nouveau logger cloud) ne touche que la factory, pas le code client. |

### Adapter Pattern (Structurel)

| Aspect | Description |
|--------|-------------|
| **Problématique** | Comment utiliser des composants existants (DLL EasyLog, APIs système) dont l'interface ne correspond pas à ce que notre application attend ? Comment isoler les dépendances externes ? |
| **Solution** | Créer une classe intermédiaire qui traduit l'interface attendue (Target) vers l'interface existante (Adaptee). Le client utilise l'interface uniforme sans connaître les détails d'implémentation. |
| **Bénéfice EasySave** | La DLL EasyLog peut être remplacée sans impact sur le reste de l'application. Les chemins sont normalisés de manière uniforme quel que soit l'OS. |

---

## Synthèse des Patterns et Principes

```
┌─────────────┬────────────────┬─────────────────────────────────────────────┐
│ Pattern GoF │   Catégorie    │           Rôle dans EasySave                │
├─────────────┼────────────────┼─────────────────────────────────────────────┤
│ Strategy    │ Comportemental │ Algorithme de sélection des fichiers        │
│             │                │ interchangeable (full vs différentiel)      │
├─────────────┼────────────────┼─────────────────────────────────────────────┤
│ Observer    │ Comportemental │ Notification des handlers quand un          │
│             │                │ événement domaine est publié                │
├─────────────┼────────────────┼─────────────────────────────────────────────┤
│ Command     │ Comportemental │ Encapsulation des requêtes CLI en objets    │
├─────────────┼────────────────┼─────────────────────────────────────────────┤
│ Factory     │ Créationnel    │ Création de IBackupStrategy et ILogger      │
│ Method      │                │ sans exposer les classes concrètes          │
├─────────────┼────────────────┼─────────────────────────────────────────────┤
│ Adapter     │ Structurel     │ Interface uniforme pour des implémentations │
│             │                │ variées (chemins, logging)                  │
└─────────────┴────────────────┴─────────────────────────────────────────────┘
```

---

## Arborescence des Livrables

```
Document/
├── README.md                          # Ce fichier
├── DSS_UC01_Gerer_Travaux.puml        # DSS : Gestion des travaux (CRUD)
├── DSS_UC02_Executer_Sauvegarde.puml  # DSS : Exécution d'une sauvegarde
├── DSS_UC03_Changer_Langue.puml       # DSS : Changement de langue
└── ClassDiagram.puml                  # Diagramme de classes complet
```

---

## Conclusion

Notre démarche de conception illustre un cheminement rigoureux : des fonctionnalités utilisateur aux DSS pour identifier les opérations système, puis de l'analyse du domaine métier au diagramme de classes respectant les principes de qualité logicielle. Le choix d'une architecture hexagonale hybride, combiné à l'utilisation judicieuse des design patterns GoF, garantit un code maintenable, testable et évolutif - des qualités essentielles pour un logiciel professionnel destiné à évoluer vers une version 2.0 avec interface graphique.
