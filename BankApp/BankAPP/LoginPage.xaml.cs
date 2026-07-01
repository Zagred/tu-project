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
            NavigationPage.SetHasNavigationBar(this, false);
            _userApiService = userApiService;
            _serviceProvider = serviceProvider;
        }

        private string GetUsername() =>
            !string.IsNullOrEmpty(UsernameEntry?.Text)
                ? UsernameEntry.Text
                : UsernameEntryMobile?.Text ?? string.Empty;

        private string GetPassword() =>
            !string.IsNullOrEmpty(PasswordEntry?.Text)
                ? PasswordEntry.Text
                : PasswordEntryMobile?.Text ?? string.Empty;

        private async void OnLoginClicked(object sender, EventArgs e)
        {
            var username = GetUsername();
            var password = GetPassword();

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                await DisplayAlert("Error", "Please enter username and password.", "OK");
                return;
            }

            var loginResponse = await _userApiService.LoginAsync(username, password);

            if (loginResponse == null)
            {
                await DisplayAlert("Error", "Invalid credentials or API is not reachable.", "OK");
                return;
            }

            SessionManager.CurrentUserId = loginResponse.User.Id;
            SessionManager.CurrentUsername = loginResponse.User.Username;
            SessionManager.Token = loginResponse.Token;

            var appShell = _serviceProvider.GetRequiredService<AppShell>();
            appShell.RefreshAdminVisibility();
            Application.Current!.MainPage = appShell;

            appShell.NavigateTo(SessionManager.IsAdmin ? "Admin" : "Accounts");
        }

        private async void OnGoToRegisterClicked(object sender, EventArgs e)
        {
            var registerPage = _serviceProvider.GetRequiredService<RegisterPage>();
            await Navigation.PushAsync(registerPage);
        }
    }
}
