using System.Text.Json;
using Application.Ports;
using Model;

namespace Infrastructure;

public class FileJobRepository : IJobRepository
{
    private readonly string _filePath;
    private readonly object _lock = new();
    private List<BackupJob> _jobs;

    public FileJobRepository(string filePath)
    {
        _filePath = filePath;
        _jobs = LoadFromDisk();
    }

    public void Save(BackupJob job)
    {
        lock (_lock)
        {
            if (job.Id == 0)
                job.Id = GenerateId();

            _jobs.Add(job);
            PersistToDisk();
        }
    }

    public void Delete(int id)
    {
        lock (_lock)
        {
            _jobs.RemoveAll(j => j.Id == id);
            PersistToDisk();
        }
    }

    public List<BackupJob> GetAll()
    {
        lock (_lock)
            return new List<BackupJob>(_jobs);
    }

    public BackupJob? GetById(int id)
    {
        lock (_lock)
        {
            var job = _jobs.FirstOrDefault(j => j.Id == id);
            if (job is null) return null;
            return new BackupJob(job.Id, job.Name, job.SourcePath, job.TargetPath, job.Type)
            {
                LastFullBackupDate = job.LastFullBackupDate,
                CreatedDate = job.CreatedDate
            };
        }
    }

    public void Update(BackupJob job)
    {
        lock (_lock)
        {
            var index = _jobs.FindIndex(j => j.Id == job.Id);
            if (index >= 0)
            {
                _jobs[index] = job;
                PersistToDisk();
            }
        }
    }

    public int Count()
    {
        lock (_lock)
            return _jobs.Count;
    }

    // Caller must hold _lock
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
