// See https://aka.ms/new-console-template for more information
using Easy.Log;
using Easy.Log.Interface;

Console.WriteLine("Hello, World!");
Console.WriteLine();
Console.WriteLine("--- Démarrage du test  ---");

string logsDirectory = Path.Combine(Directory.GetCurrentDirectory(), "logs");


ILogger myLogger = new DailyLogsService();

var testLog = new LogData()
{
    Timestamp = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"),
    BackupJobName = "Travail_Test_Alpha",
    SourceFilePath = @"C:\Utilisateurs\Moi\Documents\Projet.doc",
    TargetFilePath = @"D:\Backups\Projet.doc",
    FileSize = 4500,
    TransferTimeMs = 125,
    EncryptionTimeMs = 0,
};

try
{
    myLogger.WriteInFile(logsDirectory, testLog);
}
catch (Exception ex)
{
    Console.WriteLine($"Erreur : {ex.Message}");
}