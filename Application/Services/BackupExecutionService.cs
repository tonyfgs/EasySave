using Application.DTOs;
using Application.Ports;
using Model;

namespace Application.Services;

public class BackupExecutionService
{
    private readonly IJobRepository _repository;
    private readonly BackupExecutor _executor;
    private readonly BackupStrategyFactory _strategyFactory;
    private readonly BusinessSoftwareWatcher _watcher;
    private readonly IBusinessSoftwareDetector _detector;
    private readonly IBusinessSoftwareConfig _detectorConfig;

    public BackupExecutionService(
        IJobRepository repository,
        BackupExecutor executor,
        BackupStrategyFactory strategyFactory,
        BusinessSoftwareWatcher watcher,
        IBusinessSoftwareDetector detector,
        IBusinessSoftwareConfig detectorConfig)
    {
        _repository = repository;
        _executor = executor;
        _strategyFactory = strategyFactory;
        _watcher = watcher;
        _detector = detector;
        _detectorConfig = detectorConfig;
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

            // Pre-flight check: block if business software is running
            if (_detectorConfig.IsDetectionEnabled())
            {
                var status = _detector.GetStatus();
                if (status.IsBlocking())
                {
                    earlyFailures.Add(new JobExecutionResult(jobId,
                        BackupResult.Fail(
                            new List<string> { $"Business software detected ({status})" },
                            TimeSpan.Zero)));
                    break;
                }
            }

            validJobs.Add((job, _strategyFactory.Create(job.Type)));
        }

        // V3: start business software watcher for auto-pause during execution
        _watcher.Start();
        try
        {
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
        finally
        {
            await _watcher.StopAsync().ConfigureAwait(false);
        }
    }

    public async Task<List<JobExecutionResult>> ExecuteAllJobsAsync(CancellationToken ct = default)
    {
        var jobs = _repository.GetAll();
        var jobIds = jobs.Select(j => j.Id).ToList();
        return await ExecuteJobsAsync(jobIds, ct).ConfigureAwait(false);
    }
}
