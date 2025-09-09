using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace RealEstateApp.Models;

public class PropertyListItem : INotifyPropertyChanged
{
    private double _distance;
    public double distance
    {
        get => _distance;
        set
        {
            if (_distance != value)
            {
                _distance = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(DistanceFormatted)); // notifier også helper property
            }
        }
    }

    public string DistanceFormatted => $"{distance:F2} km";

    public PropertyListItem(Property property)
    {
        Property = property;
    }

    private Property _property;
    public Property Property
    {
        get => _property;
        set
        {
            _property = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;
    public void OnPropertyChanged([CallerMemberName] string propertyName = "")
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
