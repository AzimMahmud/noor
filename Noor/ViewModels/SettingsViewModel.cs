using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Noor.ViewModels;

public class SettingsViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private string _cityName = "Makkah";
    private string _countryName = "Saudi Arabia";
    private double _latitude = 21.4225;
    private double _longitude = 39.8262;
    private double _timezoneOffset = 3.0;
    private Models.Madhab _madhab = Models.Madhab.Hanafi;
    private Models.CalculationMethod _calculationMethod = Models.CalculationMethod.MuslimWorldLeague;
    private bool _enableNotifications = true;
    private bool _enableAzan = true;

    public string CityName
    {
        get => _cityName;
        set { _cityName = value; OnPropertyChanged(); }
    }

    public string CountryName
    {
        get => _countryName;
        set { _countryName = value; OnPropertyChanged(); }
    }

    public double Latitude
    {
        get => _latitude;
        set { _latitude = value; OnPropertyChanged(); }
    }

    public double Longitude
    {
        get => _longitude;
        set { _longitude = value; OnPropertyChanged(); }
    }

    public double TimezoneOffset
    {
        get => _timezoneOffset;
        set { _timezoneOffset = value; OnPropertyChanged(); }
    }

    public Models.Madhab Madhab
    {
        get => _madhab;
        set { _madhab = value; OnPropertyChanged(); }
    }

    public Models.CalculationMethod CalculationMethod
    {
        get => _calculationMethod;
        set { _calculationMethod = value; OnPropertyChanged(); }
    }

    public bool EnableNotifications
    {
        get => _enableNotifications;
        set { _enableNotifications = value; OnPropertyChanged(); }
    }

    public bool EnableAzan
    {
        get => _enableAzan;
        set { _enableAzan = value; OnPropertyChanged(); }
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
