using RealEstateApp.Models;
using RealEstateApp.Services;
using RealEstateApp.Views;
using System.Windows.Input;
using Microsoft.Maui.Devices.Sensors; // For Text-to-Speech
using Microsoft.Maui.ApplicationModel; // For TextToSpeech

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
        if (Property?.Description == null)
            return;

        try
        {
            cts = new CancellationTokenSource();
            IsSpeaking = true;

            await TextToSpeech.Default.SpeakAsync(Property.Description);

        }
        catch (OperationCanceledException)
        {
            // cancelled, ignore
        }
        finally
        {
            IsSpeaking = false;
            cts.Dispose();
            cts = null;
        }
    }

    private void StopSpeaking()
    {
        if (cts != null && !cts.IsCancellationRequested)
        {
            cts.Cancel();
        }
    }
    #endregion
        
    private Command editPropertyCommand;
    public ICommand EditPropertyCommand => editPropertyCommand ??= new Command(async () => await GotoEditProperty());
    async Task GotoEditProperty()
    {
        await Shell.Current.GoToAsync($"{nameof(AddEditPropertyPage)}?mode=editproperty", true, new Dictionary<string, object>
        {
            { "MyProperty", Property }
        });
    }
}
