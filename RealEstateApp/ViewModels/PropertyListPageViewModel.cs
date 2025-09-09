using RealEstateApp.Models;
using RealEstateApp.Services;
using RealEstateApp.Views;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Input;
using Microsoft.Maui.Devices.Sensors; // For geolocation

namespace RealEstateApp.ViewModels;

public class PropertyListPageViewModel : BaseViewModel
{
    public ObservableCollection<PropertyListItem> PropertiesCollection { get; } = new();

    private readonly IPropertyService service;

    public PropertyListPageViewModel(IPropertyService service)
    {
        Title = "Property List";
        this.service = service;
    }

    private bool isRefreshing;
    public bool IsRefreshing
    {
        get => isRefreshing;
        set => SetProperty(ref isRefreshing, value);
    }

    private Command getPropertiesCommand;
    public ICommand GetPropertiesCommand => getPropertiesCommand ??= new Command(async () => await GetPropertiesAsync());

    private Command sortCommand;
    public ICommand SortCommand => sortCommand ??= new Command(async () => await SortAsync());

    private Location? lastKnownLocation;

    private Command<PropertyListItem> goToDetailsCommand;
    public ICommand GoToDetailsCommand => goToDetailsCommand ??= new Command<PropertyListItem>(async (item) => await GoToDetails(item));

    private Command goToAddPropertyCommand;
    public ICommand GoToAddPropertyCommand => goToAddPropertyCommand ??= new Command(async () => await GotoAddProperty());

    async Task GetPropertiesAsync()
    {
        if (IsBusy)
            return;

        try
        {
            IsBusy = true;

            // 1. Ensure we have the current location
            if (lastKnownLocation == null)
            {
                var request = new GeolocationRequest(GeolocationAccuracy.Medium);
                lastKnownLocation = await Geolocation.Default.GetLocationAsync(request);
            }

            if (lastKnownLocation == null)
            {
                await Shell.Current.DisplayAlert("Location Error", "Unable to get current location.", "OK");
                return;
            }

            // 2. Fetch properties
            var properties = service.GetProperties();
            var listItems = new List<PropertyListItem>();

            foreach (var property in properties)
            {
                var item = new PropertyListItem(property);

                // Calculate distance (without sorting)
                item.distance = Location.CalculateDistance(
                    lastKnownLocation.Latitude,
                    lastKnownLocation.Longitude,
                    property.Latitude,
                    property.Longitude,
                    DistanceUnits.Kilometers
                );

                listItems.Add(item);
            }

            // 3. Populate collection (unsorted)
            PropertiesCollection.Clear();
            foreach (var item in listItems)
                PropertiesCollection.Add(item);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Unable to get properties: {ex.Message}");
            await Shell.Current.DisplayAlert("Error!", ex.Message, "OK");
        }
        finally
        {
            IsBusy = false;
            IsRefreshing = false;
        }
    }


    async Task SortAsync()
    {
        try
        {
            // Sort the collection by distance only when the button is clicked
            var sorted = PropertiesCollection.OrderBy(p => p.distance).ToList();
            PropertiesCollection.Clear();
            foreach (var item in sorted)
                PropertiesCollection.Add(item);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Sort error: {ex.Message}");
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
        }
    }



    async Task GoToDetails(PropertyListItem propertyListItem)
    {
        if (propertyListItem == null)
            return;

        await Shell.Current.GoToAsync(nameof(PropertyDetailPage), true, new Dictionary<string, object>
        {
            { "MyPropertyListItem", propertyListItem }
        });
    }

    async Task GotoAddProperty()
    {
        await Shell.Current.GoToAsync($"{nameof(AddEditPropertyPage)}?mode=newproperty", true, new Dictionary<string, object>
        {
            { "MyProperty", new Property() }
        });
    }
}
