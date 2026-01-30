# 🛡️ EasySave - Solution de Sauvegarde ProSoft

![C#](https://img.shields.io/badge/C%23-239120?style=for-the-badge&logo=c-sharp&logoColor=white)
![.NET](https://img.shields.io/badge/.NET-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![Docker](https://img.shields.io/badge/Docker-2496ED?style=for-the-badge&logo=docker&logoColor=white)
![JSON](https://img.shields.io/badge/JSON-000000?style=for-the-badge&logo=json&logoColor=white)

**EasySave** est une suite logicielle robuste de gestion de sauvegardes sécurisées. Développée pour répondre aux besoins critiques des entreprises, elle assure la protection des données via un système multi-threadé performant.

---

## 🚀 Évolution Techniques & Livrables

Le projet a suivi une montée en puissance technologique découpée en 3 grandes étapes :

### 📊 Matrice des Versions
| Fonctionnalité | v1.1 | v2.0 | v3.0 (Final) |
| :--- | :---: | :---: | :---: |
| **Interface** | `Console` | `WPF UI` | `WPF UI` |
| **Mode** | Séquentiel | Séquentiel | **Parallèle ⚡** |
| **Cryptage** | ❌ | ✅ CryptoSoft | ✅ Mono-Instance |
| **Logs** | JSON / XML | JSON / XML | **Docker Centralized 🐳** |
| **Logiciel Métier** | ❌ | Blocage | **Pause Auto ⏸️** |

---

## ✨ Fonctionnalités Majeures (v3.0)

### 🏎️ Performance & Parallélisme
* **Multi-threading complet :** Exécution de plusieurs travaux de sauvegarde simultanément.
* **Priorisation intelligente :** Les extensions prioritaires passent toujours avant les autres tâches.
* **Contrôle de Bande Passante :** Un seuil de $n$ Ko empêche le transfert simultané de trop gros fichiers pour éviter la saturation réseau.

### 🔐 Sécurité Avancée
* **Intégration CryptoSoft :** Chiffrement des fichiers sensibles configurés par l'utilisateur.
* **Gestion Mono-Instance :** Sécurisation de l'accès à l'utilitaire de cryptage pour éviter les erreurs de corruption.
* **Diagnostic :** Mesure et log du temps de cryptage précis (en ms).

### 🖥️ Expérience Utilisateur (WPF)
* **Interface Intuitive :** Dashboard permettant de piloter (Play/Pause/Stop) chaque travail individuellement.
* **Monitoring Temps Réel :** Barre de progression et pourcentage d'avancement pour chaque tâche en cours.
* **Smart Pause :** Si un "Logiciel Métier" (ex: Calculatrice) est détecté, EasySave met les sauvegardes en pause pour libérer les ressources.

---

## 🛠️ Stack Technique

* **Logiciel :** `C#` / `.NET Core`
* **Interface :** `WPF` (Windows Presentation Foundation)
* **Conteneurisation :** `Docker` (Service de centralisation des logs)
* **Formats :** `JSON` & `XML`
* **Architecture :** `MVVM` (Model-View-ViewModel)

---

## ⚙️ Installation Rapide

1. **Cloner le projet**
   ```bash
   git clone [https://github.com/votre-username/EasySave.git](https://github.com/votre-username/EasySave.git)

---

Equipe de développement
<p align="center" >

<a href=""  style="margin-right: 20px;">
  <img src="img/David" width="50" height="50" title="David D'ALMEIDA" alt="David D'ALMEIDA"/>
</a>
<a href="https://codefirst.iut.uca.fr/git/tony.fages" style="margin-right: 20px;">
  <img src="img/Tony.png" width="50" height="50" title="Tony Fages" alt="Tony Fages"/>
</a>
<a href=""  style="margin-right: 20px;">
  <img src="img/Martin
Martin CAPARROS" width="50" height="50" title="Martin CAPARROS" alt="Martin CAPARROS"/>
</a>
<p>

---

En faisant ce TP, j'écoutais...  

<table>
    <tr>
        <td>
            <img src="./images/help.jpg" width="120"/>
        </td>
        <td>
            <div>
                <p><b>Help!</b></p>
                <p><i>The Beatles</i> (1965)</p>
            </div>
        </td>
    </tr>
</table>
<table>
    <tr>
        <td>
            <img src="./images/cry.jpg" width="120"/>
        </td>
        <td>
            <div>
                <p><b>Don't Cry</b></p>
                <p><i>Guns N' Roses</i> (1991)</p>
            </div>
        </td>
    </tr>
</table>
<table>
    <tr>
        <td>
            <img src="./images/queen.jpg" width="120"/>
        </td>
        <td>
            <div>
                <p><b>Bohemian Rhapsody</b></p>
                <p><i>Queen</i> (1975)</p>
            </div>
        </td>
    </tr>
</table>

<table>
    <tr>
        <td>
            <img src="./images/sos.jpg" width="120"/>
        </td>
        <td>
            <div>
                <p><b>Tous les cris les S.O.S</b></p>
                <p><i>Daniel Balavoine</i> (1985)</p>
            </div>
        </td>
    </tr>
</table>

