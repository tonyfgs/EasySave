using System.Text.Json;
using Logger.Interface;

namespace Logger.Service;

public class DailyLogsService : ILogger
{
    public void WriteInFile<T>(string path, T logData) where T : LogData
    {
        string? directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }
        string fileName = $"{DateTime.Now:yyyy-MM-dd}.json";
        string fullPath = Path.Combine(directory!, fileName);

        try
        {
            var options = new JsonSerializerOptions { WriteIndented = true };

            string jsonString = JsonSerializer.Serialize(logData, options);

            using (StreamWriter outputFile = new StreamWriter(fullPath, append: true))
            {
                outputFile.WriteLine(jsonString);
            }
        }
        catch
        {
            throw;
        }
    }
}