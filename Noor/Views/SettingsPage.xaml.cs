using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Navigation;
using Noor.Models;
using Noor.Services;
using System;
using System.Threading.Tasks;

namespace Noor.Views;

public sealed partial class SettingsPage : Page
{
    private MainPageViewModel? _mainViewModel;
    private bool _isLoaded;

    public SettingsPage()
    {
        this.InitializeComponent();
        Loaded += OnLoaded;
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        if (e.Parameter is MainPageViewModel viewModel)
            _mainViewModel = viewModel;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _isLoaded = false;
        LoadSettings();
        _isLoaded = true;
    }

    private void LoadSettings()
    {
        if (_mainViewModel == null) return;
        var settings = _mainViewModel.Settings;

        if (settings.Location != null)
        {
            CityNameBox.Text = settings.Location.City ?? "Makkah";
            CountryNameBox.Text = settings.Location.Country ?? "Saudi Arabia";
            LatitudeBox.Text = settings.Location.Latitude.ToString("F4");
            LongitudeBox.Text = settings.Location.Longitude.ToString("F4");

            foreach (ComboBoxItem item in TimeZoneBox.Items)
            {
                if (item.Tag is string tag && int.TryParse(tag, out int tz))
                {
                    if (Math.Abs(tz - settings.Location.TimeZoneOffset) < 0.1)
                    {
                        TimeZoneBox.SelectedItem = item;
                        break;
                    }
                }
            }
        }

        foreach (ComboBoxItem item in MadhabBox.Items)
        {
            if (item.Tag?.ToString() == settings.Madhab.ToString())
            {
                MadhabBox.SelectedItem = item;
                break;
            }
        }

        foreach (ComboBoxItem item in CalculationMethodBox.Items)
        {
            if (item.Tag?.ToString() == settings.CalculationMethod.ToString())
            {
                CalculationMethodBox.SelectedItem = item;
                break;
            }
        }

        EnableNotificationsToggle.IsOn = settings.EnableNotifications;
        EnableAzanToggle.IsOn = settings.PlayAzan;
        Use24HourToggle.IsOn = settings.Use24HourFormat;
        DarkModeToggle.IsOn = settings.IsDarkMode;
        ShowBlockScreenToggle.IsOn = settings.ShowBlockScreen;
        BlockDurationSlider.Value = settings.BlockDurationMinutes;
        BlockDurationText.Text = settings.BlockDurationMinutes.ToString();

        ApplyTheme();
    }

    // ==================== NAVIGATION ====================

    private void OnBackClick(object sender, RoutedEventArgs e)
    {
        if (Frame.CanGoBack)
            Frame.GoBack();
    }

    // ==================== SAVE SETTINGS ====================

    private async void OnSaveClick(object sender, RoutedEventArgs e)
    {
        if (_mainViewModel == null) return;

        SaveButton.Content = "Saving...";
        SaveButton.IsEnabled = false;

        try
        {
            if (double.TryParse(LatitudeBox.Text, out double lat) &&
                double.TryParse(LongitudeBox.Text, out double lng))
            {
                int timezone = 0;
                if (TimeZoneBox.SelectedItem is ComboBoxItem tzItem && tzItem.Tag is string tag)
                    int.TryParse(tag, out timezone);

                var location = new Coordinates(
                    lat, lng,
                    string.IsNullOrWhiteSpace(CityNameBox.Text) ? null : CityNameBox.Text,
                    string.IsNullOrWhiteSpace(CountryNameBox.Text) ? null : CountryNameBox.Text,
                    timezone
                );

                var madhab = Madhab.Shafi;
                if (MadhabBox.SelectedItem is ComboBoxItem madhabItem && madhabItem.Tag?.ToString() == "Hanafi")
                    madhab = Madhab.Hanafi;

                var calcMethod = CalculationMethod.MuslimWorldLeague;
                if (CalculationMethodBox.SelectedItem is ComboBoxItem calcItem)
                    Enum.TryParse<CalculationMethod>(calcItem.Tag?.ToString(), out var method);

                _mainViewModel.Settings.Location = location;
                _mainViewModel.Settings.Madhab = madhab;
                _mainViewModel.Settings.CalculationMethod = calcMethod;
                _mainViewModel.Settings.EnableNotifications = EnableNotificationsToggle.IsOn;
                _mainViewModel.Settings.PlayAzan = EnableAzanToggle.IsOn;
                _mainViewModel.Settings.Use24HourFormat = Use24HourToggle.IsOn;
                _mainViewModel.Settings.IsDarkMode = DarkModeToggle.IsOn;
                _mainViewModel.Settings.ShowBlockScreen = ShowBlockScreenToggle.IsOn;
                _mainViewModel.Settings.BlockDurationMinutes = (int)BlockDurationSlider.Value;

                await _mainViewModel.SaveSettingsAsync();
                await _mainViewModel.RefreshAsync();

                SaveButton.Content = "Saved";
                await Task.Delay(1200);
                SaveButton.Content = "Save";
            }
            else
            {
                LatitudeBox.Text = "Invalid!";
                LongitudeBox.Text = "Invalid!";
            }
        }
        finally
        {
            SaveButton.IsEnabled = true;
        }
    }

