using Application.DTOs;
using Application.Ports;
using Model;

namespace Application.Services;

public class BackupExecutionService
{
    private readonly IJobRepository _repository;
    private readonly BackupExecutor _executor;
    private readonly BackupStrategyFactory _strategyFactory;

    public BackupExecutionService(
        IJobRepository repository,
        BackupExecutor executor,
        BackupStrategyFactory strategyFactory)
    {
        _repository = repository;
        _executor = executor;
        _strategyFactory = strategyFactory;
    }

    public List<JobExecutionResult> ExecuteJobs(List<int> jobIds)
    {
        var results = new List<JobExecutionResult>();

        foreach (var jobId in jobIds)
        {
            var job = _repository.GetById(jobId);
            if (job is null)
            {
                var failResult = BackupResult.Fail(
                    new List<string> { $"Job with ID {jobId} not found." },
                    TimeSpan.Zero);
                results.Add(new JobExecutionResult(jobId, failResult));
                continue;
            }

            var strategy = _strategyFactory.Create(job.Type);
            var result = _executor.Execute(job, strategy);
            results.Add(new JobExecutionResult(jobId, result));
        }

        return results;
    }

    public List<JobExecutionResult> ExecuteAllJobs()
    {
        var jobs = _repository.GetAll();
        var jobIds = jobs.Select(j => j.Id).ToList();
        return ExecuteJobs(jobIds);
    }
}
