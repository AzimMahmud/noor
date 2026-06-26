using System.Timers;
using Noor.Models;
using Quartz;
using Quartz.Impl;
using Timer = System.Timers.Timer;

namespace Noor.Services;

/// <summary>
/// Service for scheduling prayer time reminders and daily recalculation using Quartz.NET.
/// </summary>
public class SchedulerService : IDisposable
{
    private readonly PrayerCalculatorService _prayerCalculator;
    private AppSettings _settings;
    private readonly Timer _dailyRecalculationTimer;
    private IScheduler? _scheduler;
    private readonly Dictionary<PrayerType, (DateTime time, CancellationTokenSource cts)> _activePrayers = new();
    private bool _isUpdating = false;

    /// <summary>
    /// Event raised when a prayer time begins.
    /// </summary>
    public event EventHandler<PrayerEventArgs>? PrayerTimeReached;

    /// <summary>
    /// Event raised when prayer times are recalculated.
    /// </summary>
    public event EventHandler<PrayerTimesRecalculatedEventArgs>? PrayerTimesRecalculated;

    /// <summary>
    /// Gets the current prayer times.
    /// </summary>
    public PrayerTimes? CurrentPrayerTimes { get; private set; }

    public SchedulerService(AppSettings settings, PrayerCalculatorService prayerCalculator)
    {
        _settings = settings;
        _prayerCalculator = prayerCalculator;

        _dailyRecalculationTimer = new Timer
        {
            Interval = 60000,
            AutoReset = true
        };
        _dailyRecalculationTimer.Elapsed += CheckForMidnightRecalculation;
    }

    public void UpdateSettings(AppSettings settings)
    {
        _settings = settings;
    }

