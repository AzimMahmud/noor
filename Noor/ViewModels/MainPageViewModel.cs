using System.ComponentModel;
using System.Runtime.CompilerServices;
using Noor.Models;
using Noor.Services;

namespace Noor;

public class MainPageViewModel : INotifyPropertyChanged
{
    private readonly PrayerCalculatorService _prayerCalculator;
    private readonly SchedulerService _scheduler;
    private readonly LocationService _locationService;
    private AppSettings _settings;
    private readonly AudioService _audioService;
    private readonly NotificationService _notificationService;

    private DateTime _currentTime = DateTime.Now;
    private string _greeting = "Loading...";
    private string _locationDisplay = "Unknown Location";
    private PrayerTimes? _todayPrayers;
    private PrayerType? _nextPrayer;
    private DateTime? _nextPrayerTime;
    private TimeSpan? _timeUntilNextPrayer;
    private string _countdownDisplay = "--:--:--";
    private bool _isLoading = true;

    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler<PrayerEventArgs>? PrayerTimeReached;

    public MainPageViewModel()
    {
        _settings = new AppSettings();
        _prayerCalculator = new PrayerCalculatorService();
        _locationService = new LocationService();
        _scheduler = new SchedulerService(_settings, _prayerCalculator);
        _audioService = new AudioService(_settings.AzanFilePath ?? "");
        _notificationService = new NotificationService(_settings, _audioService);

        _scheduler.PrayerTimesRecalculated += OnPrayerTimesRecalculated;
        _scheduler.PrayerTimeReached += OnSchedulerPrayerTimeReached;
    }

    public DateTime CurrentTime
    {
        get => _currentTime;
        set
        {
            _currentTime = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CurrentTimeDisplay));
            UpdateGreeting();
        }
    }

    public string CurrentTimeDisplay
    {
        get
        {
            var format = _settings.Use24HourFormat ? "HH:mm:ss" : "hh:mm:ss tt";
            return CurrentTime.ToString($"dddd, MMMM d, yyyy {format}");
        }
    }

    public string Greeting
    {
        get => _greeting;
        set { _greeting = value; OnPropertyChanged(); }
    }

    public string LocationDisplay
    {
        get => _locationDisplay;
        set { _locationDisplay = value; OnPropertyChanged(); }
    }

    public PrayerTimes? TodayPrayers
    {
        get => _todayPrayers;
        set { _todayPrayers = value; OnPropertyChanged(); }
    }

    public PrayerType? NextPrayer
    {
        get => _nextPrayer;
        set
        {
            _nextPrayer = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(NextPrayerName));
            OnPropertyChanged(nameof(NextPrayerNameArabic));
        }
    }

    public string NextPrayerName => NextPrayer?.GetDisplayName() ?? "--";
    public string NextPrayerNameArabic => NextPrayer?.GetArabicName() ?? "--";

    public DateTime? NextPrayerTime
    {
        get => _nextPrayerTime;
        set { _nextPrayerTime = value; OnPropertyChanged(); }
    }

    public TimeSpan? TimeUntilNextPrayer
    {
        get => _timeUntilNextPrayer;
        set { _timeUntilNextPrayer = value; OnPropertyChanged(); }
    }

    public string CountdownDisplay
    {
        get => _countdownDisplay;
        set { _countdownDisplay = value; OnPropertyChanged(); }
    }

    public bool IsLoading
    {
        get => _isLoading;
        set { _isLoading = value; OnPropertyChanged(); }
    }

    public AppSettings Settings => _settings;

    public async Task LoadSettingsAsync()
    {
        _settings = await AppSettings.LoadAsync(AppSettings.SettingsPath);
        OnPropertyChanged(nameof(Settings));
        OnPropertyChanged(nameof(CurrentTimeDisplay));
    }

    public async Task SaveSettingsAsync()
    {
        await _settings.SaveAsync(AppSettings.SettingsPath);
        OnPropertyChanged(nameof(CurrentTimeDisplay));
    }

    public async Task InitializeAsync()
    {
        try
        {
            await LoadSettingsAsync();
            var location = await _locationService.GetLocationFromIpAsync();

            if (location == null)
                location = new Coordinates(21.4225, 39.8262, "Makkah", "Saudi Arabia", 3.0);

            _settings.Location = location;
            LocationDisplay = location.DisplayString;

            _scheduler.UpdateSettings(_settings);
            _audioService.UpdateFilePath(_settings.AzanFilePath ?? "");

            await _scheduler.StartAsync();
            UpdatePrayerDisplay();
            IsLoading = false;
        }
        catch (Exception ex)
        {
            Greeting = "Error loading prayer times";
            IsLoading = false;
            System.Diagnostics.Debug.WriteLine($"[INIT] Error: {ex.Message}");
        }
    }

    public void UpdateCountdown()
    {
        if (!NextPrayerTime.HasValue)
        {
            CountdownDisplay = "--:--:--";
            return;
        }

        var remaining = NextPrayerTime.Value - DateTime.Now;

        if (remaining.TotalSeconds > 0)
        {
            TimeUntilNextPrayer = remaining;
            CountdownDisplay = $"{remaining.Hours:D2}:{remaining.Minutes:D2}:{remaining.Seconds:D2}";
        }
        else if (remaining.TotalSeconds > -60)
        {
            CountdownDisplay = "00:00:00";
            TimeUntilNextPrayer = TimeSpan.Zero;
        }
        else
        {
            UpdatePrayerDisplay();
        }
    }

    public void UpdatePrayerDisplay()
    {
        TodayPrayers = _scheduler.CurrentPrayerTimes;
        if (TodayPrayers == null) return;

        var nextPrayer = _scheduler.GetNextPrayer();
        if (nextPrayer != null)
        {
            NextPrayer = nextPrayer.Value.type;
            NextPrayerTime = nextPrayer.Value.time;
            TimeUntilNextPrayer = nextPrayer.Value.remaining;
            UpdateGreeting();
        }
    }

    private void UpdateGreeting()
    {
        var hour = DateTime.Now.Hour;
        if (hour < 6) Greeting = "Good Night";
        else if (hour < 12) Greeting = "Good Morning";
        else if (hour < 17) Greeting = "Good Afternoon";
        else if (hour < 20) Greeting = "Good Evening";
        else Greeting = "Good Night";
    }

    private void OnPrayerTimesRecalculated(object? sender, PrayerTimesRecalculatedEventArgs e)
    {
        UpdatePrayerDisplay();
    }

    private void OnSchedulerPrayerTimeReached(object? sender, PrayerEventArgs e)
    {
        _notificationService.ShowPrayerNotification(e.Prayer, e.ScheduledTime);
        PrayerTimeReached?.Invoke(this, e);
    }

    public async Task RefreshAsync()
    {
        IsLoading = true;
        await _scheduler.RecalculatePrayerTimesAsync();
        UpdatePrayerDisplay();
        IsLoading = false;
    }

    public async Task ShutdownAsync()
    {
        await _scheduler.StopAsync();
        _scheduler.Dispose();
        _locationService.Dispose();
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
