using Application.DTOs;
using Application.Events;
using Application.Handlers;
using Application.Ports;
using Moq;

namespace ApplicationTest;

public class TransferCompletedHandlerTests
{
    [Fact]
    public void Handle_ShouldCallLogTransferOnLogger()
    {
        var mockLogger = new Mock<ITransferLogger>();
        var handler = new TransferCompletedHandler(mockLogger.Object);
        var log = new TransferLog
        {
            Timestamp = DateTime.Now,
            BackupJobName = "TestJob",
            SourceFilePath = "/src/file.txt",
            TargetFilePath = "/dst/file.txt",
            FileSize = 1024,
            TransferTimeMs = 50
        };
        var @event = new TransferCompletedEvent(log);

        handler.Handle(@event);

        mockLogger.Verify(l => l.LogTransfer(log), Times.Once);
    }
}
