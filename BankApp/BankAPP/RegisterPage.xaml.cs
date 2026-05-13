using BankAPP.Services;
using BankAPP.Shared.DTOs;

namespace BankAPP
{
    public partial class RegisterPage : ContentPage
    {
        private readonly UserApiService _userApiService;

        public RegisterPage(UserApiService userApiService)
        {
            InitializeComponent();
            _userApiService = userApiService;
        }

        private async void OnRegisterClicked(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(NameEntry.Text) ||
                string.IsNullOrWhiteSpace(UsernameEntry.Text) ||
                string.IsNullOrWhiteSpace(EmailEntry.Text) ||
                string.IsNullOrWhiteSpace(EgnEntry.Text) ||
                string.IsNullOrWhiteSpace(PasswordEntry.Text))
            {
                await DisplayAlert("Error", "Please fill all fields.", "OK");
                return;
            }

            var request = new RegisterRequest
            {
                Name = NameEntry.Text,
                Username = UsernameEntry.Text,
                Email = EmailEntry.Text,
                Egn = EgnEntry.Text,
                Password = PasswordEntry.Text
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