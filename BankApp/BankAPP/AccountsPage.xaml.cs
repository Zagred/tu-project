using BankAPP.Services;
using BankAPP.Shared.Constants;
using BankAPP.Shared.DTOs;
using BankAPP.Shared.Models;
using Microsoft.Extensions.DependencyInjection;

namespace BankAPP
{
    public partial class AccountsPage : ContentPage
    {
        private readonly MovementApiService _movementApiService;
        private readonly IServiceProvider _serviceProvider;
        private readonly AccountApiService _accountApiService;

        public AccountsPage(
            MovementApiService movementApiService,
            AccountApiService accountApiService,
            IServiceProvider serviceProvider)
        {
            InitializeComponent();

            _movementApiService = movementApiService;
            _accountApiService = accountApiService;
            _serviceProvider = serviceProvider;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            var accounts = await _accountApiService.GetMyAccountsAsync();
            var movements = await _movementApiService.GetMyMovementsAsync();

            AccountsCollection.ItemsSource = accounts;
            WelcomeLabel.Text = $"Hello, {SessionManager.CurrentUsername}!";
            LoadSummary(accounts, movements);
        }

        private void LoadSummary(List<AccountDto> accounts, List<Movement> movements)
        {
            var totalBalance = accounts.Sum(a => a.Balance);
            var totalDebit = movements.Where(m => MovementTypes.IsExpense(m.MovementType)).Sum(m => m.Amount);
            var totalCredit = movements.Where(m => MovementTypes.IsIncome(m.MovementType)).Sum(m => m.Amount);

            BalanceLabel.Text = totalBalance.ToString("F2");
            IncomeLabel.Text = totalCredit.ToString("F2");
            ExpenseLabel.Text = totalDebit.ToString("F2");
        }

        private async void OnRefreshClicked(object sender, EventArgs e)
        {
            await LoadDataAsync();
        }

        private async void OnLogoutClicked(object sender, EventArgs e)
        {
            bool confirm = await DisplayAlert("Logout", "Do you want to log out?", "Yes", "No");

            if (!confirm)
                return;

            SessionManager.Logout();

            var loginPage = _serviceProvider.GetRequiredService<LoginPage>();
            Application.Current!.MainPage = new NavigationPage(loginPage);
        }

        private async void OnAddMovementClicked(object sender, EventArgs e)
        {
            var page = _serviceProvider.GetRequiredService<AddMovementPage>();
            await Navigation.PushAsync(page);
        }

        private async void OnTransferClicked(object sender, EventArgs e)
        {
            var page = _serviceProvider.GetRequiredService<TransferPage>();
            await Navigation.PushAsync(page);
        }

        private async void OnCreateAccountClicked(object sender, EventArgs e)
        {
            bool confirm = await DisplayAlert("Create Account", "Create a new account?", "Yes", "No");
            if (!confirm)
                return;

            var newAccount = await _accountApiService.CreateAccountAsync();
            if (newAccount == null)
            {
                await DisplayAlert("Error", "Failed to create account", "OK");
                return;
            }

            await DisplayAlert("Success", $"Account created: {newAccount.Iban}", "OK");
            await LoadDataAsync();
        }
    }
}
