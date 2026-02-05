using Application.Ports;
using Model;

namespace Application.Services;

public class JobManagementService
{
    private const int MaxJobs = 5;
    private readonly IJobRepository _repository;
    private readonly BackupDomainService _domainService;

    public JobManagementService(IJobRepository repository, BackupDomainService domainService)
    {
        _repository = repository;
        _domainService = domainService;
    }

    public BackupJob CreateJob(string name, string source, string target, BackupType type)
    {
        _domainService.ValidateJobLimit(_repository.Count(), MaxJobs);

        var job = new BackupJob(0, name, source, target, type);
        job.Validate();
        _repository.Save(job);
        return job;
    }

    public List<BackupJob> ListJobs()
    {
        return _repository.GetAll();
    }

    public void DeleteJob(int id)
    {
        _repository.Delete(id);
    }

    public void ModifyJob(int id, string name, string source, string target, BackupType type)
    {
        var job = _repository.GetById(id)
            ?? throw new DomainException($"Job with ID {id} not found.");

        job.Name = name;
        job.SourcePath = source;
        job.TargetPath = target;
        job.Type = type;

        job.Validate();
        _repository.Update(job);
    }
}
