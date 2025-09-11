using RealEstateApp.ViewModels;

namespace RealEstateApp.Views;

public partial class HeightCalculatorPage : ContentPage
{
    public HeightCalculatorPage(HeightCalculatorPageViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
