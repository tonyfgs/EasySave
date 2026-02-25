using Application.Concurrency;
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
        => ExecuteJobsAsync(jobIds).GetAwaiter().GetResult();

    public List<JobExecutionResult> ExecuteAllJobs()
        => ExecuteAllJobsAsync().GetAwaiter().GetResult();

    public async Task<List<JobExecutionResult>> ExecuteJobsAsync(
        List<int> jobIds, CancellationToken ct = default)
    {
        var results = new List<JobExecutionResult>();
        var pauseGate = new AsyncManualResetEvent(initialState: true);

        foreach (var jobId in jobIds)
        {
            ct.ThrowIfCancellationRequested();

            var job = _repository.GetById(jobId);
            if (job is null)
            {
                var failResult = BackupResult.Fail(
                    new List<string> { $"Job with ID {jobId} not found." },
                    TimeSpan.Zero);
                results.Add(new JobExecutionResult(jobId, failResult));
                continue;
            }

            // Pre-flight business software detection
            if (_detectorConfig.IsDetectionEnabled())
            {
                var status = _detector.GetStatus();
                if (status.IsBlocking())
                {
                    _eventBus.Publish(new BusinessSoftwareDetectedEvent(
                        job.Name, status, DateTime.Now));
                    var failResult = BackupResult.Fail(
                        new List<string> { $"Business software detected ({status})" },
                        TimeSpan.Zero);
                    results.Add(new JobExecutionResult(jobId, failResult));
                    break;
                }
            }

            var strategy = _strategyFactory.Create(job.Type);
            var result = await _executor.ExecuteAsync(job, strategy, pauseGate, ct).ConfigureAwait(false);
            _repository.Update(job);
            results.Add(new JobExecutionResult(jobId, result));
        }

        return results;
    }

    public async Task<List<JobExecutionResult>> ExecuteAllJobsAsync(CancellationToken ct = default)
    {
        var jobs = _repository.GetAll();
        var jobIds = jobs.Select(j => j.Id).ToList();
        return await ExecuteJobsAsync(jobIds, ct).ConfigureAwait(false);
    }
}