    /// <summary>
    /// Starts the scheduler service.
    /// </summary>
    public async Task StartAsync()
    {
        if (_scheduler != null && !_scheduler.IsShutdown)
        {
            return;
        }

        try
        {
            // Create Quartz scheduler factory
            var schedulerFactory = new StdSchedulerFactory();
            _scheduler = await schedulerFactory.GetScheduler();
            await _scheduler.Start();

            // Calculate initial prayer times
            await RecalculatePrayerTimesAsync();

            // Start the midnight check timer
            _dailyRecalculationTimer.Start();

            // Schedule all prayer jobs for today
            await ScheduleTodaysPrayersAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error starting scheduler: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Stops the scheduler service.
    /// </summary>
    public async Task StopAsync()
    {
        _dailyRecalculationTimer.Stop();

        // Cancel all active prayers
        foreach (var (_, (_, cts)) in _activePrayers)
        {
            cts.Cancel();
        }
        _activePrayers.Clear();

        if (_scheduler != null && !_scheduler.IsShutdown)
        {
            await _scheduler.Shutdown(waitForJobsToComplete: true);
        }
    }

    /// <summary>
    /// Recalculates prayer times based on current settings.
    /// </summary>
    public async Task RecalculatePrayerTimesAsync()
    {
        if (_settings.Location == null || _isUpdating)
        {
            return;
        }

        _isUpdating = true;
        try
        {
            CurrentPrayerTimes = _prayerCalculator.CalculateForToday(
                _settings.Location,
                _settings.Madhab,
                _settings.CalculationMethod
            );

            PrayerTimesRecalculated?.Invoke(this, new PrayerTimesRecalculatedEventArgs(CurrentPrayerTimes));
        }
        finally
        {
            _isUpdating = false;
        }
    }

    /// <summary>
    /// Schedules all prayer jobs for the current day.
    /// </summary>
    private async Task ScheduleTodaysPrayersAsync()
    {
        if (_scheduler == null || CurrentPrayerTimes == null)
        {
            return;
        }

        var now = DateTime.Now;

        foreach ((PrayerType prayer, TimeSpan time) in CurrentPrayerTimes.DailyPrayers)
        {
            var prayerDateTime = DateOnly.FromDateTime(now).ToDateTime(time);

            // Skip if prayer time has passed for today
            if (prayerDateTime < now.AddMinutes(-1))
            {
                continue;
            }

            await SchedulePrayerJob(prayer, prayerDateTime);
        }
    }

    /// <summary>
    /// Schedules a single prayer job.
    /// </summary>
    private async Task SchedulePrayerJob(PrayerType prayer, DateTime prayerTime)
    {
        if (_scheduler == null)
        {
            return;
        }

        var jobKey = new JobKey($"PrayerJob_{prayer}_{prayerTime:yyyyMMdd_HHmm}");
        var triggerKey = new TriggerKey($"PrayerTrigger_{prayer}_{prayerTime:yyyyMMdd_HHmm}");

        // Create job data
        var jobData = new JobDataMap
        {
            ["prayerType"] = prayer,
            ["scheduledTime"] = prayerTime
        };

        var job = JobBuilder.Create<PrayerJob>()
            .WithIdentity(jobKey)
            .UsingJobData(jobData)
            .Build();

        var trigger = TriggerBuilder.Create()
            .WithIdentity(triggerKey)
            .StartAt(prayerTime)
            .WithDescription($"Prayer time job for {prayer} at {prayerTime:HH:mm}")
            .Build();

        // Set up a timer for this prayer
        var cts = new CancellationTokenSource();
        var timeUntilPrayer = prayerTime - DateTime.Now;

        if (timeUntilPrayer > TimeSpan.Zero)
        {
            var prayerTimer = new Timer(timeUntilPrayer.TotalMilliseconds);
            prayerTimer.Elapsed += (s, e) =>
            {
                if (!cts.Token.IsCancellationRequested)
                {
                    OnPrayerTimeReached(prayer, prayerTime);
                    prayerTimer.Stop();
                    prayerTimer.Dispose();
                }
            };
            prayerTimer.Start();
            _activePrayers[prayer] = (prayerTime, cts);
        }

        await _scheduler.ScheduleJob(job, trigger);
    }

    /// <summary>
    /// Handles when a prayer time is reached.
    /// </summary>
    private void OnPrayerTimeReached(PrayerType prayer, DateTime scheduledTime)
    {
        PrayerTimeReached?.Invoke(this, new PrayerEventArgs(prayer, scheduledTime));

        // Remove from active prayers
        if (_activePrayers.TryGetValue(prayer, out var value))
        {
            value.cts.Cancel();
            _activePrayers.Remove(prayer);
        }
    }

    /// <summary>
    /// Checks if we've passed midnight and need to recalculate.
    /// </summary>
    private void CheckForMidnightRecalculation(object? sender, ElapsedEventArgs e)
    {
        var now = DateTime.Now;

        // If it's past midnight and we haven't recalculated yet (or last recalculation was yesterday)
        if (CurrentPrayerTimes != null && now.Date > CurrentPrayerTimes.Date.ToDateTime(TimeOnly.MinValue).Date)
        {
            // Recalculate on background thread
            Task.Run(async () =>
            {
                await RecalculatePrayerTimesAsync();
                await ScheduleTodaysPrayersAsync();
            });
        }
    }

    /// <summary>
    /// Gets the next scheduled prayer.
    /// </summary>
    public (PrayerType type, DateTime time, TimeSpan remaining)? GetNextPrayer()
    {
        if (CurrentPrayerTimes == null)
        {
            return null;
        }

        // Try to get next prayer from today's times
        var result = _prayerCalculator.GetNextPrayerFromTimes(CurrentPrayerTimes);
        if (result != null)
        {
            return (result.Value.prayerType, result.Value.prayerTime, result.Value.timeUntil);
        }

        // All prayers for today have passed - return tomorrow's Fajr
        if (_settings.Location == null)
        {
            return null;
        }

        var tomorrow = DateOnly.FromDateTime(DateTime.Today).AddDays(1);
        var tomorrowPrayers = _prayerCalculator.CalculatePrayerTimes(
            tomorrow,
            _settings.Location,
            _settings.Madhab,
            _settings.CalculationMethod);

        var fajrDateTime = tomorrow.ToDateTime(tomorrowPrayers.Fajr);
        return (PrayerType.Fajr, fajrDateTime, fajrDateTime - DateTime.Now);
    }

    /// <summary>
    /// Gets the current prayer (if active).
    /// </summary>
    public PrayerType? GetCurrentPrayer()
    {
        if (CurrentPrayerTimes == null)
        {
            return null;
        }

        return _prayerCalculator.GetCurrentPrayer(CurrentPrayerTimes, _settings.BlockDurationMinutes);
    }

    public void Dispose()
    {
        _dailyRecalculationTimer?.Dispose();

        foreach (var (_, (_, cts)) in _activePrayers)
        {
            cts.Dispose();
        }
        _activePrayers.Clear();
    }
}

/// <summary>
/// Quartz job for executing prayer time tasks.
/// </summary>
internal class PrayerJob : IJob
{
    public Task Execute(IJobExecutionContext context)
    {
        var prayerType = context.JobDetail.JobDataMap["prayerType"] as PrayerType? ?? PrayerType.Fajr;
        var scheduledTime = context.JobDetail.JobDataMap["scheduledTime"] as DateTime? ?? DateTime.Now;

        // The actual event is raised by the SchedulerService's timer
        // This job serves as a backup and persistence mechanism
        return Task.CompletedTask;
    }
}

/// <summary>
/// Event arguments for prayer time reached event.
/// </summary>
public class PrayerEventArgs : EventArgs
{
    public PrayerType Prayer { get; }
    public DateTime ScheduledTime { get; }

    public PrayerEventArgs(PrayerType prayer, DateTime scheduledTime)
    {
        Prayer = prayer;
        ScheduledTime = scheduledTime;
    }
}

/// <summary>
/// Event arguments for prayer times recalculated event.
/// </summary>
public class PrayerTimesRecalculatedEventArgs : EventArgs
{
    public PrayerTimes PrayerTimes { get; }

    public PrayerTimesRecalculatedEventArgs(PrayerTimes prayerTimes)
    {
        PrayerTimes = prayerTimes;
    }
}
