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
    readonly IPropertyService service;
    readonly IConnectivity connectivity;

    public AddEditPropertyPageViewModel(IPropertyService service, IConnectivity connectivity)
    {
        this.service = service;
        this.connectivity = connectivity;
        this.connectivity.ConnectivityChanged += Connectivity_ConnectivityChanged;
        Agents = new ObservableCollection<Agent>(service.GetAgents());

        GetCurrentLocationCommand = new Command(async () => await GetCurrentLocation());
        GetCoordinatesFromAddressCommand = new Command(async () => await ResolveAddressToCoordinates(),
                                                      () => connectivity.NetworkAccess == NetworkAccess.Internet);

        Command CancelCommand = new Command(async () => await Shell.Current.GoToAsync(".."));

        connectivity.ConnectivityChanged += Connectivity_ConnectivityChanged;
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
                SelectedAgent = Agents.FirstOrDefault(x => x.Id == _property?.AgentId);

            if (!string.IsNullOrWhiteSpace(_property?.Address))
                _ = ResolveAddressToLocation(_property.Address);
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

    private Command cancelCommand;
    public ICommand CancelCommand => cancelCommand ??= new Command(async () => await Shell.Current.GoToAsync(".."));

    public ICommand GetCurrentLocationCommand { get; }
    public ICommand GetCoordinatesFromAddressCommand { get; }
    #endregion

    private async Task SaveProperty()
    {
        if (!IsValid())
        {
            StatusMessage = "Please fill in all required fields";
            StatusColor = Colors.Red;

            try
            {
                // Vibrate for 5 seconds
                var duration = TimeSpan.FromSeconds(5);
                Vibration.Default.Vibrate(duration);
            }
            catch
            {
                // Not all devices support vibration, ignore exceptions
            }

            return;
        }

        service.SaveProperty(Property);
        await Shell.Current.GoToAsync("///propertylist");
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

    #region LOCATION FUNCTIONS
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
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", $"Failed to get coordinates: {ex.Message}", "OK");
        }
    }

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
            // ignore
        }
    }
    #endregion

    #region CONNECTIVITY
    private void CheckConnectivity()
    {
        IsGeocodingEnabled = connectivity.NetworkAccess == NetworkAccess.Internet;
        (GetCoordinatesFromAddressCommand as Command)?.ChangeCanExecute();
    }

    private void Connectivity_ConnectivityChanged(object sender, ConnectivityChangedEventArgs e)
    {
        CheckConnectivity();
    }
    #endregion

    public void Dispose()
    {
        connectivity.ConnectivityChanged -= Connectivity_ConnectivityChanged;
    }
}