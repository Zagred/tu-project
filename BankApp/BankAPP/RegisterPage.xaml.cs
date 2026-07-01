using BankAPP.Services;
using BankShared.DTOs;

namespace BankAPP
{
    public partial class RegisterPage : ContentPage
    {
        private readonly UserApiService _userApiService;

        public RegisterPage(UserApiService userApiService)
        {
            InitializeComponent();
            NavigationPage.SetHasNavigationBar(this, false);
            _userApiService = userApiService;
        }

        private string Get(Entry desktop, Entry mobile) =>
            !string.IsNullOrEmpty(desktop?.Text) ? desktop.Text : mobile?.Text ?? string.Empty;

        private async void OnRegisterClicked(object sender, EventArgs e)
        {
            var name = Get(NameEntry, NameEntryMobile);
            var username = Get(UsernameEntry, UsernameEntryMobile);
            var email = Get(EmailEntry, EmailEntryMobile);
            var egn = Get(EgnEntry, EgnEntryMobile);
            var password = Get(PasswordEntry, PasswordEntryMobile);

            if (string.IsNullOrWhiteSpace(name) ||
                string.IsNullOrWhiteSpace(username) ||
                string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(egn) ||
                string.IsNullOrWhiteSpace(password))
            {
                await DisplayAlert("Error", "Please fill all fields.", "OK");
                return;
            }

            var request = new RegisterRequest
            {
                Name = name,
                Username = username,
                Email = email,
                Egn = egn,
                Password = password
            };

            var success = await _userApiService.RegisterAsync(request);

            if (!success)
            {
                await DisplayAlert("Error", "Could not create account. Username, email or EGN may already exist.", "OK");
                return;
            }

            await DisplayAlert("Success", "Account created successfully.", "OK");
            await Navigation.PopAsync();
        }

        private async void OnGoToLoginClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }
    }
}