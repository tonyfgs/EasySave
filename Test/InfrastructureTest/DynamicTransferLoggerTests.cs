using Application.DTOs;
using Infrastructure;
using Shared;
using Xunit;

namespace InfrastructureTest;

public class DynamicTransferLoggerTests
{
    [Fact]
    public void LogTransfer_WithJsonFormat_ShouldWriteJsonFile()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "EasySaveTest_" + Guid.NewGuid());
        var configPath = Path.Combine(tempDir, "config.json");
        var logDirectory = Path.Combine(tempDir, "logs");

        try
        {
            var config = new AppConfiguration(configPath, logDirectory);
            config.SetLogFormat(LogFormat.JSON);

            var logger = new DynamicTransferLogger(config, new Logger.Service.DailyLogsService());

            var transferLog = new TransferLog
            {
                Timestamp = DateTime.Now,
                BackupName = "TestBackup",
                SourcePath = "/source/test.txt",
                DestPath = "/dest/test.txt",
                FileSize = 1024,
                TransferTimeMs = 50,
                EncryptionTimeMs = 0
            };

            logger.LogTransfer(transferLog);

            var logFile = Path.Combine(logDirectory, $"{DateTime.Now:yyyy-MM-dd}.json");
            Assert.True(File.Exists(logFile), $"Log file should exist at {logFile}");

            var content = File.ReadAllText(logFile);
            Assert.Contains("TestBackup", content);
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void LogTransfer_WithXmlFormat_ShouldWriteXmlFile()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "EasySaveTest_" + Guid.NewGuid());
        var configPath = Path.Combine(tempDir, "config.json");
        var logDirectory = Path.Combine(tempDir, "logs");

        try
        {
            var config = new AppConfiguration(configPath, logDirectory);
            config.SetLogFormat(LogFormat.XML);

            var logger = new DynamicTransferLogger(config, new Logger.Service.DailyLogsService());

            var transferLog = new TransferLog
            {
                Timestamp = DateTime.Now,
                BackupName = "TestBackup",
                SourcePath = "/source/test.txt",
                DestPath = "/dest/test.txt",
                FileSize = 1024,
                TransferTimeMs = 50,
                EncryptionTimeMs = 0
            };

            logger.LogTransfer(transferLog);

            var logFile = Path.Combine(logDirectory, $"{DateTime.Now:yyyy-MM-dd}.xml");
            Assert.True(File.Exists(logFile), $"Log file should exist at {logFile}");

            var content = File.ReadAllText(logFile);
            Assert.Contains("TestBackup", content);
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }
}
