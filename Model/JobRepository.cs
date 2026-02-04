using System.Text.Json;

namespace Model;

public class JobRepository
{
    private const int MAX_JOBS = 5;
    private string PathDest { get; init; }
    public JobRepository(string pathDest= null)
    {
  
        if (pathDest == null)
        {
            PathDest = Path.Combine(Directory.GetCurrentDirectory(), "BackupJobs");
        }
        else
        {
            PathDest = pathDest;
        }

        if (!Directory.Exists(PathDest))
        {
            Directory.CreateDirectory(PathDest);
        }
    }

    public bool AddJob(BackupJob job)
    {
        if (job == null) return false;
        if (string.IsNullOrWhiteSpace(job.Name)) return false;
        
        
        try
        {
            EnsureDirectoryExists();

            if (CountJobs() >= MAX_JOBS) return false;
            if (job.Id != 0 && ExistsById(job.Id)) return false;
            if (ExistsByName(job.Name)) return false;

            var jobToSave = GenerateIdIfNeeded(job);
            var fullPath = BuildFilePath(jobToSave);

            return WriteJobToFile(jobToSave, fullPath);
        }
        catch
        {
            return false;
        }
    }

    private void EnsureDirectoryExists()
    {
        Directory.CreateDirectory(PathDest);
    }

    private int CountJobs()
    {
        return Directory.EnumerateFiles(PathDest, "*.json", SearchOption.TopDirectoryOnly).Count();
    }
    
    private bool ExistsById(long id)
    {
        var files = Directory.EnumerateFiles(PathDest, "*.json", SearchOption.TopDirectoryOnly);
        return files.Any(f => Path.GetFileName(f).StartsWith($"{id}_", StringComparison.Ordinal));
    }

    private bool ExistsByName(string name)
    {
        if (string.IsNullOrEmpty(name)) return false;
        var pattern = $"*_{name}.json";
        return Directory.EnumerateFiles(PathDest, pattern, SearchOption.TopDirectoryOnly).Any();
    }
    
    private BackupJob GenerateIdIfNeeded(BackupJob job)
    {
        if (job.Id != 0) return job;
        return job with { Id = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() };
    }

    private string BuildFileName(BackupJob job)
    {
        return $"{job.Id}_{job.Name}.json";
    }
    
    private string BuildFilePath(BackupJob job)
    {
        return Path.Combine(PathDest, BuildFileName(job));
    }

    private bool WriteJobToFile(BackupJob job, string fullPath)
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        var json = JsonSerializer.Serialize(job, options);
        File.WriteAllText(fullPath, json);
        return true;
    }
    
    public bool DeleteJob(long id)
    {
        try
        {
            var file = Directory.EnumerateFiles(PathDest, $"{id}_*.json", SearchOption.TopDirectoryOnly).FirstOrDefault();
            if (file == null) return false;
            File.Delete(file);
            return true;
        }
        catch
        {
            return false;
        }
    }
    
    public bool UpdateJob(BackupJob job)
    {
        if (job == null) return false;
        if (job.Id == 0) return false;
        if (string.IsNullOrWhiteSpace(job.Name)) return false;

        try
        {
            // vérifie que l'élément existe
            var existingFile = Directory.EnumerateFiles(PathDest, $"{job.Id}_*.json", SearchOption.TopDirectoryOnly).FirstOrDefault();
            if (existingFile == null) return false;
            
            var nameCollision = Directory.EnumerateFiles(PathDest, $"*_{job.Name}.json", SearchOption.TopDirectoryOnly)
                .Select(Path.GetFileName)
                .FirstOrDefault();
            if (nameCollision != null)
            {
                var parts = nameCollision.Split('_');
                if (parts.Length > 0 && long.TryParse(parts[0], out var existingId) && existingId != job.Id)
                {
                    return false;
                }
            }
            var newPath = BuildFilePath(job);
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(job, options);
            File.WriteAllText(newPath, json);

            if (!string.Equals(existingFile, newPath, StringComparison.OrdinalIgnoreCase) && File.Exists(existingFile))
            {
                File.Delete(existingFile);
            }
            return true;
        }
        catch
        {
            return false;
        }
    }
        
    public BackupJob GetJobByID(long id)
    {
        try
        {
            var file = Directory.EnumerateFiles(PathDest, $"{id}_*.json", SearchOption.TopDirectoryOnly).FirstOrDefault();
            if (file == null) return null;
            var json = File.ReadAllText(file);
            return JsonSerializer.Deserialize<BackupJob>(json);
        }
        catch
        {
            return null;
        }
    }
    
    public BackupJob GetJobByName(string Name)
    {
        if (string.IsNullOrWhiteSpace(Name)) return null;
        try
        {
            var file = Directory.EnumerateFiles(PathDest, $"*_{Name}.json", SearchOption.TopDirectoryOnly).FirstOrDefault();
            if (file == null) return null;
            var json = File.ReadAllText(file);
            return JsonSerializer.Deserialize<BackupJob>(json);
        }
        catch
        {
            return null;
        }
    }
    
    public IReadOnlyList<BackupJob> GetAllJobs()
    {
        if (!Directory.Exists(PathDest))
            return Array.Empty<BackupJob>(); 

        var result = new List<BackupJob>();
        foreach (var f in Directory.EnumerateFiles(PathDest, "*.json"))
        {
            var json = File.ReadAllText(f);
            var job = JsonSerializer.Deserialize<BackupJob>(json);
            if (job != null) result.Add(job);

        }

        return result;
    }
}



