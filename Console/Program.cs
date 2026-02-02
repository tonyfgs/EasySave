// See https://aka.ms/new-console-template for more information
using Easy.Log;
using Easy.Log.Interface;

Console.WriteLine("Hello, World!");
Console.WriteLine();
Console.WriteLine("--- Démarrage du test EasySave ---");

// 1. Définir l'endroit où on veut stocker les logs
// Directory.GetCurrentDirectory() pointe vers le dossier bin/Debug/net8.0/ lors du run
string logsDirectory = Path.Combine(Directory.GetCurrentDirectory(), "logs");

// 2. Instancier le Logger
// On utilise l'interface (ILogger) pour la déclaration, c'est une bonne pratique (polymorphisme)
ILogger myLogger = new DailyLogsService();

// 3. Créer une fausse donnée de log (Simule un travail de sauvegarde fini)
LogData testLog = new LogDataCustom()
{
    Timestamp = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"),
    BackupJobName = "Travail_Test_Alpha",
    SourceFilePath = @"C:\Utilisateurs\Moi\Documents\Projet.doc",
    TargetFilePath = @"D:\Backups\Projet.doc",
    FileSize = 4500,          // 4.5 Ko
    TransferTimeMs = 125,     // 125 ms
    EncryptionTimeMs = 0,
};

try
{
    myLogger.WriteInFile(logsDirectory, testLog);
    Console.WriteLine($"YEYYYYY");


}
catch (Exception ex)
{
    Console.WriteLine($"Erreur : {ex.Message}");
}