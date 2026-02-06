using Application.Ports;
using Infrastructure;
using Logger.Interface;
using Moq;
using Shared;

namespace InfrastructureTest;

public class TransferLoggerFactoryTests
{
    [Fact]
    public void Create_WithJsonFormat_ShouldReturnJsonTransferLogger()
    {
        var mockLogger = new Mock<IEasyLogger>();
        var factory = new TransferLoggerFactory(LogFormat.JSON, mockLogger.Object, "/tmp/logs");

        var logger = factory.Create();

        Assert.IsType<JsonTransferLogger>(logger);
    }

    [Fact]
    public void Create_WithXmlFormat_ShouldReturnXmlTransferLogger()
    {
        var mockLogger = new Mock<IEasyLogger>();
        var factory = new TransferLoggerFactory(LogFormat.XML, mockLogger.Object, "/tmp/logs");

        var logger = factory.Create();

        Assert.IsType<XmlTransferLogger>(logger);
    }
}
