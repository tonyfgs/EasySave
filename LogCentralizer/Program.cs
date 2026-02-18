using System.Text.Json;
using LogCentralizer.Services;

var builder = WebApplication.CreateBuilder(args);

// Configure services
builder.Services.AddSingleton<LogAggregator>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure Swagger for development
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Health check endpoint
app.MapGet("/api/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }))
    .WithName("HealthCheck")
    .WithTags("Health");

// Receive log entry
app.MapPost("/api/log", async (LogEntry entry, LogAggregator aggregator) =>
{
    try
    {
        await aggregator.AddLogAsync(entry);
        return Results.Ok(new { success = true, message = "Log received" });
    }
    catch (Exception ex)
    {
        return Results.Problem($"Failed to process log: {ex.Message}");
    }
})
    .WithName("PostLog")
    .WithTags("Logs");

// Receive batch of log entries
app.MapPost("/api/logs/batch", async (List<LogEntry> entries, LogAggregator aggregator) =>
{
    try
    {
        foreach (var entry in entries)
        {
            await aggregator.AddLogAsync(entry);
        }
        return Results.Ok(new { success = true, count = entries.Count });
    }
    catch (Exception ex)
    {
        return Results.Problem($"Failed to process logs: {ex.Message}");
    }
})
    .WithName("PostLogBatch")
    .WithTags("Logs");

// Get logs for a specific date
app.MapGet("/api/logs/{date}", async (string date, LogAggregator aggregator, string? format) =>
{
    try
    {
        if (!DateOnly.TryParse(date, out var parsedDate))
        {
            return Results.BadRequest("Invalid date format. Use yyyy-MM-dd");
        }

        var logs = await aggregator.GetLogsAsync(parsedDate);

        if (format?.ToLowerInvariant() == "xml")
        {
            var xml = aggregator.SerializeToXml(logs);
            return Results.Content(xml, "application/xml");
        }

        return Results.Ok(logs);
    }
    catch (FileNotFoundException)
    {
        return Results.NotFound($"No logs found for {date}");
    }
    catch (Exception ex)
    {
        return Results.Problem($"Failed to retrieve logs: {ex.Message}");
    }
})
    .WithName("GetLogsByDate")
    .WithTags("Logs");

// Get today's logs
app.MapGet("/api/logs/today", async (LogAggregator aggregator) =>
{
    try
    {
        var logs = await aggregator.GetLogsAsync(DateOnly.FromDateTime(DateTime.Now));
        return Results.Ok(logs);
    }
    catch (FileNotFoundException)
    {
        return Results.Ok(new List<LogEntry>());
    }
})
    .WithName("GetTodayLogs")
    .WithTags("Logs");

// Get statistics
app.MapGet("/api/stats", async (LogAggregator aggregator) =>
{
    var stats = await aggregator.GetStatisticsAsync();
    return Results.Ok(stats);
})
    .WithName("GetStatistics")
    .WithTags("Statistics");

Console.WriteLine("═══════════════════════════════════════════════════════");
Console.WriteLine("  LogCentralizer v1.0 - EasySave Log Aggregation Service");
Console.WriteLine("═══════════════════════════════════════════════════════");
Console.WriteLine();
Console.WriteLine("Listening on: http://localhost:5050");
Console.WriteLine();
Console.WriteLine("Endpoints:");
Console.WriteLine("  POST /api/log          - Receive a log entry");
Console.WriteLine("  POST /api/logs/batch   - Receive batch of logs");
Console.WriteLine("  GET  /api/logs/{date}  - Get logs for a date (yyyy-MM-dd)");
Console.WriteLine("  GET  /api/logs/today   - Get today's logs");
Console.WriteLine("  GET  /api/stats        - Get statistics");
Console.WriteLine("  GET  /api/health       - Health check");
Console.WriteLine();

app.Run("http://localhost:5050");

public record LogEntry
{
    public DateTime Timestamp { get; init; }
    public string BackupName { get; init; } = string.Empty;
    public string SourcePath { get; init; } = string.Empty;
    public string DestPath { get; init; } = string.Empty;
    public long FileSize { get; init; }
    public long TransferTimeMs { get; init; }
    public long EncryptionTimeMs { get; init; }
    public string UserId { get; init; } = string.Empty;
    public string MachineName { get; init; } = string.Empty;
}
