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

    [Obsolete("Deadlocks on UI threads. Use ExecuteJobsAsync instead.")]
    public List<JobExecutionResult> ExecuteJobs(List<int> jobIds)
        => ExecuteJobsAsync(jobIds).GetAwaiter().GetResult();

    [Obsolete("Deadlocks on UI threads. Use ExecuteAllJobsAsync instead.")]
    public List<JobExecutionResult> ExecuteAllJobs()
        => ExecuteAllJobsAsync().GetAwaiter().GetResult();

    public async Task<List<JobExecutionResult>> ExecuteJobsAsync(
        List<int> jobIds, CancellationToken ct = default)
    {
        if (jobIds.Count == 0) return new List<JobExecutionResult>();

        ct.ThrowIfCancellationRequested();

        // Phase 1 — sequential pre-flight: resolve jobs and detect blocking software
        var earlyFailures = new List<JobExecutionResult>();
        var validJobs = new List<(BackupJob Job, IBackupStrategy Strategy)>();

        foreach (var jobId in jobIds)
        {
            var job = _repository.GetById(jobId);
            if (job is null)
            {
                earlyFailures.Add(new JobExecutionResult(jobId,
                    BackupResult.Fail(
                        new List<string> { $"Job with ID {jobId} not found." },
                        TimeSpan.Zero)));
                continue;
            }

            if (_detectorConfig.IsDetectionEnabled())
            {
                var status = _detector.GetStatus();
                if (status.IsBlocking())
                {
                    _eventBus.Publish(new BusinessSoftwareDetectedEvent(
                        job.Name, status, DateTime.UtcNow));
                    earlyFailures.Add(new JobExecutionResult(jobId,
                        BackupResult.Fail(
                            new List<string> { $"Business software detected ({status})" },
                            TimeSpan.Zero)));
                    // Fail-safe: one blocking software aborts all remaining jobs
                    break;
                }
            }

            validJobs.Add((job, _strategyFactory.Create(job.Type)));
        }

        // Phase 2 — execute all valid jobs in parallel
        var tasks = validJobs.Select(async entry =>
        {
            var result = await _executor.ExecuteAsync(entry.Job, entry.Strategy, ct).ConfigureAwait(false);
            _repository.Update(entry.Job);
            return new JobExecutionResult(entry.Job.Id, result);
        });

        var parallelResults = await Task.WhenAll(tasks).ConfigureAwait(false);

        var combined = new List<JobExecutionResult>(earlyFailures);
        combined.AddRange(parallelResults);
        return combined;
    }

    public async Task<List<JobExecutionResult>> ExecuteAllJobsAsync(CancellationToken ct = default)
    {
        var jobs = _repository.GetAll();
        var jobIds = jobs.Select(j => j.Id).ToList();
        return await ExecuteJobsAsync(jobIds, ct).ConfigureAwait(false);
    }
}
