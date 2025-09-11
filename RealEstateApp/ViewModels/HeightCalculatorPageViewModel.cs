using RealEstateApp.Models;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Microsoft.Maui.Devices.Sensors;

namespace RealEstateApp.ViewModels;

public class HeightCalculatorPageViewModel : INotifyPropertyChanged
{
    private const double SeaLevelPressure = 1013.25; // DMI sea level pressure

    public event PropertyChangedEventHandler PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string propertyName = "") =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    private double _currentPressure;
    public double CurrentPressure
    {
        get => _currentPressure;
        set
        {
            if (_currentPressure != value)
            {
                _currentPressure = value;
                OnPropertyChanged();
                UpdateAltitude();
            }
        }
    }

    private double _currentAltitude;
    public double CurrentAltitude
    {
        get => _currentAltitude;
        set { _currentAltitude = value; OnPropertyChanged(); }
    }

    private string _measurementLabel;
    public string MeasurementLabel
    {
        get => _measurementLabel;
        set { _measurementLabel = value; OnPropertyChanged(); }
    }

    public ObservableCollection<BarometerMeasurement> Measurements { get; } = new();

    public ICommand SaveMeasurementCommand { get; }

    public HeightCalculatorPageViewModel()
    {
        // Default starting pressure
        CurrentPressure = SeaLevelPressure;
        SaveMeasurementCommand = new Command(SaveMeasurement);
    }

    private void UpdateAltitude()
    {
        CurrentAltitude = 44307.694 * (1 - Math.Pow(CurrentPressure / SeaLevelPressure, 0.190284));
    }

    private void SaveMeasurement()
    {
        if (string.IsNullOrWhiteSpace(MeasurementLabel))
            MeasurementLabel = "Unnamed Measurement";

        var measurement = new BarometerMeasurement
        {
            Pressure = CurrentPressure,
            Altitude = CurrentAltitude,
            Label = MeasurementLabel
        };

        // Check for previous measurement
        if (Measurements.Count > 0)
        {
            var previous = Measurements[^1];
            measurement.HeightChange = measurement.Altitude - previous.Altitude;
        }

        Measurements.Add(measurement);

        // Clear label for next measurement
        MeasurementLabel = string.Empty;
    }
}
