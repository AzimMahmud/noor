using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using Noor.Models;
using Noor.Services;
using Timer = System.Timers.Timer;

namespace Noor.Views;

public sealed partial class MainPage : Page
{
    private readonly MainPageViewModel _viewModel;
    private readonly Timer _updateTimer;
    private BlockScreenOverlay? _blockOverlay;

    public MainPage()
    {
        this.InitializeComponent();
        _viewModel = new MainPageViewModel();
        DataContext = _viewModel;

        _updateTimer = new Timer(1000);
        _updateTimer.Elapsed += OnUpdateTimer;
        _updateTimer.AutoReset = true;

        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        _viewModel.PropertyChanged += OnPropertyChanged;
        _viewModel.PrayerTimeReached += OnPrayerTimeReached;

        await _viewModel.InitializeAsync();
        ApplyTheme();

        _updateTimer.Start();
        UpdateUI();
        PlayEntranceAnimations();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        ApplyTheme();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        _updateTimer?.Stop();
        _updateTimer?.Dispose();
        _viewModel.PropertyChanged -= OnPropertyChanged;
        _viewModel.PrayerTimeReached -= OnPrayerTimeReached;

        _ = _viewModel.ShutdownAsync();
    }

    private void OnUpdateTimer(object? sender, System.Timers.ElapsedEventArgs e)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            _viewModel.CurrentTime = DateTime.Now;
            _viewModel.UpdateCountdown();
            UpdateUI();
        });
    }

    private void OnPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            switch (e.PropertyName)
            {
                case nameof(MainPageViewModel.Greeting):
                    GreetingText.Text = _viewModel.Greeting;
                    break;
                case nameof(MainPageViewModel.LocationDisplay):
                    LocationText.Text = _viewModel.LocationDisplay;
                    break;
                case nameof(MainPageViewModel.IsLoading):
                    UpdateLoadingState();
                    break;
                case nameof(MainPageViewModel.CurrentTimeDisplay):
                    CurrentTimeText.Text = _viewModel.CurrentTimeDisplay;
                    break;
            }
        });
    }

    private void OnPrayerTimeReached(object? sender, PrayerEventArgs e)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            if (_viewModel.Settings.ShowBlockScreen)
            {
                ShowBlockOverlay(e.Prayer);
            }
        });
    }

    private void UpdateUI()
    {
        CurrentTimeText.Text = _viewModel.CurrentTimeDisplay;
        HijriDateText.Text = HijriDateService.ToHijriString(DateTime.Today);
        HijriDateHeroText.Text = HijriDateService.ToHijriString(DateTime.Today);
        NextPrayerNameText.Text = _viewModel.NextPrayerName;
        NextPrayerArabicText.Text = _viewModel.NextPrayerNameArabic;
        CountdownText.Text = _viewModel.CountdownDisplay;

        if (_viewModel.TodayPrayers == null) return;

        var use24h = _viewModel.Settings.Use24HourFormat;
        FajrTime.Text = _viewModel.TodayPrayers.Fajr.FormatDisplay(use24h);
        SunriseTime.Text = _viewModel.TodayPrayers.Sunrise.FormatDisplay(use24h);
        DhuhrTime.Text = _viewModel.TodayPrayers.Dhuhr.FormatDisplay(use24h);
        AsrTime.Text = _viewModel.TodayPrayers.Asr.FormatDisplay(use24h);
        MaghribTime.Text = _viewModel.TodayPrayers.Maghrib.FormatDisplay(use24h);
        IshaTime.Text = _viewModel.TodayPrayers.Isha.FormatDisplay(use24h);

        UpdateNextBadge(FajrNextBadge, false);
        UpdateNextBadge(DhuhrNextBadge, false);
        UpdateNextBadge(AsrNextBadge, false);
        UpdateNextBadge(MaghribNextBadge, false);
        UpdateNextBadge(IshaNextBadge, false);

        switch (_viewModel.NextPrayer)
        {
            case PrayerType.Fajr: UpdateNextBadge(FajrNextBadge, true); break;
            case PrayerType.Dhuhr: UpdateNextBadge(DhuhrNextBadge, true); break;
            case PrayerType.Asr: UpdateNextBadge(AsrNextBadge, true); break;
            case PrayerType.Maghrib: UpdateNextBadge(MaghribNextBadge, true); break;
            case PrayerType.Isha: UpdateNextBadge(IshaNextBadge, true); break;
        }
    }

    private void UpdateNextBadge(Microsoft.UI.Xaml.Controls.Border badge, bool isVisible)
    {
        badge.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
    }

    private void UpdateLoadingState()
    {
        LoadingOverlay.Visibility = _viewModel.IsLoading ? Visibility.Visible : Visibility.Collapsed;
        LoadingRing.IsActive = _viewModel.IsLoading;
    }

    // ==================== NAVIGATION ====================

    private void OnSettingsClick(object sender, RoutedEventArgs e)
    {
        Frame.Navigate(typeof(SettingsPage), _viewModel);
    }

    private void OnHijriDateTapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
    {
        Frame.Navigate(typeof(HijriCalendarPage));
    }

    private void OnCalendarClick(object sender, RoutedEventArgs e)
    {
        Frame.Navigate(typeof(HijriCalendarPage));
    }

    private async void OnRefreshClick(object sender, RoutedEventArgs e)
    {
        PlayRefreshAnimation();
        await _viewModel.RefreshAsync();
        UpdateUI();
    }

    // ==================== THEME ====================

    private void OnThemeToggleClick(object sender, RoutedEventArgs e)
    {
        _viewModel.Settings.IsDarkMode = !_viewModel.Settings.IsDarkMode;
        ApplyTheme();
        _ = _viewModel.SaveSettingsAsync();
    }

    private void ApplyTheme()
    {
        var theme = _viewModel.Settings.IsDarkMode ? ElementTheme.Dark : ElementTheme.Light;
        if (Frame != null) Frame.RequestedTheme = theme;
        ThemeIcon.Glyph = _viewModel.Settings.IsDarkMode ? "\uE706" : "\uE771";
    }

    // ==================== BLOCK SCREEN ====================

    private void ShowBlockOverlay(PrayerType prayer)
    {
        if (_blockOverlay != null) return;

        _blockOverlay = new BlockScreenOverlay(prayer, _viewModel.Settings.BlockDurationMinutes);
        _blockOverlay.Dismissed += OnBlockOverlayDismissed;
        BlockScreenHost.Children.Add(_blockOverlay);
        _blockOverlay.Show();
    }

    private void OnBlockOverlayDismissed(object? sender, EventArgs e)
    {
        if (_blockOverlay != null)
        {
            _blockOverlay.Dismissed -= OnBlockOverlayDismissed;
            BlockScreenHost.Children.Remove(_blockOverlay);
            _blockOverlay = null;
        }
    }

    // ==================== ANIMATIONS ====================

    private void PlayEntranceAnimations()
    {
        var cards = new UIElement[]
        {
            LocationCard, HeroCard, FajrCard, DhuhrCard, AsrCard, MaghribCard, IshaCard
        };

        var sb = new Storyboard();
        for (int i = 0; i < cards.Length; i++)
            AddSlideAnimation(sb, cards[i], i * 60);
        sb.Begin();
    }

    private void AddSlideAnimation(Storyboard sb, UIElement element, double delayMs)
    {
        element.Opacity = 0;
        var transform = new Microsoft.UI.Xaml.Media.CompositeTransform { TranslateY = 24 };
        element.RenderTransform = transform;

        var fadeAnim = new DoubleAnimation
        {
            From = 0,
            To = 1,
            Duration = new Duration(TimeSpan.FromMilliseconds(350)),
            BeginTime = TimeSpan.FromMilliseconds(delayMs)
        };
        fadeAnim.EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut };
        Storyboard.SetTarget(fadeAnim, element);
        Storyboard.SetTargetProperty(fadeAnim, "Opacity");
        sb.Children.Add(fadeAnim);

        var slideAnim = new DoubleAnimation
        {
            From = 24,
            To = 0,
            Duration = new Duration(TimeSpan.FromMilliseconds(350)),
            BeginTime = TimeSpan.FromMilliseconds(delayMs)
        };
        slideAnim.EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut };
        Storyboard.SetTarget(slideAnim, transform);
        Storyboard.SetTargetProperty(slideAnim, "TranslateY");
        sb.Children.Add(slideAnim);
    }

    private void PlayRefreshAnimation()
    {
        var transform = new Microsoft.UI.Xaml.Media.RotateTransform { CenterX = 0.5, CenterY = 0.5 };
        RefreshIcon.RenderTransform = transform;

        var rotateAnim = new DoubleAnimation
        {
            From = 0,
            To = 360,
            Duration = new Duration(TimeSpan.FromMilliseconds(600))
        };
        rotateAnim.EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut };

        var sb = new Storyboard();
        Storyboard.SetTarget(rotateAnim, transform);
        Storyboard.SetTargetProperty(rotateAnim, "Angle");
        sb.Children.Add(rotateAnim);
        sb.Begin();
    }
}
