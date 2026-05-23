using BankAPP.Services;
using BankAPP.Shared.DTOs;
using BankAPP.Shared.Models;

namespace BankAPP
{
    public partial class TransfersPage : ContentPage
    {
        private readonly TransferApiService _transferApiService;
        private readonly AccountApiService _accountApiService;
        private readonly MovementApiService _movementApiService;

        public TransfersPage(
            TransferApiService transferApiService,
            AccountApiService accountApiService,
            MovementApiService movementApiService)
        {
            InitializeComponent();
            NavigationPage.SetHasNavigationBar(this, false);
            _transferApiService = transferApiService;
            _accountApiService = accountApiService;
            _movementApiService = movementApiService;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            var accounts = await _accountApiService.GetMyAccountsAsync();
            FromAccountPicker.ItemsSource = accounts;
            if (accounts.Count > 0)
                FromAccountPicker.SelectedIndex = 0;

            var transfers = await _movementApiService.GetMovementsByUserAndTypeAsync("transfer");
            TransfersCollection.ItemsSource = transfers;

            var username = SessionManager.CurrentUsername ?? "?";
            AvatarLabel.Text = username.Length > 0 ? username[0].ToString().ToUpper() : "?";
            SidebarUsernameLabel.Text = username;
        }

        private async void OnTransferClicked(object sender, EventArgs e)
        {
            if (FromAccountPicker.SelectedItem is not AccountDto selectedAccount)
            {
                await DisplayAlert("Error", "Please select source account.", "OK");
                return;
            }

            if (string.IsNullOrWhiteSpace(ToIbanEntry.Text))
            {
                await DisplayAlert("Error", "Please enter recipient IBAN.", "OK");
                return;
            }

            var toIban = ToIbanEntry.Text.Trim();
            if (string.Equals(toIban, selectedAccount.Iban, StringComparison.OrdinalIgnoreCase))
            {
                await DisplayAlert("Error", "Recipient IBAN cannot be the same as the source account.", "OK");
                return;
            }

            if (!decimal.TryParse(AmountEntry.Text, out decimal amount) || amount <= 0)
            {
                await DisplayAlert("Error", "Invalid amount.", "OK");
                return;
            }

            if (selectedAccount.Balance < amount)
            {
                await DisplayAlert("Error", $"Insufficient funds. Available balance: {selectedAccount.Balance:F2}", "OK");
                return;
            }

            var toAccount = await _accountApiService.GetAccountByIbanAsync(toIban);
            if (toAccount == null)
            {
                await DisplayAlert("Error", "IBAN not found.", "OK");
                return;
            }

            var request = new TransferRequest
            {
                FromAccountId = selectedAccount.Id,
                ToAccountId = toAccount.Id,
                Amount = amount,
                Description = DescriptionEntry.Text?.Trim() ?? string.Empty
            };

            var success = await _transferApiService.TransferAsync(request);
            if (!success)
            {
                await DisplayAlert("Error", "Transfer failed.", "OK");
                return;
            }

            await DisplayAlert("Success", "Transfer completed.", "OK");

            ToIbanEntry.Text = string.Empty;
            AmountEntry.Text = string.Empty;
            DescriptionEntry.Text = string.Empty;

            await LoadDataAsync();
        }

        private void OnNavigateToAccounts(object sender, EventArgs e)
        {
            ((AppShell)Shell.Current).NavigateTo("Accounts");
        }

        private void OnNavigateToPayments(object sender, EventArgs e)
        {
            ((AppShell)Shell.Current).NavigateTo("Payments");
        }

        private async void OnLogoutClicked(object sender, EventArgs e)
        {
            bool confirm = await DisplayAlert("Logout", "Do you want to log out?", "Yes", "No");
            if (!confirm) return;

            SessionManager.Logout();

            var loginPage = IPlatformApplication.Current!.Services.GetService<LoginPage>()!;
            Application.Current!.MainPage = new NavigationPage(loginPage);
        }
    }
}