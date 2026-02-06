using Application.Ports;
using Infrastructure;
using Shared;

namespace InfrastructureTest;

public class AppConfigurationTests : IDisposable
{
    private readonly string _testDir;
    private readonly string _configPath;

    public AppConfigurationTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), $"easysave_config_test_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDir);
        _configPath = Path.Combine(_testDir, "config.json");
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDir))
            Directory.Delete(_testDir, true);
    }

    [Fact]
    public void GetLanguage_Default_ShouldReturnEN()
    {
        var config = new AppConfiguration(_configPath, "/logs");

        Assert.Equal(Language.EN, config.GetLanguage());
    }

    [Fact]
    public void SetLanguage_ThenGetLanguage_ShouldRoundTrip()
    {
        var config = new AppConfiguration(_configPath, "/logs");

        config.SetLanguage(Language.FR);

        Assert.Equal(Language.FR, config.GetLanguage());
    }

    [Fact]
    public void GetLogFormat_Default_ShouldReturnJSON()
    {
        var config = new AppConfiguration(_configPath, "/logs");

        Assert.Equal(LogFormat.JSON, config.GetLogFormat());
    }

    [Fact]
    public void SetLogFormat_ThenGetLogFormat_ShouldRoundTrip()
    {
        var config = new AppConfiguration(_configPath, "/logs");

        config.SetLogFormat(LogFormat.XML);

        Assert.Equal(LogFormat.XML, config.GetLogFormat());
    }

    [Fact]
    public void GetLogDirectory_ShouldReturnConfiguredValue()
    {
        var config = new AppConfiguration(_configPath, "/my/logs");

        Assert.Equal("/my/logs", config.GetLogDirectory());
    }

    [Fact]
    public void Save_ThenLoad_ShouldPersistConfiguration()
    {
        var config1 = new AppConfiguration(_configPath, "/logs");
        config1.SetLanguage(Language.FR);
        config1.SetLogFormat(LogFormat.XML);
        config1.Save();

        var config2 = new AppConfiguration(_configPath, "/logs");

        Assert.Equal(Language.FR, config2.GetLanguage());
        Assert.Equal(LogFormat.XML, config2.GetLogFormat());
    }

    [Fact]
    public void Load_FromNonExistentFile_ShouldUseDefaults()
    {
        var missingPath = Path.Combine(_testDir, "missing", "config.json");
        var config = new AppConfiguration(missingPath, "/logs");

        Assert.Equal(Language.EN, config.GetLanguage());
        Assert.Equal(LogFormat.JSON, config.GetLogFormat());
    }

    [Fact]
    public void AppConfiguration_ImplementsILanguageConfig()
    {
        var config = new AppConfiguration(_configPath, "/logs");

        ILanguageConfig langConfig = config;

        langConfig.SetLanguage(Language.FR);
        Assert.Equal(Language.FR, langConfig.GetLanguage());
    }

    [Fact]
    public void SetLanguage_WithoutSave_ShouldNotPersistToNewInstance()
    {
        var config1 = new AppConfiguration(_configPath, "/logs");
        config1.SetLanguage(Language.FR);

        var config2 = new AppConfiguration(_configPath, "/logs");

        Assert.Equal(Language.EN, config2.GetLanguage());
    }

    [Fact]
    public void SetLanguage_WithSave_ThenNewInstance_ShouldPersist()
    {
        var config1 = new AppConfiguration(_configPath, "/logs");
        config1.SetLanguage(Language.FR);
        config1.Save();

        var config2 = new AppConfiguration(_configPath, "/logs");

        Assert.Equal(Language.FR, config2.GetLanguage());
    }
}
