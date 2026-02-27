using LogCentralizer.Models;
using LogCentralizer.Services;

var builder = WebApplication.CreateBuilder(args);

// Configure services
builder.Services.AddSingleton<LogAggregator>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure logging
builder.Logging.AddConsole();

var app = builder.Build();

var logger = app.Services.GetRequiredService<ILogger<Program>>();

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

// Receive log entry with validation
app.MapPost("/api/log", async (LogEntry entry, LogAggregator aggregator, ILogger<Program> log) =>
{
    // Validate entry
    if (!entry.IsValid())
    {
        log.LogWarning("Invalid log entry received: BackupName={BackupName}, Timestamp={Timestamp}",
            entry.BackupName, entry.Timestamp);
        return Results.BadRequest(new { error = "Invalid log entry. BackupName is required and Timestamp must be set." });
    }

    try
    {
        await aggregator.AddLogAsync(entry);
        return Results.Ok(new { success = true, message = "Log received" });
    }
    catch (Exception ex)
    {
        log.LogError(ex, "Failed to process log entry");
        return Results.Problem($"Failed to process log: {ex.Message}");
    }
})
    .WithName("PostLog")
    .WithTags("Logs");

// Receive batch of log entries with validation
app.MapPost("/api/logs/batch", async (List<LogEntry> entries, LogAggregator aggregator, ILogger<Program> log) =>
{
    // Validate all entries
    var invalidEntries = entries.Where(e => !e.IsValid()).ToList();
    if (invalidEntries.Count > 0)
    {
        log.LogWarning("Batch contains {Count} invalid entries", invalidEntries.Count);
        return Results.BadRequest(new { error = $"Batch contains {invalidEntries.Count} invalid entries." });
    }

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
        log.LogError(ex, "Failed to process log batch");
        return Results.Problem($"Failed to process logs: {ex.Message}");
    }
})
    .WithName("PostLogBatch")
    .WithTags("Logs");

// Get logs for a specific date
app.MapGet("/api/logs/{date}", async (string date, LogAggregator aggregator, string? format, ILogger<Program> log) =>
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
        log.LogError(ex, "Failed to retrieve logs for {Date}", date);
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
        var logs = await aggregator.GetLogsAsync(DateOnly.FromDateTime(DateTime.UtcNow));
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

logger.LogInformation("═══════════════════════════════════════════════════════");
logger.LogInformation("  LogCentralizer v1.0 - EasySave Log Aggregation Service");
logger.LogInformation("═══════════════════════════════════════════════════════");
logger.LogInformation("Listening on: http://localhost:5050");
logger.LogInformation("Endpoints:");
logger.LogInformation("  POST /api/log          - Receive a log entry");
logger.LogInformation("  POST /api/logs/batch   - Receive batch of logs");
logger.LogInformation("  GET  /api/logs/{{date}}  - Get logs for a date (yyyy-MM-dd)");
logger.LogInformation("  GET  /api/logs/today   - Get today's logs");
logger.LogInformation("  GET  /api/stats        - Get statistics");
logger.LogInformation("  GET  /api/health       - Health check");

app.Run("http://+:5050");
