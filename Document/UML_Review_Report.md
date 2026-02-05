# Rapport d'Audit UML - EasySave v1.0 Class Diagram

**Document**: Revue experte du diagramme de classes
**Fichier audite**: `Document/ClassDiagram.puml`
**Date**: 2026-02-05
**Norme de reference**: UML 2.5.1 (OMG formal/2017-12-05)

---

## Table des matieres

1. [Synthese executive](#1-synthese-executive)
2. [Rappel normatif UML 2.5.1](#2-rappel-normatif-uml-251)
3. [Inventaire exhaustif des relations](#3-inventaire-exhaustif-des-relations)
4. [Erreurs critiques](#4-erreurs-critiques)
5. [Erreurs majeures](#5-erreurs-majeures)
6. [Points debattables](#6-points-debattables)
7. [Multiplicites manquantes](#7-multiplicites-manquantes)
8. [Labels de role manquants](#8-labels-de-role-manquants)
9. [Relations manquantes](#9-relations-manquantes)
10. [Observations architecturales](#10-observations-architecturales)
11. [Synthese des corrections](#11-synthese-des-corrections)

---

## 1. Synthese executive

### 1.1 Inventaire reel du diagramme

| Categorie                    | Nombre | Detail                          |
|------------------------------|--------|---------------------------------|
| Classes                      | 38     |                                 |
| Interfaces                   | 12     |                                 |
| Enumerations                 | 4      | Language, LogFormat, BackupType, JobState |
| **Total classifieurs**       | **54** |                                 |
| Realisations (`<\|..`)       | 22     |                                 |
| Agregations (`o--`)          | 2      |                                 |
| Dependances (`..>`)          | 2      |                                 |
| Associations (`-->`)         | 41     |                                 |
| **Total relations**          | **67** |                                 |

### 1.2 Bilan des problemes identifies

| Severite            | Nombre | Description                                        |
|---------------------|--------|----------------------------------------------------|
| **Critique**        | 2      | Composition manquante (Event → VO)                 |
| **Majeure**         | 14     | Association au lieu de dependance                   |
| **Debattable**      | 6      | Enums (5) + ProgressTracker (1) : convention       |
| **Mineure**         | 26     | Multiplicites manquantes (sur relations restantes)  |
| **Manquante**       | 2      | Relations absentes du diagramme                     |
| **Structurelle**    | 1      | Relation semantiquement incorrecte (generics)       |
| **Labels**          | ~20    | Labels de role manquants sur les liens cles         |

### 1.3 Verdict

Le diagramme a une bonne architecture (Clean Architecture / DDD). Les realisations d'interface et les deux agregations sont correctes. Le probleme principal est l'utilisation systematique de `-->` (association dirigee) pour **toutes** les relations non-interface, sans distinguer composition, dependance et association.

---

## 2. Rappel normatif UML 2.5.1

### 2.1 Definitions normatives (OMG)

#### Association (UML 2.5.1, section 11.5)

> An Association classifies a set of tuples representing links between typed instances.

En pratique : A stocke une reference vers B comme **attribut persistant**. La relation dure dans le temps. En PlantUML : `-->` (dirigee) ou `--` (bidirectionnelle).

#### Composition (UML 2.5.1, section 9.9.17)

> Composite aggregation is a strong form of aggregation that requires a part object be included in at most one composite object at a time. If a composite object is deleted, all of its part objects that are objects are normally deleted with it.

Criteres :
- B fait partie integrante de A (relation whole-part)
- B ne peut appartenir qu'a **un seul** A a la fois
- Destruction de A => destruction de B
- En PlantUML : `*--`

#### Agregation (UML 2.5.1, section 9.5.3)

> Indicates that the Property has shared aggregation semantics. Precise semantics of shared aggregations varies by application area and modeler.

Criteres :
- A "possede" B mais B peut exister independamment
- B peut etre partage entre plusieurs A
- En PlantUML : `o--`

#### Dependance (UML 2.5.1, section 7.7)

> A Dependency is a Relationship that signifies that a single model Element or a set of model Elements requires other model Elements for their specification or implementation.

Criteres :
- B est un parametre de methode de A
- B est un type de retour de A
- A cree B localement dans une methode sans le stocker
- Aucun lien structurel persistant
- En PlantUML : `..>`

#### Realisation (UML 2.5.1, section 7.7.4)

> An InterfaceRealization is a specialized Realization between a BehavioredClassifier and an Interface.

En PlantUML : `<|..`

### 2.2 Cas particulier : Enums / DataTypes

Les enumerations sont des `DataType` en UML (section 10.2) :

> A DataType is a type whose instances are identified only by their value.

Pour les attributs de type enum, trois representations sont toutes **valides** :

1. **Attribut textuel** : ecrire `- type: BackupType` dans la classe (deja fait)
2. **Association dirigee** `-->` : montre que la classe utilise le type (acceptable)
3. **Composition** `*--` : defensible car la valeur est "contenue" sans identite propre

La norme ne tranche pas formellement. En pratique, `-->` pour les enums est la convention la plus repandue. Composer un enum est debattable car les litteraux d'enum sont des valeurs partagees (`BackupType.FULL` est la meme valeur pour tous les objets).

### 2.3 Arbre de decision

```
La classe A a-t-elle un ATTRIBUT (champ) de type B ?
  |
  +-- NON: B n'apparait que dans les signatures de methodes
  |       => DEPENDANCE (..>)
  |
  +-- OUI: A stocke une reference vers B
        |
        +-- B est un objet a identite propre ?
        |     |
        |     +-- A cree et controle le cycle de vie de B ?
        |     |     => COMPOSITION (*--)
        |     |
        |     +-- B est injecte / partage ?
        |           => ASSOCIATION (-->) ou AGREGATION (o--)
        |
        +-- B est un DataType / enum / ValueObject sans identite ?
              => COMPOSITION (*--) ou ASSOCIATION (-->)
                 selon la convention du projet
```

---

## 3. Inventaire exhaustif des relations

### 3.1 Realisations (22) - TOUTES CORRECTES

| #  | Ligne | Relation                                        | Verdict |
|----|-------|-------------------------------------------------|---------|
| R1 | 438   | `IDomainEvent <\|.. StateChangedEvent`          | OK      |
| R2 | 439   | `IDomainEvent <\|.. TransferCompletedEvent`     | OK      |
| R3 | 440   | `IBackupStrategy <\|.. FullBackupStrategy`      | OK      |
| R4 | 441   | `IBackupStrategy <\|.. DifferentialBackupStrategy` | OK   |
| R5 | 448   | `IDomainEventHandler <\|.. TransferCompletedEventHandler` | OK |
| R6 | 449   | `IDomainEventHandler <\|.. StateChangedEventHandler` | OK |
| R7 | 469   | `IEasyLogger <\|.. DailyLogsService`            | OK      |
| R8 | 473   | `IJobRepository <\|.. FileJobRepository`        | OK      |
| R9 | 474   | `IStateStore <\|.. FileStateStore`               | OK      |
| R10| 475   | `ILogger <\|.. JsonLogger`                      | OK      |
| R11| 476   | `ILogger <\|.. XmlLogger`                       | OK      |
| R12| 477   | `IFileSystemGateway <\|.. LocalFileSystemGateway` | OK   |
| R13| 478   | `IPathAdapter <\|.. CanonicalPathAdapter`       | OK      |
| R14| 479   | `IBackupStrategyFactory <\|.. BackupStrategyFactory` | OK |
| R15| 480   | `IEventPublisher <\|.. InProcessEventPublisher` | OK      |
| R16| 492   | `ICommand <\|.. CreateJobCommand`               | OK      |
| R17| 493   | `ICommand <\|.. ExecuteJobCommand`              | OK      |
| R18| 494   | `ICommand <\|.. DeleteJobCommand`               | OK      |
| R19| 495   | `ICommand <\|.. ListJobsCommand`                | OK      |
| R20| 496   | `ICommand <\|.. ModifyJobCommand`               | OK      |
| R21| 497   | `ICommand <\|.. ChangeLanguageCommand`          | OK      |
| R22| 498   | `ICommand <\|.. ExitCommand`                    | OK      |

### 3.2 Agregations (2) - TOUTES CORRECTES

| #  | Ligne | Relation                                                  | Verdict |
|----|-------|-----------------------------------------------------------|---------|
| A1 | 484   | `InProcessEventPublisher "1" o-- "*" IDomainEventHandler` | OK      |
| A2 | 500   | `ConsoleUI "1" o-- "*" ICommand`                          | OK      |

**Justification** : Les handlers et les commandes sont injectes (pas crees par le conteneur) et pourraient theoriquement exister independamment. L'agregation avec multiplicites est semantiquement correcte.

### 3.3 Dependances (2) - CORRECTES

| #  | Ligne | Relation                                       | Verdict |
|----|-------|------------------------------------------------|---------|
| D1 | 485   | `BackupStrategyFactory ..> IBackupStrategy : creates` | OK |
| D2 | 487   | `LoggerFactory ..> ILogger : creates`          | OK      |

### 3.4 Associations (41) - AUDIT DETAILLE

| #   | Ligne | Relation actuelle                              | Attribut? | Verdict     | Correction                             |
|-----|-------|------------------------------------------------|-----------|-------------|----------------------------------------|
| S1  | 442   | `BackupJob --> BackupType`                     | Oui (`- type`) | DEBATTABLE | `*--` ou `-->` selon convention    |
| S2  | 443   | `StateInfo --> JobState`                       | Oui (`+ State`) | DEBATTABLE | `*--` ou `-->` selon convention   |
| S3  | 444   | `StateChangedEvent --> StateInfo`              | Oui (`+ State`) | CRITIQUE | `*--` composition                    |
| S4  | 445   | `TransferCompletedEvent --> TransferInfo`      | Oui (`+ Transfer`) | CRITIQUE | `*--` composition                |
| S5  | 450   | `TransferCompletedEventHandler --> ILogger`    | Oui (`- logger`) | OK | Association (injection)               |
| S6  | 451   | `StateChangedEventHandler --> IStateStore`     | Oui (`- stateStore`) | OK | Association (injection)          |
| S7  | 453   | `BackupOrchestrator --> IFileSystemGateway`    | Oui (`- fileSystem`) | OK | Association (injection)          |
| S8  | 454   | `BackupOrchestrator --> IPathAdapter`          | Oui (`- pathAdapter`) | OK | Association (injection)         |
| S9  | 455   | `BackupOrchestrator --> IBackupStrategyFactory`| Oui (`- strategyFactory`) | OK | Association (injection)    |
| S10 | 456   | `BackupOrchestrator --> IEventPublisher`       | Oui (`- eventPublisher`) | OK | Association (injection)      |
| S11 | 457   | `BackupOrchestrator --> ProgressTracker`       | Oui (`- tracker`) | DEBATTABLE | `*--` ou `-->` selon cycle de vie |
| S12 | 458   | `BackupOrchestrator --> BackupJob`             | Non (parametre) | MAJEURE | `..>` dependance                    |
| S13 | 459   | `BackupOrchestrator --> BackupResult`          | Non (retour) | MAJEURE | `..>` dependance                       |
| S14 | 460   | `BackupOrchestrator --> FileDescriptor`        | Non (local) | MAJEURE | `..>` dependance                        |
| S15 | 462   | `BackupApplicationService --> IJobRepository`  | Oui (`- repository`) | OK | Association (injection)         |
| S16 | 463   | `BackupApplicationService --> BackupOrchestrator` | Oui (`- orchestrator`) | OK | Association (injection)     |
| S17 | 465   | `ProgressTracker --> FileDescriptor`           | Non (parametre) | MAJEURE | `..>` dependance                    |
| S18 | 466   | `ProgressTracker --> StateInfo`                | Non (retour) | MAJEURE | `..>` dependance                       |
| S19 | 470   | `DailyLogsService --> LogData`                 | Non (generique) | STRUCTURELLE | Voir section 4.4              |
| S20 | 482   | `JsonLogger --> IEasyLogger : delegates to`    | Oui (`- easyLogger`) | OK | Association (injection/delegation) |
| S21 | 486   | `LoggerFactory --> ConfigurationManager`       | Oui (`- configManager`) | OK | Association (injection)       |
| S22 | 488   | `ConfigurationManager --> LogFormat`           | Implicite (get/set) | DEBATTABLE | `*--` ou `-->` (enum)         |
| S23 | 489   | `ConfigurationManager --> Language`            | Implicite (get/set) | DEBATTABLE | `*--` ou `-->` (enum)         |
| S24 | 501   | `ConsoleUI --> CommandLineParser`              | Oui (`- parser`) | OK | Association (potentiellement `*--`)   |
| S25 | 502   | `ConsoleUI --> LanguageManager`                | Oui (`- languageManager`) | OK | Association (partage)          |
| S26 | 503   | `CommandLineParser --> ParsedCommand`          | Non (retour) | MAJEURE | `..>` dependance                       |
| S27 | 505   | `CreateJobCommand --> BackupApplicationService`| Oui (`- appService`) | OK | Association (injection)          |
| S28 | 506   | `ExecuteJobCommand --> BackupApplicationService`| Oui (`- appService`) | OK | Association (injection)         |
| S29 | 507   | `DeleteJobCommand --> BackupApplicationService`| Oui (`- appService`) | OK | Association (injection)          |
| S30 | 508   | `ListJobsCommand --> BackupApplicationService` | Oui (`- appService`) | OK | Association (injection)          |
| S31 | 509   | `ModifyJobCommand --> BackupApplicationService`| Oui (`- appService`) | OK | Association (injection)          |
| S32 | 510   | `ChangeLanguageCommand --> LanguageManager`    | Oui (`- languageManager`) | OK | Association (injection)      |
| S33 | 511   | `LanguageManager --> Language`                 | Oui (`- currentLanguage`) | DEBATTABLE | `*--` ou `-->` (enum)    |
| S34 | 514   | `Program --> ConsoleUI`                        | Non (local static) | MAJEURE | `..>` dependance                 |
| S35 | 515   | `Program --> BackupApplicationService`         | Non (local static) | MAJEURE | `..>` dependance                 |
| S36 | 516   | `Program --> BackupOrchestrator`               | Non (local static) | MAJEURE | `..>` dependance                 |
| S37 | 517   | `Program --> ConfigurationManager`             | Non (local static) | MAJEURE | `..>` dependance                 |
| S38 | 518   | `Program --> LoggerFactory`                    | Non (local static) | MAJEURE | `..>` dependance                 |
| S39 | 519   | `Program --> InProcessEventPublisher`          | Non (local static) | MAJEURE | `..>` dependance                 |
| S40 | 520   | `Program --> TransferCompletedEventHandler`    | Non (local static) | MAJEURE | `..>` dependance                 |
| S41 | 521   | `Program --> StateChangedEventHandler`         | Non (local static) | MAJEURE | `..>` dependance                 |

---

## 4. Erreurs critiques

### 4.1 S3 : `StateChangedEvent --> StateInfo` (ligne 444)

**Probleme** : `StateChangedEvent` possede l'attribut `+ State: StateInfo`. Le `StateInfo` est un Value Object immutable cree specifiquement pour cet evenement. Quand l'evenement est detruit, le StateInfo n'a plus de raison d'exister.

**Norme** : UML 2.5.1 section 9.9.17 - L'objet compose est inclus dans au plus un composite a la fois.

**Correction** :
```plantuml
StateChangedEvent "1" *-- "1" StateInfo
```

### 4.2 S4 : `TransferCompletedEvent --> TransferInfo` (ligne 445)

**Probleme** : Identique a S3. `TransferCompletedEvent` possede `+ Transfer: TransferInfo`.

**Correction** :
```plantuml
TransferCompletedEvent "1" *-- "1" TransferInfo
```

---

## 5. Erreurs majeures

### 5.1 Dependances deguisees en associations (Application Layer)

Ces relations pointent vers des types qui n'apparaissent que comme **parametres ou retours de methodes**, jamais comme attributs stockes.

| #   | Relation                              | Preuve                                             | Correction                           |
|-----|---------------------------------------|-----------------------------------------------------|--------------------------------------|
| S12 | `BackupOrchestrator --> BackupJob`    | Parametre de `Execute(job: BackupJob)`              | `..> BackupJob : receives`           |
| S13 | `BackupOrchestrator --> BackupResult` | Retour de `Execute(): BackupResult`                 | `..> BackupResult : returns`         |
| S14 | `BackupOrchestrator --> FileDescriptor`| Variable locale dans methodes privees              | `..> FileDescriptor : processes`     |
| S17 | `ProgressTracker --> FileDescriptor`  | Parametre de `Initialize()` et `UpdateProgress()`   | `..> FileDescriptor : receives`      |
| S18 | `ProgressTracker --> StateInfo`       | Retour de `BuildState(): StateInfo`                 | `..> StateInfo : builds`             |
| S26 | `CommandLineParser --> ParsedCommand` | Retour de `Parse(): ParsedCommand`                  | `..> ParsedCommand : creates`        |

### 5.2 Composition Root : `Program --> *` (8 relations)

**Probleme** : `Program` n'a que des methodes `{static}` et **aucun attribut d'instance**. Il cree tous les composants dans `BuildCompositionRoot()` comme variables locales puis les injecte. Ce ne sont pas des associations (pas de reference persistante).

| #   | Relation actuelle                         | Correction                                       |
|-----|-------------------------------------------|--------------------------------------------------|
| S34 | `Program --> ConsoleUI`                   | `Program ..> ConsoleUI : creates`                |
| S35 | `Program --> BackupApplicationService`    | `Program ..> BackupApplicationService : creates` |
| S36 | `Program --> BackupOrchestrator`          | `Program ..> BackupOrchestrator : creates`       |
| S37 | `Program --> ConfigurationManager`        | `Program ..> ConfigurationManager : creates`     |
| S38 | `Program --> LoggerFactory`               | `Program ..> LoggerFactory : creates`            |
| S39 | `Program --> InProcessEventPublisher`     | `Program ..> InProcessEventPublisher : creates`  |
| S40 | `Program --> TransferCompletedEventHandler`| `Program ..> TransferCompletedEventHandler : creates` |
| S41 | `Program --> StateChangedEventHandler`    | `Program ..> StateChangedEventHandler : creates` |

### 5.3 S19 : `DailyLogsService --> LogData` (ligne 470) - Erreur structurelle

**Probleme** : La methode de `DailyLogsService` est **generique** :
```
+ WriteInFile<T>(path: string, logData: T): void
```

Avec un type generique `<T>`, `DailyLogsService` ne connait **pas** le type `LogData`. Il n'en depend pas. La relation est semantiquement fausse.

C'est l'**appelant** (typiquement `JsonLogger`) qui connait `LogData` et le passe en tant que `T`.

**Correction** : Supprimer cette relation. Ajouter eventuellement :
```plantuml
JsonLogger ..> LogData : maps TransferInfo to
```

---

## 6. Points debattables

### 6.1 Enums montres comme associations

Ces 5 relations utilisent `-->` pour relier une classe a un enum qu'elle stocke comme attribut :

| #   | Relation                            | Attribut concerne          |
|-----|-------------------------------------|----------------------------|
| S1  | `BackupJob --> BackupType`          | `- type: BackupType`       |
| S2  | `StateInfo --> JobState`            | `+ State: JobState`        |
| S22 | `ConfigurationManager --> LogFormat` | Implicite (get/set)       |
| S23 | `ConfigurationManager --> Language`  | Implicite (get/set)       |
| S33 | `LanguageManager --> Language`       | `- currentLanguage: Language` |

**Analyse UML 2.5.1** : Les enumerations sont des `DataType` (section 10.2). Leurs instances sont identifiees par leur valeur, pas par une identite. La norme ne prescrit pas de relation specifique. Trois conventions coexistent :

| Convention      | Argument pour                                    | Argument contre                           |
|-----------------|--------------------------------------------------|-------------------------------------------|
| `-->` association | Le plus repandu en pratique. L'attribut est deja montre dans la classe. | Ne distingue pas "utilise" de "possede" |
| `*--` composition | La valeur est "contenue" sans identite propre | Les litteraux d'enum sont des valeurs partagees ; composition implique exclusivite d'instance |
| Pas de relation | L'attribut type est deja lisible dans la classe | Perd la visibilite de la dependance     |

**Recommandation** : `-->` est **acceptable**. Si votre convention de projet prefere `*--` pour les DataTypes, appliquez-la uniformement. L'important est la **coherence** : choisir une convention et la garder.

### 6.2 `BackupOrchestrator --> ProgressTracker` (S11)

`BackupOrchestrator` a `- tracker: ProgressTracker` comme attribut. Le diagramme ne montre pas explicitement qui cree le `ProgressTracker` ni son cycle de vie.

- Si l'orchestrateur **cree et detruit** le tracker en interne → composition `*--`
- Si le tracker est **injecte** par le Composition Root → association `-->`

Sans preuve explicite dans le diagramme (pas de constructeur visible, pas de note), les deux sont defensibles. L'association actuelle n'est pas fausse.

**Si composition** :
```plantuml
BackupOrchestrator "1" *-- "1" ProgressTracker : tracks
```

**Si injection** :
```plantuml
BackupOrchestrator "1" --> "1" ProgressTracker : tracks
```

### 6.3 `ConsoleUI --> CommandLineParser` (S24)

`ConsoleUI` a `- parser: CommandLineParser` comme attribut. Deux interpretations :

- Si ConsoleUI **cree** le parser en interne → composition `*--`
- Si le parser est **injecte** → association `-->`

Le diagramme ne permet pas de trancher. Association est un choix par defaut raisonnable.

---

## 7. Multiplicites manquantes

### 7.1 Etat des lieux

Sur les 41 associations (`-->`), **aucune** ne porte de multiplicite. Seules les 2 agregations (A1, A2) en ont.

Apres application des corrections des sections 4 et 5 :
- 14 associations deviennent des dependances (`..>`) → pas de multiplicite requise
- 1 association est supprimee (S19) → pas de multiplicite
- **26 relations** restent comme associations ou compositions et **necessitent des multiplicites**

C'est ce chiffre de **26** qui est reporte en section 1.2 comme "mineures".

### 7.2 Regles UML pour les multiplicites

En UML 2.5.1, l'absence de multiplicite signifie **non specifiee** (pas "1"). C'est tolere mais imprecis. Les best practices recommandent de toujours specifier la multiplicite sur les associations et compositions.

Les dependances (`..>`) n'ont **pas besoin** de multiplicites (pas de lien structurel).

### 7.3 Multiplicites recommandees

#### Injections de dependances (1 vers 1)

Toutes les dependances injectees via constructeur sont `"1" --> "1"` :

```plantuml
BackupOrchestrator "1" --> "1" IFileSystemGateway
BackupOrchestrator "1" --> "1" IPathAdapter
BackupOrchestrator "1" --> "1" IBackupStrategyFactory
BackupOrchestrator "1" --> "1" IEventPublisher
BackupApplicationService "1" --> "1" IJobRepository
BackupApplicationService "1" --> "1" BackupOrchestrator
TransferCompletedEventHandler "1" --> "1" ILogger
StateChangedEventHandler "1" --> "1" IStateStore
JsonLogger "1" --> "1" IEasyLogger
LoggerFactory "1" --> "1" ConfigurationManager
ConsoleUI "1" --> "1" CommandLineParser
ConsoleUI "1" --> "1" LanguageManager
CreateJobCommand "1" --> "1" BackupApplicationService
ExecuteJobCommand "1" --> "1" BackupApplicationService
DeleteJobCommand "1" --> "1" BackupApplicationService
ListJobsCommand "1" --> "1" BackupApplicationService
ModifyJobCommand "1" --> "1" BackupApplicationService
ChangeLanguageCommand "1" --> "1" LanguageManager
```

#### Compositions (1 vers 1 ou 1 vers *)

```plantuml
StateChangedEvent "1" *-- "1" StateInfo
TransferCompletedEvent "1" *-- "1" TransferInfo
```

#### Enums (si gardes comme associations)

```plantuml
BackupJob "1" --> "1" BackupType
StateInfo "1" --> "1" JobState
ConfigurationManager "1" --> "1" LogFormat
ConfigurationManager "1" --> "1" Language
LanguageManager "1" --> "1" Language
```

#### Collections (deja definies dans les agregations)

```plantuml
InProcessEventPublisher "1" o-- "*" IDomainEventHandler       ' deja correct
ConsoleUI "1" o-- "1..*" ICommand                             ' suggestion: 1..* au lieu de *
```

**Note** : `ConsoleUI o-- "*" ICommand` pourrait etre `"1..*"` car une UI sans aucune commande n'a pas de sens fonctionnel.

---

## 8. Labels de role manquants

### 8.1 Pourquoi les labels importent

Un label de role sur une association (texte apres `:` en PlantUML) explicite la **semantique** de la relation. Sans label, le lecteur doit deviner pourquoi A connait B. C'est une best practice UML pour les diagrammes de conception detaillee.

### 8.2 Labels recommandes sur les associations existantes

#### Associations d'injection (couche Application)

```plantuml
BackupOrchestrator "1" --> "1" IFileSystemGateway : uses
BackupOrchestrator "1" --> "1" IPathAdapter : uses
BackupOrchestrator "1" --> "1" IBackupStrategyFactory : uses
BackupOrchestrator "1" --> "1" IEventPublisher : publishes via
BackupApplicationService "1" --> "1" IJobRepository : queries
BackupApplicationService "1" --> "1" BackupOrchestrator : delegates to
TransferCompletedEventHandler "1" --> "1" ILogger : writes to
StateChangedEventHandler "1" --> "1" IStateStore : updates
```

#### Associations d'injection (couche Infrastructure)

```plantuml
JsonLogger "1" --> "1" IEasyLogger : delegates to   ' deja present
LoggerFactory "1" --> "1" ConfigurationManager : reads
```

#### Associations d'injection (couche Presentation)

```plantuml
ConsoleUI "1" --> "1" CommandLineParser : parses with
ConsoleUI "1" --> "1" LanguageManager : translates with
CreateJobCommand "1" --> "1" BackupApplicationService : calls
ExecuteJobCommand "1" --> "1" BackupApplicationService : calls
DeleteJobCommand "1" --> "1" BackupApplicationService : calls
ListJobsCommand "1" --> "1" BackupApplicationService : calls
ModifyJobCommand "1" --> "1" BackupApplicationService : calls
ChangeLanguageCommand "1" --> "1" LanguageManager : configures
```

#### Dependances (deja corrigees en section 5)

```plantuml
BackupOrchestrator ..> BackupJob : receives
BackupOrchestrator ..> BackupResult : returns
BackupOrchestrator ..> FileDescriptor : processes
ProgressTracker ..> FileDescriptor : receives
ProgressTracker ..> StateInfo : builds
CommandLineParser ..> ParsedCommand : creates
BackupStrategyFactory ..> IBackupStrategy : creates     ' deja present
LoggerFactory ..> ILogger : creates                     ' deja present
```

#### Compositions et relations manquantes

```plantuml
StateChangedEvent "1" *-- "1" StateInfo : carries
TransferCompletedEvent "1" *-- "1" TransferInfo : carries
FileStateStore "1" *-- "*" StateInfo : stores
FileJobRepository "1" *-- "0..5" BackupJob : stores
```

### 8.3 Vocabulaire de labels recommande

| Label       | Usage                                                |
|-------------|------------------------------------------------------|
| `uses`      | Relation generique d'utilisation                     |
| `creates`   | Factory qui instancie un objet                       |
| `stores`    | Repository / store qui persiste des objets           |
| `receives`  | Objet recu en parametre                              |
| `returns`   | Objet cree et retourne par une methode               |
| `builds`    | Objet construit a partir d'un etat interne           |
| `processes` | Objet manipule pendant un traitement                 |
| `calls`     | Delegation d'appel vers un service                   |
| `delegates to` | Delegation explicite (pattern Delegation)         |
| `publishes via` | Publication d'evenements via un publisher         |
| `updates`   | Modification d'etat dans un store                    |
| `writes to` | Ecriture dans un log / fichier                       |
| `reads`     | Lecture de configuration                             |
| `carries`   | Evenement qui transporte un payload                  |
| `configures`| Modification de parametres                           |
| `translates with` | Traduction via un gestionnaire de langue       |
| `parses with` | Parsing de commandes                              |
| `tracks`    | Suivi de progression                                 |

---

## 9. Relations manquantes

### 9.1 `FileStateStore` vers `StateInfo`

`FileStateStore` declare l'attribut `- states: List<StateInfo>`. Cette relation de stockage n'apparait nulle part dans le diagramme.

**Correction** :
```plantuml
FileStateStore "1" *-- "*" StateInfo
```

Composition car le store cree et detruit les StateInfo qu'il stocke.

### 9.2 `FileJobRepository` vers `BackupJob`

`FileJobRepository` stocke des jobs (methodes `PersistJobs()`, `LoadJobs()`, `GetAll(): List<BackupJob>`). Cette relation structurelle est absente.

**Correction** :
```plantuml
FileJobRepository "1" *-- "0..5" BackupJob
```

`0..5` car `BackupJob.MAX_JOBS = 5` (contrainte metier v1.0).

### 9.3 Dependances implicites non montrees (optionnel)

Les interfaces de port utilisent des types domaine dans leurs signatures (ex: `IJobRepository.Save(job: BackupJob)`). Ces dependances sont **implicites** et lisibles dans les signatures. Les montrer ou non est un choix de lisibilite.

**Ne pas les montrer** est une pratique **valide et courante** pour eviter de surcharger le diagramme. Ce n'est pas une erreur.

---

## 10. Observations architecturales

### 10.1 Couplage concret (hors perimetre UML strict)

Certaines associations pointent vers des **classes concretes** au lieu d'interfaces :

| Relation                                        | Remarque                                              |
|-------------------------------------------------|-------------------------------------------------------|
| `BackupApplicationService --> BackupOrchestrator`| Couplage concret ; pourrait passer par une interface |
| `ConsoleUI --> CommandLineParser`                | Couplage concret                                     |
| `ConsoleUI --> LanguageManager`                  | Couplage concret                                     |

Ce n'est pas une erreur UML mais une violation potentielle du Dependency Inversion Principle (DIP). Cela peut compliquer les tests unitaires (mocking).

### 10.2 Stereotypes manquants

| Classe                | Stereotype actuel | Suggestion                                       |
|-----------------------|-------------------|--------------------------------------------------|
| `ProgressTracker`     | Aucun             | Laisser vide (service interne)                   |
| `CommandLineParser`   | Aucun             | `<<utility>>` si stateless                       |
| `CanonicalPathAdapter`| Aucun             | `<<utility>>` (confirme par la note "stateless") |
| `BackupStrategyFactory`| Aucun            | `<<factory>>`                                    |
| `LoggerFactory`       | Aucun             | `<<factory>>`                                    |
| `LogData`             | Aucun             | `<<ValueObject>>` ou `<<DTO>>`                   |

### 10.3 EasyLog DLL : coherence du modele generique

`DailyLogsService.WriteInFile<T>()` est generique, mais `LogData` existe dans le meme package. Si `DailyLogsService` est vraiment generique et decouple de `LogData`, alors :
- `LogData` devrait etre dans le package **Infrastructure** (c'est un DTO de mapping)
- Ou `DailyLogsService` n'est pas generique et prend directement `LogData`

Le diagramme actuel est ambigu sur ce point.

---

## 11. Synthese des corrections

### 11.1 Corrections obligatoires (Critiques + Majeures)

```plantuml
' ===== CRITIQUES : Compositions (lignes 444, 445) =====
StateChangedEvent "1" *-- "1" StateInfo : carries
TransferCompletedEvent "1" *-- "1" TransferInfo : carries

' ===== MAJEURES : Dependances au lieu d'associations =====
' Remplacer --> par ..> (lignes 458-460, 465-466, 503)
BackupOrchestrator ..> BackupJob : receives
BackupOrchestrator ..> BackupResult : returns
BackupOrchestrator ..> FileDescriptor : processes
ProgressTracker ..> FileDescriptor : receives
ProgressTracker ..> StateInfo : builds
CommandLineParser ..> ParsedCommand : creates

' ===== MAJEURES : Program (Composition Root) =====
' Remplacer --> par ..> (lignes 514-521)
Program ..> ConsoleUI : creates
Program ..> BackupApplicationService : creates
Program ..> BackupOrchestrator : creates
Program ..> ConfigurationManager : creates
Program ..> LoggerFactory : creates
Program ..> InProcessEventPublisher : creates
Program ..> TransferCompletedEventHandler : creates
Program ..> StateChangedEventHandler : creates

' ===== STRUCTURELLE : Supprimer (ligne 470) =====
' SUPPRIMER: DailyLogsService --> LogData
' AJOUTER (optionnel):
JsonLogger ..> LogData : maps TransferInfo to
```

### 11.2 Relations manquantes a ajouter

```plantuml
FileStateStore "1" *-- "*" StateInfo : stores
FileJobRepository "1" *-- "0..5" BackupJob : stores
```

### 11.3 Multiplicites a ajouter

Ajouter `"1" --> "1"` sur les 18 associations d'injection listees en section 7.3.

### 11.4 Labels de role a ajouter

Ajouter les labels `: role` sur les ~20 liens cles listes en section 8.2. Priorite aux associations d'injection et aux dependances.

### 11.5 Debattable (convention projet)

**S11 - ProgressTracker** : choisir composition ou association selon le cycle de vie reel (voir section 6.2).

**Enums** : si le projet adopte la convention `*--` pour les enums :
```plantuml
BackupJob "1" *-- "1" BackupType
StateInfo "1" *-- "1" JobState
ConfigurationManager "1" *-- "1" LogFormat
ConfigurationManager "1" *-- "1" Language
LanguageManager "1" *-- "1" Language
```

---

## Checklist de validation

- [ ] Les 2 compositions critiques sont corrigees (S3, S4)
- [ ] Les 6 dependances Application sont corrigees (S12-S14, S17-S18, S26)
- [ ] Les 8 dependances Program sont corrigees (S34-S41)
- [ ] La relation DailyLogsService --> LogData est supprimee (S19)
- [ ] Les 2 relations manquantes sont ajoutees (FileStateStore, FileJobRepository)
- [ ] Les 26 multiplicites sont ajoutees sur les associations et compositions restantes
- [ ] Les labels de role sont ajoutes sur les ~20 liens cles
- [ ] Convention S11 (ProgressTracker) choisie et appliquee
- [ ] Convention enum choisie et appliquee uniformement
- [ ] ConsoleUI `o--` ICommand : multiplicite `"1..*"` au lieu de `"*"`

---

*Rapport audite et corrige - Norme UML 2.5.1 (OMG formal/2017-12-05)*
