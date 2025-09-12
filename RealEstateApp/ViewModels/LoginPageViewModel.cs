using RealEstateApp.Models;
using RealEstateApp.Services;
using System.Windows.Input;

namespace RealEstateApp.ViewModels
{
    public class LoginPageViewModel : BaseViewModel
    {
        private readonly IPropertyService service;

        public LoginPageViewModel(IPropertyService service)
        {
            this.service = service;

            LoginCommand = new Command(async () => await LoginAsync());
            LogoutCommand = new Command(Logout);
            _ = TryAutoLoginAsync();
        }

        private string username;
        public string Username
        {
            get => username;
            set => SetProperty(ref username, value);
        }

        private string password;
        public string Password
        {
            get => password;
            set => SetProperty(ref password, value);
        }

        private bool isLoggedIn;
        public bool IsLoggedIn
        {
            get => isLoggedIn;
            set => SetProperty(ref isLoggedIn, value);
        }

        public ICommand LoginCommand { get; }
        public ICommand LogoutCommand { get; }

        private async Task LoginAsync()
        {
            var result = await service.LoginAsync(Username, Password);

            if (result.Succeded)
            {
                IsLoggedIn = true;
                await Shell.Current.DisplayAlert("Success", "You are now logged in!", "OK");

                // TODO: navigate to PropertyListPage or main app shell
            }
            else
            {
                await Shell.Current.DisplayAlert("Error", "Invalid credentials.", "OK");
            }
        }

        private async Task TryAutoLoginAsync()
        {
            var result = await service.TryAutoLoginAsync();

            if (result.Succeded)
            {
                IsLoggedIn = true;
                // auto login success — optionally auto navigate
            }
        }

        private void Logout()
        {
            service.Logout();
            IsLoggedIn = false;
            Username = string.Empty;
            Password = string.Empty;
        }
    }
}
