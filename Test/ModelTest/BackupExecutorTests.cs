using Model;

namespace ModelTest;


public class BackupExecutorTests : IDisposable
{
    private readonly DirectoryInfo _sourceDir;
    private readonly DirectoryInfo _targetDir;

    public BackupExecutorTests()
    {
        _sourceDir = Directory.CreateTempSubdirectory("easysave_src_");
        _targetDir = Directory.CreateTempSubdirectory("easysave_dst_");
    }

    public void Dispose()
    {
        if (_sourceDir.Exists) _sourceDir.Delete(true);
        if (_targetDir.Exists) _targetDir.Delete(true);
    }
}
