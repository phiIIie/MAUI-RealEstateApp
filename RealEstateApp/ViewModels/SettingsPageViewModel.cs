// ViewModels/SettingsPageViewModel.cs
using GalaSoft.MvvmLight;
using Microsoft.Maui.Storage;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace RealEstateApp.ViewModels;

public partial class SettingsPageViewModel : INotifyPropertyChanged
{
    private const double DefaultVolume = 0.5;
    private const double DefaultPitch = 1.0;

    private double volume;
    public double Volume
    {
        get => volume;
        set
        {
            if (volume != value)
            {
                volume = value;
                OnPropertyChanged();
            }
        }
    }

    private double pitch;
    public double Pitch
    {
        get => pitch;
        set
        {
            if (pitch != value)
            {
                pitch = value;
                OnPropertyChanged();
            }
        }
    }

    public ICommand ResetCommand { get; }

    public SettingsPageViewModel()
    {
        ResetCommand = new Command(ResetSettings);
        LoadSettings();
    }

    private void LoadSettings()
    {
        Volume = Preferences.Get(nameof(Volume), DefaultVolume);
        Pitch = Preferences.Get(nameof(Pitch), DefaultPitch);
    }

    public void SaveSettings()
    {
        Preferences.Set(nameof(Volume), Volume);
        Preferences.Set(nameof(Pitch), Pitch);
    }

    private void ResetSettings()
    {
        Volume = DefaultVolume;
        Pitch = DefaultPitch;
        SaveSettings();
    }

    public event PropertyChangedEventHandler PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
