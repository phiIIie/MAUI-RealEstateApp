using RealEstateApp.Models;
using System.Windows.Input;

namespace RealEstateApp.ViewModels
{
    [QueryProperty(nameof(Property), "Property")]
    public class CompassViewModel : BaseViewModel
    {
        public CompassViewModel()
        {
            if (Compass.Default.IsSupported)
            {
                if (!Compass.Default.IsMonitoring)
                {
                    // Turn on compass
                    Compass.Default.ReadingChanged += Compass_ReadingChanged;
                    Compass.Default.Start(SensorSpeed.UI);
                }
            }
        }
        ~CompassViewModel()
        {
            // Turn off compass
            Compass.Default.Stop();
            Compass.Default.ReadingChanged -= Compass_ReadingChanged;
        }

        public Property Property { get; set; }

        private double rotationAngle;
        public double RotationAngle
        {
            get => rotationAngle; set
            {
                SetProperty(ref rotationAngle, value);
            }
        }

        private string currentAspect;
        public string CurrentAspect
        {
            get => currentAspect; set
            {
                SetProperty(ref currentAspect, value);
            }
        }

        private double currentHeading;
        public double CurrentHeading
        {
            get => currentHeading; set
            {
                SetProperty(ref currentHeading, value);
            }
        }

        private Command goBackAndSetAspectCommand;
        public ICommand GoBackAndSetAspectCommand => goBackAndSetAspectCommand ??= new Command(async () => await GoBackAndSetAspectAsync());

        private async Task GoBackAndSetAspectAsync()
        {
            Property.Aspect = CurrentAspect;
            await Shell.Current.GoToAsync("..");
        }

        private void Compass_ReadingChanged(object sender, CompassChangedEventArgs e)
        {
            CurrentHeading = e.Reading.HeadingMagneticNorth;
            RotationAngle = -e.Reading.HeadingMagneticNorth;

            if (e.Reading.HeadingMagneticNorth >= 315 || e.Reading.HeadingMagneticNorth < 45)
                CurrentAspect = "North";
            else if (e.Reading.HeadingMagneticNorth >= 45 && e.Reading.HeadingMagneticNorth < 135)
                CurrentAspect = "East";
            else if (e.Reading.HeadingMagneticNorth >= 135 && e.Reading.HeadingMagneticNorth < 225)
                CurrentAspect = "South";
            else if (e.Reading.HeadingMagneticNorth >= 225 && e.Reading.HeadingMagneticNorth < 315)
                CurrentAspect = "West";
        }
    }
}