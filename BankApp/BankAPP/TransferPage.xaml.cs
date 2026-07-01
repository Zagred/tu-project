using BankAPP.Services;
using BankShared.DTOs;
using BankShared.Utilities;

namespace BankAPP
{
    public partial class TransferPage : ContentPage
    {
        private readonly TransferApiService _transferApiService;
        private readonly AccountApiService _accountApiService;

        public TransferPage(
            TransferApiService transferApiService,
            AccountApiService accountApiService)
        {
            InitializeComponent();
            _transferApiService = transferApiService;
            _accountApiService = accountApiService;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            var accounts = await _accountApiService.GetMyAccountsAsync();

            FromAccountPicker.ItemsSource = accounts;

            if (accounts.Count > 0)
                FromAccountPicker.SelectedIndex = 0;
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

            var toIban = BankInputNormalizer.NormalizeIban(ToIbanEntry.Text);
            if (string.Equals(toIban, BankInputNormalizer.NormalizeIban(selectedAccount.Iban), StringComparison.OrdinalIgnoreCase))
            {
                await DisplayAlert("Error", "Recipient IBAN cannot be the same as the source account.", "OK");
                return;
            }

            if (!BankInputNormalizer.TryParseAmount(AmountEntry.Text, out decimal amount) || amount <= 0)
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

            var (success, errorMessage) = await _transferApiService.TransferAsync(request);
            if (!success)
            {
                await DisplayAlert("Error", BankInputNormalizer.ToDisplayError(errorMessage), "OK");
                return;
            }

            await DisplayAlert("Success", "Transfer submitted for admin approval.", "OK");
            await Navigation.PopAsync();
        }
    }
}
