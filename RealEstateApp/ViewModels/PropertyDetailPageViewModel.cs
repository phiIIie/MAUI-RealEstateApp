using System.Windows.Input;
using RealEstateApp.Models;
using RealEstateApp.Services;
using Microsoft.Maui.ApplicationModel.Communication;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using RealEstateApp.Views;

namespace RealEstateApp.ViewModels;

[QueryProperty(nameof(PropertyListItem), "MyPropertyListItem")]
public class PropertyDetailPageViewModel : BaseViewModel
{
    private readonly IPropertyService service;

    public PropertyDetailPageViewModel(IPropertyService service)
    {
        this.service = service;
        SpeakCommand = new Command(async () => await SpeakDescription(), () => !IsSpeaking);
        StopCommand = new Command(StopSpeaking, () => IsSpeaking);
    }

    Property property;
    public Property Property { get => property; set { SetProperty(ref property, value); } }

    Agent agent;
    public Agent Agent { get => agent; set { SetProperty(ref agent, value); } }

    PropertyListItem propertyListItem;
    public PropertyListItem PropertyListItem
    {
        set
        {
            SetProperty(ref propertyListItem, value);
            Property = propertyListItem.Property;
            Agent = service.GetAgents().FirstOrDefault(x => x.Id == Property.AgentId);
        }
    }

    #region Text-to-Speech
    private bool isSpeaking;
    public bool IsSpeaking
    {
        get => isSpeaking;
        set
        {
            SetProperty(ref isSpeaking, value);
            (SpeakCommand as Command)?.ChangeCanExecute();
            (StopCommand as Command)?.ChangeCanExecute();
        }
    }

    public ICommand SpeakCommand { get; }
    public ICommand StopCommand { get; }

    private CancellationTokenSource cts;

    private async Task SpeakDescription()
    {
        if (Property?.Description == null) return;

        try
        {
            cts = new CancellationTokenSource();
            IsSpeaking = true;
            await TextToSpeech.Default.SpeakAsync(Property.Description);
        }
        catch (OperationCanceledException) { }
        finally
        {
            IsSpeaking = false;
            cts?.Dispose();
            cts = null;
        }
    }

    private void StopSpeaking()
    {
        if (cts != null && !cts.IsCancellationRequested) cts.Cancel();
    }
    #endregion

    #region Edit Property
    private Command editPropertyCommand;
    public ICommand EditPropertyCommand => editPropertyCommand ??= new Command(async () => await GotoEditProperty());

    private async Task GotoEditProperty()
    {
        await Shell.Current.GoToAsync($"{nameof(AddEditPropertyPage)}?mode=editproperty", true,
            new Dictionary<string, object> { { "MyProperty", Property } });
    }
    #endregion

    #region Vendor Phone Tap
    public ICommand VendorPhoneTappedCommand => new Command(async () => await OnVendorPhoneTapped());

    private async Task OnVendorPhoneTapped()
    {
        if (string.IsNullOrWhiteSpace(Property?.Vendor?.Phone)) return;

        try
        {
            var action = await Shell.Current.DisplayActionSheet("Contact Vendor", "Cancel", null, "Call", "SMS");

            switch (action)
            {
                case "Call":
                    if (PhoneDialer.Default.IsSupported)
                        PhoneDialer.Default.Open(Property.Vendor.Phone);
                    else
                        await Shell.Current.DisplayAlert("Error", "Calling not supported on this device.", "OK");
                    break;

                case "SMS":
                    if (Sms.Default.IsComposeSupported)
                    {
                        var message = new SmsMessage(
                            $"Hej, {Property.Vendor.FirstName}, angående {Property.Address}",
                            Property.Vendor.Phone
                        );
                        await Sms.Default.ComposeAsync(message);
                    }
                    else
                        await Shell.Current.DisplayAlert("Error", "SMS not supported on this device.", "OK");
                    break;
            }
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", $"Could not perform action: {ex.Message}", "OK");
        }
    }
    #endregion

    #region Vendor Email Tap
    public ICommand VendorEmailTappedCommand => new Command(async () => await OnVendorEmailTapped());

    private async Task OnVendorEmailTapped()
    {
        if (string.IsNullOrWhiteSpace(Property?.Vendor?.Email)) return;

        try
        {
            var folder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var attachmentFilePath = Path.Combine(folder, "property.txt");
            File.WriteAllText(attachmentFilePath, $"{Property.Address}");

            if (Email.Default.IsComposeSupported)
            {
                var message = new EmailMessage
                {
                    Subject = $"Property Inquiry: {Property.Address}",
                    Body = $"Hej {Property.Vendor.FirstName},\n\nJeg er interesseret i ejendommen på {Property.Address}.",
                    To = new List<string> { Property.Vendor.Email },
                };

                message.Attachments.Add(new EmailAttachment(attachmentFilePath));
                await Email.Default.ComposeAsync(message);
            }
            else
            {
                await Shell.Current.DisplayAlert("Error", "Email is not supported on this device.", "OK");
            }
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", $"Could not send email: {ex.Message}", "OK");
        }
    }
    #endregion

    #region Maps Commands
    public ICommand OpenMapCommand => new Command(async () => await OnOpenMap());
    public ICommand NavigateMapCommand => new Command(async () => await OnNavigateMap());

    private async Task OnOpenMap()
    {
        if (Property == null || Property.Latitude == 0 || Property.Longitude == 0) return;

        try
        {
            var location = new Location(Property.Latitude, Property.Longitude);
            var options = new MapLaunchOptions { Name = Property.Address };
            await Map.OpenAsync(location, options);
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", $"Cannot open map: {ex.Message}", "OK");
        }
    }

    private async Task OnNavigateMap()
    {
        if (Property == null || Property.Latitude == 0 || Property.Longitude == 0) return;

        try
        {
            Location location = new Location(Property.Latitude, Property.Longitude);
            var options = new MapLaunchOptions
            {
                Name = Property.Address,
                NavigationMode = NavigationMode.Driving
            };
            await Map.OpenAsync(location, options);
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", $"Cannot open navigation: {ex.Message}", "OK");
        }
    }
    #endregion
}
