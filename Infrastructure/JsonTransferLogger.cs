using System.Text.Json;
using Application.DTOs;
using Application.Ports;

namespace Infrastructure;

public class JsonTransferLogger : ITransferLogger
{
    private readonly string _logDirectory;

    public JsonTransferLogger(string logDirectory)
    {
        _logDirectory = logDirectory;
    }

    public void LogTransfer(TransferLog log)
    {
        Directory.CreateDirectory(_logDirectory);
        var filePath = Path.Combine(_logDirectory, $"{DateTime.Now:yyyy-MM-dd}.json");

        var entries = new List<TransferLog>();
        if (File.Exists(filePath))
        {
            var existingJson = File.ReadAllText(filePath);
            if (!string.IsNullOrWhiteSpace(existingJson))
            {
                entries = JsonSerializer.Deserialize<List<TransferLog>>(existingJson)
                          ?? new List<TransferLog>();
            }
        }

        entries.Add(log);

        var options = new JsonSerializerOptions { WriteIndented = true };
        var json = JsonSerializer.Serialize(entries, options);
        File.WriteAllText(filePath, json);
    }
}
