using System.Text.Json;
using Application.DTOs;
using Application.Ports;

namespace Infrastructure;

public class JsonStateManager : IStateManager
{
    private readonly string _filePath;
    private List<StateSnapshot> _states;

    public JsonStateManager(string filePath)
    {
        _filePath = filePath;
        _states = LoadFromDisk();
    }

    public void UpdateState(StateSnapshot snapshot)
    {
        var index = _states.FindIndex(s => s.Name == snapshot.Name);
        if (index >= 0)
            _states[index] = snapshot;
        else
            _states.Add(snapshot);

        PersistToDisk();
    }

    public List<StateSnapshot> GetAllStates()
    {
        return new List<StateSnapshot>(_states);
    }

    public void ClearAll()
    {
        _states.Clear();
        PersistToDisk();
    }

    private void PersistToDisk()
    {
        var directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        var options = new JsonSerializerOptions { WriteIndented = true };
        var json = JsonSerializer.Serialize(_states, options);
        File.WriteAllText(_filePath, json);
    }

    private List<StateSnapshot> LoadFromDisk()
    {
        if (!File.Exists(_filePath))
            return new List<StateSnapshot>();

        var json = File.ReadAllText(_filePath);
        if (string.IsNullOrWhiteSpace(json))
            return new List<StateSnapshot>();

        return JsonSerializer.Deserialize<List<StateSnapshot>>(json) ?? new List<StateSnapshot>();
    }
}
