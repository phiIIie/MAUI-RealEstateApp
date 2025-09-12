using RealEstateApp.ViewModels;

namespace RealEstateApp.Views;

public partial class LoginPage : ContentPage
{
    public LoginPage(LoginPageViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
