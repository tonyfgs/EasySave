using Application.DTOs;
using Application.Events;
using Application.Ports;
using Model;

namespace Application.Services;

public class BackupExecutionService
{
    private readonly IJobRepository _repository;
    private readonly BackupExecutor _executor;
    private readonly BackupStrategyFactory _strategyFactory;
    private readonly IBusinessSoftwareDetector _detector;
    private readonly IBusinessSoftwareConfig _detectorConfig;
    private readonly IEventBus _eventBus;

    public BackupExecutionService(
        IJobRepository repository,
        BackupExecutor executor,
        BackupStrategyFactory strategyFactory,
        IBusinessSoftwareDetector detector,
        IBusinessSoftwareConfig detectorConfig,
        IEventBus eventBus)
    {
        _repository = repository;
        _executor = executor;
        _strategyFactory = strategyFactory;
        _detector = detector;
        _detectorConfig = detectorConfig;
        _eventBus = eventBus;
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
            _repository.Update(job);
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
