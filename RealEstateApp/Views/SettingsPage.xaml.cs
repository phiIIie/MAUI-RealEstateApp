using RealEstateApp.ViewModels;

namespace RealEstateApp.Views;

public partial class SettingsPage : ContentPage
{
    private SettingsPageViewModel vm;

    public SettingsPage()
    {
        InitializeComponent();
        vm = BindingContext as SettingsPageViewModel;
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        vm?.SaveSettings();
    }
}
