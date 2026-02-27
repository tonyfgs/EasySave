using Application.Ports;

namespace Application.Services;

public class BusinessSoftwareWatcher
{
    private readonly IBusinessSoftwareDetector _detector;
    private readonly IBusinessSoftwareConfig _config;
    private readonly BackupExecutor _executor;
    private readonly TimeSpan _pollInterval;
    private CancellationTokenSource? _cts;
    private Task? _pollingTask;
    private bool _wasBlocking;

    public BusinessSoftwareWatcher(
        IBusinessSoftwareDetector detector,
        IBusinessSoftwareConfig config,
        BackupExecutor executor,
        TimeSpan? pollInterval = null)
    {
        _detector = detector;
        _config = config;
        _executor = executor;
        _pollInterval = pollInterval ?? TimeSpan.FromSeconds(2);
    }

    public void Start()
    {
        if (_cts != null) return; // already running
        _cts = new CancellationTokenSource();
        _wasBlocking = false;
        _pollingTask = PollLoopAsync(_cts.Token);
    }

    public async Task StopAsync()
    {
        if (_cts == null) return;
        _cts.Cancel();
        try
        {
            if (_pollingTask != null)
                await _pollingTask.ConfigureAwait(false);
        }
        catch (OperationCanceledException) { }
        _cts.Dispose();
        _cts = null;
        _pollingTask = null;
    }

    private async Task PollLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_pollInterval, ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            if (!_config.IsDetectionEnabled())
            {
                _wasBlocking = false;
                continue;
            }

            var isBlocking = _detector.GetStatus().IsBlocking();

            if (isBlocking && !_wasBlocking)
            {
                // Transition: not blocking -> blocking -> auto-pause all running jobs
                _executor.AutoPauseAllJobs();
                _wasBlocking = true;
            }
            else if (!isBlocking && _wasBlocking)
            {
                // Transition: blocking -> not blocking -> auto-resume only auto-paused jobs
                _executor.AutoResumeAllJobs();
                _wasBlocking = false;
            }
        }
    }
}
