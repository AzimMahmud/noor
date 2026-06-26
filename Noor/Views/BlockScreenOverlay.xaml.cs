using System;
using System.Timers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Noor.Models;
using Timer = System.Timers.Timer;

namespace Noor.Views;

public sealed partial class BlockScreenOverlay : UserControl
{
    private readonly Timer _countdownTimer;
    private readonly int _durationMinutes;
    private TimeSpan _remaining;
    public event EventHandler? Dismissed;

    public BlockScreenOverlay(PrayerType prayer, int durationMinutes = 20)
    {
        this.InitializeComponent();

        _durationMinutes = durationMinutes;
        _remaining = TimeSpan.FromMinutes(durationMinutes);

        PrayerNameText.Text = prayer.GetDisplayName();
        PrayerArabicText.Text = prayer.GetArabicName();
        UpdateCountdownDisplay();

        _countdownTimer = new Timer(1000);
        _countdownTimer.Elapsed += OnCountdownElapsed;
        _countdownTimer.AutoReset = true;
    }

    public void Show()
    {
        Visibility = Visibility.Visible;
        _countdownTimer.Start();
    }

    private void OnCountdownElapsed(object? sender, ElapsedEventArgs e)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            _remaining = _remaining.Subtract(TimeSpan.FromSeconds(1));

            if (_remaining.TotalSeconds <= 0)
            {
                _countdownTimer.Stop();
                Dismiss();
            }
            else
            {
                UpdateCountdownDisplay();
            }
        });
    }

    private void UpdateCountdownDisplay()
    {
        CountdownText.Text = $"{(int)_remaining.TotalMinutes:D2}:{_remaining.Seconds:D2}";
    }

    private void OnDismissClick(object sender, RoutedEventArgs e)
    {
        _countdownTimer.Stop();
        Dismiss();
    }

    private void Dismiss()
    {
        Visibility = Visibility.Collapsed;
        Dismissed?.Invoke(this, EventArgs.Empty);
    }
}
