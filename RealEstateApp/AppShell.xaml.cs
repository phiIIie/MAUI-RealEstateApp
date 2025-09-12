using RealEstateApp.Views;

namespace RealEstateApp;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();

		Routing.RegisterRoute(nameof(PropertyDetailPage), typeof(PropertyDetailPage));
        Routing.RegisterRoute(nameof(AddEditPropertyPage), typeof(AddEditPropertyPage));
		Routing.RegisterRoute("AddEditPropertyPage/{PropertyId}", typeof(AddEditPropertyPage));
		Routing.RegisterRoute("CompassPage", typeof(CompassPage));
		Routing.RegisterRoute("HeightCalculatorPage", typeof(HeightCalculatorPage));
		Routing.RegisterRoute("SettingsPage", typeof(SettingsPage));
		Routing.RegisterRoute(nameof(LoginPage), typeof(LoginPage));
    }
}
