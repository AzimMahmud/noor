using Noor.Models;

namespace Noor.Services;

public class NotificationService
{
    private readonly AppSettings _settings;
    private readonly AudioService _audioService;

    public event EventHandler<string>? NotificationShown;

    public NotificationService(AppSettings settings, AudioService audioService)
    {
        _settings = settings;
        _audioService = audioService;
    }

    public void ShowPrayerNotification(PrayerType prayer, DateTime scheduledTime)
    {
        if (!_settings.EnableNotifications)
            return;

        var prayerName = prayer.GetDisplayName();
        var arabicName = prayer.GetArabicName();

        System.Diagnostics.Debug.WriteLine($"[NOTIFICATION] {prayerName} prayer time reached at {scheduledTime:HH:mm}");

        if (_settings.PlayAzan)
        {
            try
            {
                _audioService.PlayAzan(_settings.Volume);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[NOTIFICATION] Error playing azan: {ex.Message}");
            }
        }

        ShowPlatformNotification(prayerName, arabicName, scheduledTime);
        NotificationShown?.Invoke(this, $"{prayerName} prayer time reached");
    }

    public void ShowCountdownNotification(PrayerType nextPrayer, TimeSpan timeUntil)
    {
        if (!_settings.EnableNotifications)
            return;

        if (timeUntil.TotalMinutes <= _settings.NotificationMinutesBefore && timeUntil.TotalMinutes > 0)
        {
            var prayerName = nextPrayer.GetDisplayName();
            var minutes = (int)timeUntil.TotalMinutes;

            System.Diagnostics.Debug.WriteLine($"[NOTIFICATION] {minutes} minutes until {prayerName}");

            ShowPlatformNotification(
                "Prayer Reminder",
                $"{minutes} minute{(minutes > 1 ? "s" : "")} until {prayerName}",
                DateTime.Now.Add(timeUntil)
            );
        }
    }

    private void ShowPlatformNotification(string title, string message, DateTime? dueTime = null)
    {
#if WINDOWS
        ShowWindowsNotification(title, message, dueTime);
#else
        System.Diagnostics.Debug.WriteLine($"[NOTIFICATION] {title}: {message}");
        if (dueTime.HasValue)
            System.Diagnostics.Debug.WriteLine($"[NOTIFICATION] Due at {dueTime:HH:mm}");
#endif
    }

#if WINDOWS
    private void ShowWindowsNotification(string title, string message, DateTime? dueTime)
    {
        System.Diagnostics.Debug.WriteLine($"[WIN_NOTIFICATION] {title}: {message}");
    }
#endif

    public void ShowTestNotification()
    {
        ShowPlatformNotification(
            "Salat Reminder Test",
            "Notifications are working correctly!",
            DateTime.Now.AddMinutes(5)
        );
    }
}
