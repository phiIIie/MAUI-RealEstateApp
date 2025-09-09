using RealEstateApp.Models;
using RealEstateApp.Services;
using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.Devices;
using Microsoft.Maui.Networking;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace RealEstateApp.ViewModels;

[QueryProperty(nameof(Mode), "mode")]
[QueryProperty(nameof(Property), "MyProperty")]
public class AddEditPropertyPageViewModel : BaseViewModel
{
    readonly IPropertyService service;
    readonly IConnectivity connectivity;

    public AddEditPropertyPageViewModel(IPropertyService service, IConnectivity connectivity)
    {
        this.service = service;
        this.connectivity = connectivity;

        Agents = new ObservableCollection<Agent>(service.GetAgents());

        GetCurrentLocationCommand = new Command(async () => await GetCurrentLocation());
        GeocodeAddressCommand = new Command(async () => await GeocodeAddress(), () => IsConnected);
        ToggleFlashlightCommand = new Command(async () => await ToggleFlashlight());

        // Subscribe to connectivity changes
        connectivity.ConnectivityChanged += Connectivity_ConnectivityChanged;

        // Subscribe to battery changes
        Battery.Default.BatteryInfoChanged += Battery_BatteryInfoChanged;

        CheckConnectivity();
        UpdateBatteryStatus();
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

    private string latitude;
    public string Latitude { get => latitude; set => SetProperty(ref latitude, value); }

    private string longitude;
    public string Longitude { get => longitude; set => SetProperty(ref longitude, value); }

    private string statusMessage;
    public string StatusMessage { get => statusMessage; set => SetProperty(ref statusMessage, value); }

    private Color statusColor;
    public Color StatusColor { get => statusColor; set => SetProperty(ref statusColor, value); }

    private bool isConnected;
    public bool IsConnected { get => isConnected; set => SetProperty(ref isConnected, value); }

    private string batteryMessage;
    public string BatteryMessage { get => batteryMessage; set => SetProperty(ref batteryMessage, value); }

    private Color batteryColor;
    public Color BatteryColor { get => batteryColor; set => SetProperty(ref batteryColor, value); }

    private bool flashlightOn;
    public bool FlashlightOn { get => flashlightOn; set => SetProperty(ref flashlightOn, value); }
    #endregion

    #region COMMANDS
    public ICommand GetCurrentLocationCommand { get; }
    public ICommand GeocodeAddressCommand { get; }
    public ICommand ToggleFlashlightCommand { get; }

    private Command savePropertyCommand;
    public ICommand SavePropertyCommand => savePropertyCommand ??= new Command(async () => await SaveProperty());

    private Command cancelSaveCommand;
    public ICommand CancelSaveCommand => cancelSaveCommand ??= new Command(async () => await Shell.Current.GoToAsync(".."));
    #endregion

    #region CONNECTIVITY
    private void Connectivity_ConnectivityChanged(object sender, ConnectivityChangedEventArgs e)
    {
        CheckConnectivity();
    }

    private void CheckConnectivity()
    {
        IsConnected = connectivity.NetworkAccess == NetworkAccess.Internet;
        StatusMessage = IsConnected ? "Connected" : "No internet connection";
        StatusColor = IsConnected ? Colors.Green : Colors.Red;

        // Refresh Geocode button availability
        ((Command)GeocodeAddressCommand).ChangeCanExecute();
    }
    #endregion

    #region BATTERY
    private void Battery_BatteryInfoChanged(object sender, BatteryInfoChangedEventArgs e)
    {
        UpdateBatteryStatus();
    }

    private void UpdateBatteryStatus()
    {
        var level = Battery.Default.ChargeLevel;
        var state = Battery.Default.State;
        var saver = Battery.Default.EnergySaverStatus;

        if (level < 0.2)
        {
            BatteryMessage = "Battery low!";
            if (saver == EnergySaverStatus.On)
                BatteryColor = Colors.Green;
            else if (state == BatteryState.Charging)
                BatteryColor = Colors.Yellow;
            else
                BatteryColor = Colors.Red;
        }
        else
        {
            BatteryMessage = string.Empty;
        }
    }
    #endregion

    #region LOCATION
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

                // Reverse geocode
                var placemarks = await Geocoding.GetPlacemarksAsync(location.Latitude, location.Longitude);
                var placemark = placemarks?.FirstOrDefault();
                if (placemark != null)
                    Property.Address = $"{placemark.Thoroughfare} {placemark.SubThoroughfare}, {placemark.Locality}";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Could not get location: {ex.Message}";
            StatusColor = Colors.Red;
        }
    }

    private async Task ResolveAddressToLocation(string address)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(address))
            {
                await App.Current.MainPage.DisplayAlert("Input Required", "Please enter an address first.", "OK");
                return;
            }

            var locations = await Geocoding.GetLocationsAsync(address);
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
            StatusMessage = $"Could not geocode address: {ex.Message}";
            StatusColor = Colors.Red;
        }
    }

    private async Task GeocodeAddress()
    {
        await ResolveAddressToLocation(Property.Address);
    }
    #endregion

    #region FLASHLIGHT
    private async Task ToggleFlashlight()
    {
        try
        {
            if (!FlashlightOn)
                await Flashlight.Default.TurnOnAsync();
            else
                await Flashlight.Default.TurnOffAsync();

            FlashlightOn = !FlashlightOn;
        }
        catch (Exception ex)
        {
            await App.Current.MainPage.DisplayAlert("Flashlight", $"Unable to toggle flashlight: {ex.Message}", "OK");
        }
    }
    #endregion

    #region SAVE
    private async Task SaveProperty()
    {
        if (!IsValid())
        {
            StatusMessage = "Please fill in all required fields";
            StatusColor = Colors.Red;
            Vibration.Default.Vibrate(TimeSpan.FromSeconds(5));
            return;
        }

        service.SaveProperty(Property);
        await Shell.Current.GoToAsync("///propertylist");
    }

    public bool IsValid()
    {
        return !string.IsNullOrEmpty(Property.Address)
            && Property.Beds != null
            && Property.Price != null
            && Property.AgentId != null;
    }
    #endregion
}
