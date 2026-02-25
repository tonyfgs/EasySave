using Application.Ports;
using Infrastructure;
using Moq;

namespace InfrastructureTest;

public class ProcessDetectorTests
{
    private readonly Mock<IBusinessSoftwareConfig> _configMock;

    public ProcessDetectorTests()
    {
        _configMock = new Mock<IBusinessSoftwareConfig>();
    }

    [Fact]
    public void GetStatus_WhenDetectionDisabled_ReturnsDisabled()
    {
        _configMock.Setup(c => c.IsDetectionEnabled()).Returns(false);
        var detector = new ProcessDetector(_configMock.Object);

        var result = detector.GetStatus();

        Assert.Equal(BusinessSoftwareStatus.Disabled, result);
    }

    [Fact]
    public void GetStatus_WhenNameEmpty_ReturnsDisabled()
    {
        _configMock.Setup(c => c.IsDetectionEnabled()).Returns(true);
        _configMock.Setup(c => c.GetBusinessSoftwareName()).Returns("");
        var detector = new ProcessDetector(_configMock.Object);

        var result = detector.GetStatus();

        Assert.Equal(BusinessSoftwareStatus.Disabled, result);
    }

    [Fact]
    public void GetStatus_WhenProcessNotFound_ReturnsNotRunning()
    {
        _configMock.Setup(c => c.IsDetectionEnabled()).Returns(true);
        _configMock.Setup(c => c.GetBusinessSoftwareName()).Returns("nonexistent_process_xyz_12345");
        var detector = new ProcessDetector(_configMock.Object);

        var result = detector.GetStatus();

        Assert.Equal(BusinessSoftwareStatus.NotRunning, result);
    }

    [Fact]
    public void GetStatus_WhenProcessExists_ReturnsRunning()
    {
        _configMock.Setup(c => c.IsDetectionEnabled()).Returns(true);
        _configMock.Setup(c => c.GetBusinessSoftwareName()).Returns("fake-process");
        var detector = new FakeProcessDetector(_configMock.Object, found: true);

        var result = detector.GetStatus();

        Assert.Equal(BusinessSoftwareStatus.Running, result);
    }

    [Fact]
    public void GetStatus_WhenExceptionOccurs_ReturnsError()
    {
        _configMock.Setup(c => c.IsDetectionEnabled()).Returns(true);
        _configMock.Setup(c => c.GetBusinessSoftwareName()).Returns("anything");
        var detector = new ThrowingProcessDetector(_configMock.Object);

        var result = detector.GetStatus();

        Assert.Equal(BusinessSoftwareStatus.Error, result);
    }

    // IsProcessRunning tests

    [Fact]
    public void IsProcessRunning_WhenNullOrWhitespace_ReturnsFalse()
    {
        var detector = new ProcessDetector(_configMock.Object);
        Assert.False(detector.IsProcessRunning(null!));
        Assert.False(detector.IsProcessRunning(""));
        Assert.False(detector.IsProcessRunning("   "));
    }

    [Fact]
    public void IsProcessRunning_WhenProcessFound_ReturnsTrue()
    {
        var detector = new FakeProcessDetector(_configMock.Object, found: true);
        Assert.True(detector.IsProcessRunning("anything"));
    }

    [Fact]
    public void IsProcessRunning_WhenProcessNotFound_ReturnsFalse()
    {
        var detector = new FakeProcessDetector(_configMock.Object, found: false);
        Assert.False(detector.IsProcessRunning("anything"));
    }

    [Fact]
    public void IsProcessRunning_WhenExceptionOccurs_ReturnsFalse()
    {
        var detector = new ThrowingProcessDetector(_configMock.Object);
        Assert.False(detector.IsProcessRunning("anything"));
    }

    private class FakeProcessDetector(IBusinessSoftwareConfig config, bool found) : ProcessDetector(config)
    {
        protected override System.Diagnostics.Process[] FindProcesses(string name)
            => found ? [System.Diagnostics.Process.GetCurrentProcess()] : [];
    }

    private class ThrowingProcessDetector : ProcessDetector
    {
        public ThrowingProcessDetector(IBusinessSoftwareConfig config) : base(config) { }

        protected override System.Diagnostics.Process[] FindProcesses(string name) =>
            throw new InvalidOperationException("simulated process error");
    }
}
