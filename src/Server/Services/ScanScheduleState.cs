namespace Server.Services;

public class ScanScheduleState
{
    private readonly object _gate = new();
    private DateTime? _lastRunUtc;
    private DateTime? _nextRunUtc;
    private int _intervalHours;

    public ScanScheduleState(int intervalHours)
    {
        _intervalHours = intervalHours;
    }

    public void SetIntervalHours(int hours)
    {
        lock (_gate) _intervalHours = hours;
    }

    public void UpdateOnRun(DateTime runStartedUtc)
    {
        lock (_gate)
        {
            _lastRunUtc = runStartedUtc;
            _nextRunUtc = runStartedUtc.AddHours(_intervalHours);
        }
    }

    public (DateTime? lastRunUtc, DateTime? nextRunUtc, int intervalHours) Snapshot()
    {
        lock (_gate) return (_lastRunUtc, _nextRunUtc, _intervalHours);
    }
}
