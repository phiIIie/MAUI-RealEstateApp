using RealEstateApp.Models;
using RealEstateApp.Services;
using System.Collections.ObjectModel;
using System.Windows.Input;
using Microsoft.Maui.Devices.Sensors;

namespace RealEstateApp.ViewModels;

[QueryProperty(nameof(Mode), "mode")]
[QueryProperty(nameof(Property), "MyProperty")]
public class AddEditPropertyPageViewModel : BaseViewModel
{
    readonly IPropertyService service;

    public AddEditPropertyPageViewModel(IPropertyService service)
    {
        this.service = service;
        Agents = new ObservableCollection<Agent>(service.GetAgents());
        GetCurrentLocationCommand = new Command(async () => await GetCurrentLocation());
    }

    public string Mode { get; set; }

    #region PROPERTIES
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

            // When editing, resolve location from address if available
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

    string statusMessage;
    public string StatusMessage
    {
        get => statusMessage;
        set => SetProperty(ref statusMessage, value);
    }

    Color statusColor;
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
    #endregion

    #region COMMANDS
    private Command savePropertyCommand;
    public ICommand SavePropertyCommand => savePropertyCommand ??= new Command(async () => await SaveProperty());

    private Command cancelSaveCommand;
    public ICommand CancelSaveCommand => cancelSaveCommand ??= new Command(async () => await Shell.Current.GoToAsync(".."));

    public ICommand GetCurrentLocationCommand { get; }
    #endregion

    private async Task SaveProperty()
    {
        if (IsValid() == false)
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
    /// Gets the device's current location (used when pressing the Thumbtack button).
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
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Kunne ikke hente lokation: {ex.Message}";
            StatusColor = Colors.Red;
        }
    }

    /// <summary>
    /// Resolves the property address into latitude/longitude using Geocoding.
    /// </summary>
    private async Task ResolveAddressToLocation(string address)
    {
        try
        {
            var locations = await Geocoding.GetLocationsAsync(address);

            var location = locations?.FirstOrDefault();
            if (location != null)
            {
                Latitude = location.Latitude.ToString("F6");
                Longitude = location.Longitude.ToString("F6");
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Kunne ikke finde lokation for adressen: {ex.Message}";
            StatusColor = Colors.Red;   
        }
    }
}
