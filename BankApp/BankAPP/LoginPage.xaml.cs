using BankAPP.Services;
using Microsoft.Extensions.DependencyInjection;

namespace BankAPP
{
    public partial class LoginPage : ContentPage
    {
        private readonly UserApiService _userApiService;
        private readonly IServiceProvider _serviceProvider;

        public LoginPage(UserApiService userApiService, IServiceProvider serviceProvider)
        {
            InitializeComponent();
            _userApiService = userApiService;
            _serviceProvider = serviceProvider;
        }

        private async void OnLoginClicked(object sender, EventArgs e)
        {
            var username = UsernameEntry.Text;
            var password = PasswordEntry.Text;

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                await DisplayAlert("Error", "Please enter username and password.", "OK");
                return;
            }

            var loginResponse = await _userApiService.LoginAsync(username, password);

            if (loginResponse == null)
            {
                await DisplayAlert("Error", "Invalid credentials", "OK");
                return;
            }

            SessionManager.CurrentUserId = loginResponse.User.Id;
            SessionManager.CurrentUsername = loginResponse.User.Username;
            SessionManager.Token = loginResponse.Token;

            var appShell = _serviceProvider.GetRequiredService<AppShell>();
            Application.Current!.MainPage = appShell;
        }

        private async void OnGoToRegisterClicked(object sender, EventArgs e)
        {
            var registerPage = _serviceProvider.GetRequiredService<RegisterPage>();
            await Navigation.PushAsync(registerPage);
        }
    }
}