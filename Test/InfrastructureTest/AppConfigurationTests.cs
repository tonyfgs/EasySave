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

    // --- IEncryptionConfig tests ---

    [Fact]
    public void AppConfiguration_ImplementsIEncryptionConfig()
    {
        var config = new AppConfiguration(_configPath, "/logs");

        Assert.IsAssignableFrom<IEncryptionConfig>(config);
    }

    [Fact]
    public void GetEncryptedExtensions_Default_ShouldReturnEmptyList()
    {
        var config = new AppConfiguration(_configPath, "/logs");

        var extensions = config.GetEncryptedExtensions();

        Assert.Empty(extensions);
    }

    [Fact]
    public void SetEncryptedExtensions_ThenGet_ShouldRoundTrip()
    {
        var config = new AppConfiguration(_configPath, "/logs");
        var extensions = new List<string> { ".docx", ".pdf" }.AsReadOnly();

        config.SetEncryptedExtensions(extensions);

        Assert.Equal(extensions, config.GetEncryptedExtensions());
    }

    [Fact]
    public void GetEncryptionKey_Default_ShouldReturnEmpty()
    {
        var config = new AppConfiguration(_configPath, "/logs");

        Assert.Equal(string.Empty, config.GetEncryptionKey());
    }

    [Fact]
    public void SetEncryptionKey_ThenGet_ShouldRoundTrip()
    {
        var config = new AppConfiguration(_configPath, "/logs");

        config.SetEncryptionKey("my-secret-key");

        Assert.Equal("my-secret-key", config.GetEncryptionKey());
    }

    [Fact]
    public void Save_ThenLoad_ShouldPersistEncryptionConfig()
    {
        var config1 = new AppConfiguration(_configPath, "/logs");
        config1.SetEncryptedExtensions(new List<string> { ".docx", ".xlsx" }.AsReadOnly());
        config1.SetEncryptionKey("secret-key-123");
        config1.Save();

        var config2 = new AppConfiguration(_configPath, "/logs");

        Assert.Equal(new List<string> { ".docx", ".xlsx" }, config2.GetEncryptedExtensions());
        Assert.Equal("secret-key-123", config2.GetEncryptionKey());
    }

    // --- IBusinessSoftwareConfig tests ---

    [Fact]
    public void AppConfiguration_ImplementsIBusinessSoftwareConfig()
    {
        var config = new AppConfiguration(_configPath, "/logs");

        Assert.IsAssignableFrom<IBusinessSoftwareConfig>(config);
    }

    [Fact]
    public void GetBusinessSoftwareName_Default_ShouldReturnEmpty()
    {
        var config = new AppConfiguration(_configPath, "/logs");

        Assert.Equal(string.Empty, config.GetBusinessSoftwareName());
    }

    [Fact]
    public void SetBusinessSoftwareName_ThenGet_ShouldRoundTrip()
    {
        var config = new AppConfiguration(_configPath, "/logs");

        config.SetBusinessSoftwareName("Calculator");

        Assert.Equal("Calculator", config.GetBusinessSoftwareName());
    }

    [Fact]
    public void IsDetectionEnabled_Default_ShouldReturnFalse()
    {
        var config = new AppConfiguration(_configPath, "/logs");

        Assert.False(config.IsDetectionEnabled());
    }

    [Fact]
    public void SetDetectionEnabled_ThenGet_ShouldRoundTrip()
    {
        var config = new AppConfiguration(_configPath, "/logs");

        config.SetDetectionEnabled(true);

        Assert.True(config.IsDetectionEnabled());
    }

    [Fact]
    public void Save_ThenLoad_ShouldPersistBusinessSoftwareConfig()
    {
        var config1 = new AppConfiguration(_configPath, "/logs");
        config1.SetBusinessSoftwareName("Calculator");
        config1.SetDetectionEnabled(true);
        config1.Save();

        var config2 = new AppConfiguration(_configPath, "/logs");

        Assert.Equal("Calculator", config2.GetBusinessSoftwareName());
        Assert.True(config2.IsDetectionEnabled());
    }

    [Fact]
    public void Save_ThenLoad_ShouldPersistAllV2Fields()
    {
        var config1 = new AppConfiguration(_configPath, "/logs");
        config1.SetLanguage(Language.FR);
        config1.SetLogFormat(LogFormat.XML);
        config1.SetEncryptedExtensions(new List<string> { ".pdf" }.AsReadOnly());
        config1.SetEncryptionKey("key");
        config1.SetBusinessSoftwareName("Calc");
        config1.SetDetectionEnabled(true);
        config1.Save();

        var config2 = new AppConfiguration(_configPath, "/logs");

        Assert.Equal(Language.FR, config2.GetLanguage());
        Assert.Equal(LogFormat.XML, config2.GetLogFormat());
        Assert.Equal(new List<string> { ".pdf" }, config2.GetEncryptedExtensions());
        Assert.Equal("key", config2.GetEncryptionKey());
        Assert.Equal("Calc", config2.GetBusinessSoftwareName());
        Assert.True(config2.IsDetectionEnabled());
    }

    [Fact]
    public void Load_FromNonExistentFile_ShouldUseV2Defaults()
    {
        var missingPath = Path.Combine(_testDir, "missing", "config.json");
        var config = new AppConfiguration(missingPath, "/logs");

        Assert.Empty(config.GetEncryptedExtensions());
        Assert.Equal(string.Empty, config.GetEncryptionKey());
        Assert.Equal(string.Empty, config.GetBusinessSoftwareName());
        Assert.False(config.IsDetectionEnabled());
    }

    // --- IPriorityFileConfig tests ---

    [Fact]
    public void AppConfiguration_ImplementsIPriorityFileConfig()
    {
        var config = new AppConfiguration(_configPath, "/logs");

        Assert.IsAssignableFrom<IPriorityFileConfig>(config);
    }

    [Fact]
    public void GetPriorityExtensions_Default_ShouldReturnEmptyList()
    {
        var config = new AppConfiguration(_configPath, "/logs");

        Assert.Empty(config.GetPriorityExtensions());
    }

    [Fact]
    public void SetPriorityExtensions_ThenGet_ShouldRoundTrip()
    {
        var config = new AppConfiguration(_configPath, "/logs");
        var extensions = new List<string> { ".docx", ".pdf", ".xlsx" }.AsReadOnly();

        config.SetPriorityExtensions(extensions);

        Assert.Equal(extensions, config.GetPriorityExtensions());
    }

    [Fact]
    public void Save_ThenLoad_ShouldPersistPriorityExtensions()
    {
        var config1 = new AppConfiguration(_configPath, "/logs");
        config1.SetPriorityExtensions(new List<string> { ".docx", ".pptx" }.AsReadOnly());
        config1.Save();

        var config2 = new AppConfiguration(_configPath, "/logs");

        Assert.Equal(new List<string> { ".docx", ".pptx" }, config2.GetPriorityExtensions());
    }

    [Fact]
    public void Load_FromNonExistentFile_ShouldUsePriorityDefaults()
    {
        var missingPath = Path.Combine(_testDir, "missing", "config.json");
        var config = new AppConfiguration(missingPath, "/logs");

        Assert.Empty(config.GetPriorityExtensions());
    }
}
