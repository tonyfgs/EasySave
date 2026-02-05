using Model;

namespace ModelTest;

public class StateManagerTests : IDisposable
{
    private readonly DirectoryInfo _sourceDir;
    private readonly DirectoryInfo _targetDir;
    private readonly DirectoryInfo _stateDir;
    private readonly string _statePath;

    public StateManagerTests()
    {
        _sourceDir = Directory.CreateTempSubdirectory("easysave_src_");
        _targetDir = Directory.CreateTempSubdirectory("easysave_dst_");
        _stateDir = Directory.CreateTempSubdirectory("easysave_state_");
        _statePath = Path.Combine(_stateDir.FullName, "state.json");
    }

    public void Dispose()
    {
        if (_sourceDir.Exists) _sourceDir.Delete(true);
        if (_targetDir.Exists) _targetDir.Delete(true);
        if (_stateDir.Exists) _stateDir.Delete(true);
    }
}
