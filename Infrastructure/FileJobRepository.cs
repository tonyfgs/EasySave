using System.Text.Json;
using Application.Ports;
using Model;

namespace Infrastructure;

public class FileJobRepository : IJobRepository
{
    private readonly string _filePath;
    private List<BackupJob> _jobs;

    public FileJobRepository(string filePath)
    {
        _filePath = filePath;
        _jobs = LoadFromDisk();
    }

    public void Save(BackupJob job)
    {
        if (job.Id == 0)
            job.Id = GenerateId();

        _jobs.Add(job);
        PersistToDisk();
    }

    public void Delete(int id)
    {
        _jobs.RemoveAll(j => j.Id == id);
        PersistToDisk();
    }

    public List<BackupJob> GetAll()
    {
        return new List<BackupJob>(_jobs);
    }

    public BackupJob? GetById(int id)
    {
        return _jobs.FirstOrDefault(j => j.Id == id);
    }

    public void Update(BackupJob job)
    {
        var index = _jobs.FindIndex(j => j.Id == job.Id);
        if (index >= 0)
        {
            _jobs[index] = job;
            PersistToDisk();
        }
    }

    public int Count()
    {
        return _jobs.Count;
    }

    private int GenerateId()
    {
        if (_jobs.Count == 0)
            return 1;
        return _jobs.Max(j => j.Id) + 1;
    }

    private void PersistToDisk()
    {
        var directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        var options = new JsonSerializerOptions { WriteIndented = true };
        var json = JsonSerializer.Serialize(_jobs, options);
        File.WriteAllText(_filePath, json);
    }

    private List<BackupJob> LoadFromDisk()
    {
        if (!File.Exists(_filePath))
            return new List<BackupJob>();

        var json = File.ReadAllText(_filePath);
        if (string.IsNullOrWhiteSpace(json))
            return new List<BackupJob>();

        return JsonSerializer.Deserialize<List<BackupJob>>(json) ?? new List<BackupJob>();
    }
}