    // ==================== LOCATION DETECTION ====================

    private async void OnDetectLocationClick(object sender, RoutedEventArgs e)
    {
        DetectLocationButton.Content = "Detecting...";
        DetectLocationButton.IsEnabled = false;

        try
        {
            var locationService = new LocationService();
            var location = await locationService.GetLocationFromIpAsync();

            if (location != null)
            {
                CityNameBox.Text = location.City ?? "Unknown";
                CountryNameBox.Text = location.Country ?? "Unknown";
                LatitudeBox.Text = location.Latitude.ToString("F4");
                LongitudeBox.Text = location.Longitude.ToString("F4");

                foreach (ComboBoxItem item in TimeZoneBox.Items)
                {
                    if (item.Tag is string tag && int.TryParse(tag, out int tz))
                    {
                        if (Math.Abs(tz - location.TimeZoneOffset) < 0.1)
                        {
                            TimeZoneBox.SelectedItem = item;
                            break;
                        }
                    }
                }
            }
            else
            {
                CityNameBox.Text = "Detection failed";
            }
        }
        finally
        {
            DetectLocationButton.Content = "Auto-detect from IP";
            DetectLocationButton.IsEnabled = true;
        }
    }

    // ==================== TOGGLE HANDLERS ====================

    private void OnEnableNotificationsToggled(object sender, RoutedEventArgs e)
    {
        if (!_isLoaded || _mainViewModel == null) return;
        _mainViewModel.Settings.EnableNotifications = EnableNotificationsToggle.IsOn;
    }

    private void OnEnableAzanToggled(object sender, RoutedEventArgs e)
    {
        if (!_isLoaded || _mainViewModel == null) return;
        _mainViewModel.Settings.PlayAzan = EnableAzanToggle.IsOn;
    }

    private void OnUse24HourToggled(object sender, RoutedEventArgs e)
    {
        if (!_isLoaded || _mainViewModel == null) return;
        _mainViewModel.Settings.Use24HourFormat = Use24HourToggle.IsOn;
    }

    private void OnShowBlockScreenToggled(object sender, RoutedEventArgs e)
    {
        if (!_isLoaded || _mainViewModel == null) return;
        _mainViewModel.Settings.ShowBlockScreen = ShowBlockScreenToggle.IsOn;
    }

    private void OnBlockDurationChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
    {
        if (!_isLoaded || _mainViewModel == null) return;
        _mainViewModel.Settings.BlockDurationMinutes = (int)e.NewValue;
        BlockDurationText.Text = ((int)e.NewValue).ToString();
    }

    // ==================== DARK MODE ====================

    private void OnDarkModeToggled(object sender, RoutedEventArgs e)
    {
        if (!_isLoaded || _mainViewModel == null) return;
        _mainViewModel.Settings.IsDarkMode = DarkModeToggle.IsOn;
        ApplyTheme();
    }

    private void ApplyTheme()
    {
        var theme = _mainViewModel?.Settings.IsDarkMode == true
            ? ElementTheme.Dark
            : ElementTheme.Light;
        if (Frame != null) Frame.RequestedTheme = theme;
    }
}
