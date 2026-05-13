using BankAPP.Services;
using BankAPP.Shared.Constants;
using BankAPP.Shared.DTOs;

namespace BankAPP
{
    public partial class AddMovementPage : ContentPage
    {
        private readonly MovementApiService _movementApiService;
        private readonly AccountApiService _accountApiService;

        public AddMovementPage(MovementApiService movementApiService, AccountApiService accountApiService)
        {
            InitializeComponent();
            _movementApiService = movementApiService;
            _accountApiService = accountApiService;

            TypePicker.ItemsSource = new List<MovementTypeOption>
            {
                new("Deposit", MovementTypes.Deposit),
                new("Card payment", MovementTypes.CardPayment),
                new("Cash withdrawal", MovementTypes.CashWithdrawal),
                new("Fee", MovementTypes.Fee)
            };

            TypePicker.SelectedIndex = 0;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            var accounts = await _accountApiService.GetMyAccountsAsync();
            AccountPicker.ItemsSource = accounts;
            if (accounts.Count > 0)
                AccountPicker.SelectedIndex = 0;
        }

        private async void OnAddClicked(object sender, EventArgs e)
        {
            if (AccountPicker.SelectedItem is not AccountDto selectedAccount)
            {
                await DisplayAlert("Error", "Please select an account", "OK");
                return;
            }

            if (!decimal.TryParse(AmountEntry.Text, out decimal amount) || amount <= 0)
            {
                await DisplayAlert("Error", "Invalid amount", "OK");
                return;
            }

            var selectedType = TypePicker.SelectedItem as MovementTypeOption;
            var movementType = selectedType?.Value ?? MovementTypes.Deposit;

            if (MovementTypes.IsExpense(movementType) && selectedAccount.Balance < amount)
            {
                await DisplayAlert("Error", $"Insufficient funds. Available balance: {selectedAccount.Balance:F2}", "OK");
                return;
            }

            var request = new CreateMovementRequest
            {
                AccountId = selectedAccount.Id,
                Amount = amount,
                MovementType = movementType,
                Description = DescriptionEntry.Text ?? string.Empty
            };

            var success = await _movementApiService.AddMovementAsync(request);

            if (!success)
            {
                await DisplayAlert("Error", "Failed to add movement", "OK");
                return;
            }

            await DisplayAlert("Success", "Movement added", "OK");

            AmountEntry.Text = string.Empty;
            DescriptionEntry.Text = string.Empty;
            TypePicker.SelectedIndex = 0;

            await Navigation.PopAsync();
        }

        private sealed record MovementTypeOption(string DisplayName, string Value);
    }
}
