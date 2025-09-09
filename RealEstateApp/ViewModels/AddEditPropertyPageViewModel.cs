using RealEstateApp.Models;
using RealEstateApp.Services;
using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.Networking;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace RealEstateApp.ViewModels;

[QueryProperty(nameof(Mode), "mode")]
[QueryProperty(nameof(Property), "MyProperty")]
public class AddEditPropertyPageViewModel : BaseViewModel, IDisposable
{
    private readonly IPropertyService service;
    private readonly IConnectivity connectivity;

    public AddEditPropertyPageViewModel(IPropertyService service, IConnectivity connectivity)
    {
        this.service = service;
        this.connectivity = connectivity;

        Agents = new ObservableCollection<Agent>(service.GetAgents());

        GetCurrentLocationCommand = new Command(async () => await GetCurrentLocation());
        GetCoordinatesFromAddressCommand = new Command(async () => await ResolveAddressToCoordinates(),
                                                      () => connectivity.NetworkAccess == NetworkAccess.Internet);

        connectivity.ConnectivityChanged += Connectivity_ConnectivityChanged;

        // Initial connectivity check
        CheckConnectivity();
    }

    #region PROPERTIES
    public string Mode { get; set; }

    public ObservableCollection<Agent> Agents { get; }

    private Property _property;
    public Property Property
    {
        get => _property;
        set
        {
            SetProperty(ref _property, value);
            Title = Mode == "newproperty" ? "Add Property" : "Edit Property";

            if (_property.AgentId != null)
            {
                SelectedAgent = Agents.FirstOrDefault(x => x.Id == _property?.AgentId);
            }

            if (!string.IsNullOrWhiteSpace(_property?.Address))
            {
                _ = ResolveAddressToLocation(_property.Address);
            }
        }
    }

    private Agent _selectedAgent;
    public Agent SelectedAgent
    {
        get => _selectedAgent;
        set
        {
            if (Property != null)
            {
                SetProperty(ref _selectedAgent, value);
                Property.AgentId = _selectedAgent?.Id;
            }
        }
    }

    private string statusMessage;
    public string StatusMessage
    {
        get => statusMessage;
        set => SetProperty(ref statusMessage, value);
    }

    private Color statusColor;
    public Color StatusColor
    {
        get => statusColor;
        set => SetProperty(ref statusColor, value);
    }

    private string latitude;
    public string Latitude
    {
        get => latitude;
        set => SetProperty(ref latitude, value);
    }

    private string longitude;
    public string Longitude
    {
        get => longitude;
        set => SetProperty(ref longitude, value);
    }

    private bool isGeocodingEnabled;
    public bool IsGeocodingEnabled
    {
        get => isGeocodingEnabled;
        set => SetProperty(ref isGeocodingEnabled, value);
    }
    #endregion

    #region COMMANDS
    private Command savePropertyCommand;
    public ICommand SavePropertyCommand => savePropertyCommand ??= new Command(async () => await SaveProperty());

    private Command cancelSaveCommand;
    public ICommand CancelSaveCommand => cancelSaveCommand ??= new Command(async () => await Shell.Current.GoToAsync(".."));

    public ICommand GetCurrentLocationCommand { get; }
    public ICommand GetCoordinatesFromAddressCommand { get; }
    #endregion

    private async Task SaveProperty()
    {
        if (!IsValid())
        {
            StatusMessage = "Please fill in all required fields";
            StatusColor = Colors.Red;
        }
        else
        {
            service.SaveProperty(Property);
            await Shell.Current.GoToAsync("///propertylist");
        }
    }

    public bool IsValid()
    {
        if (string.IsNullOrEmpty(Property.Address)
            || Property.Beds == null
            || Property.Price == null
            || Property.AgentId == null)
            return false;
        return true;
    }

    /// <summary>
    /// Reverse geocoding: get current location and fill address
    /// </summary>
    private async Task GetCurrentLocation()
    {
        try
        {
            var request = new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(10));
            var location = await Geolocation.Default.GetLocationAsync(request);

            if (location != null)
            {
                Latitude = location.Latitude.ToString("F6");
                Longitude = location.Longitude.ToString("F6");

                Property.Latitude = location.Latitude;
                Property.Longitude = location.Longitude;

                var placemarks = await Geocoding.Default.GetPlacemarksAsync(location.Latitude, location.Longitude);
                var placemark = placemarks?.FirstOrDefault();
                if (placemark != null)
                {
                    Property.Address = $"{placemark.Thoroughfare} {placemark.SubThoroughfare}, {placemark.Locality}, {placemark.PostalCode}, {placemark.CountryName}";
                }
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Kunne ikke hente lokation: {ex.Message}";
            StatusColor = Colors.Red;
        }
    }

    /// <summary>
    /// Forward geocoding: get coordinates from entered address
    /// </summary>
    private async Task ResolveAddressToCoordinates()
    {
        if (string.IsNullOrWhiteSpace(Property.Address))
        {
            await Shell.Current.DisplayAlert("Address Missing", "Please enter an address first.", "OK");
            return;
        }

        try
        {
            var locations = await Geocoding.Default.GetLocationsAsync(Property.Address);
            var location = locations?.FirstOrDefault();

            if (location != null)
            {
                Latitude = location.Latitude.ToString("F6");
                Longitude = location.Longitude.ToString("F6");

                Property.Latitude = location.Latitude;
                Property.Longitude = location.Longitude;
            }
            else
            {
                await Shell.Current.DisplayAlert("Not Found", "Could not find coordinates for the entered address.", "OK");
            }
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", $"Failed to get coordinates: {ex.Message}", "OK");
        }
    }

    /// <summary>
    /// Geocoding from address to location (used when editing existing property)
    /// </summary>
    private async Task ResolveAddressToLocation(string address)
    {
        try
        {
            var locations = await Geocoding.Default.GetLocationsAsync(address);
            var location = locations?.FirstOrDefault();
            if (location != null)
            {
                Latitude = location.Latitude.ToString("F6");
                Longitude = location.Longitude.ToString("F6");
            }
        }
        catch
        {
            // Silent fail
        }
    }

    /// <summary>
    /// Connectivity check and alerts
    /// </summary>
    private void CheckConnectivity()
    {
        if (connectivity.NetworkAccess != NetworkAccess.Internet)
        {
            IsGeocodingEnabled = false;
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await Shell.Current.DisplayAlert("No Internet", "No internet connection detected.", "OK");
            });
        }
        else
        {
            IsGeocodingEnabled = true;
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await Shell.Current.DisplayAlert("Online", "Internet connection restored.", "OK");
            });
        }

        (GetCoordinatesFromAddressCommand as Command)?.ChangeCanExecute();
    }

    private void Connectivity_ConnectivityChanged(object sender, ConnectivityChangedEventArgs e)
    {
        CheckConnectivity();
    }

    public void Dispose()
    {
        connectivity.ConnectivityChanged -= Connectivity_ConnectivityChanged;
    }
}
