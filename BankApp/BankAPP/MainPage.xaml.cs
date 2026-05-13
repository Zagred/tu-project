using BankAPP.Services;
using BankAPP.Shared.Constants;
using BankAPP.Shared.DTOs;
using BankAPP.Shared.Models;
using Microsoft.Extensions.DependencyInjection;

namespace BankAPP
{
    public partial class MainPage : ContentPage
    {
        private readonly MovementApiService _movementApiService;
        private readonly AccountApiService _accountApiService;
        private readonly IServiceProvider _serviceProvider;
        private bool _isInitialized;

        public MainPage(
            MovementApiService movementApiService,
            AccountApiService accountApiService,
            IServiceProvider serviceProvider)
        {
            InitializeComponent();

            _movementApiService = movementApiService;
            _accountApiService = accountApiService;
            _serviceProvider = serviceProvider;

            FilterPicker.SelectedIndex = 0;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadDataAsync();
            _isInitialized = true;
        }

        private async Task LoadDataAsync()
        {
            var accounts = await _accountApiService.GetMyAccountsAsync();
            AccountsCollection.ItemsSource = accounts;
            WelcomeLabel.Text = $"Hello, {SessionManager.CurrentUsername}!";

            var movements = await LoadMovementsAsync();
            LoadSummary(accounts, movements);
            ChartCollectionView.ItemsSource = BuildChartData(movements);
        }

        private async Task<List<Movement>> LoadMovementsAsync()
        {
            string selectedFilter = FilterPicker?.SelectedItem?.ToString() ?? MovementTypes.All;
            var movements = await _movementApiService.GetMovementsByUserAndTypeAsync(selectedFilter);

            TransactionsCollection.ItemsSource = movements;
            return movements;
        }

        private void LoadSummary(List<AccountDto> accounts, List<Movement> movements)
        {
            var totalBalance = accounts.Sum(a => a.Balance);
            var totalDebit = movements.Where(m => MovementTypes.IsExpense(m.MovementType)).Sum(m => m.Amount);
            var totalCredit = movements.Where(m => MovementTypes.IsIncome(m.MovementType)).Sum(m => m.Amount);
            var transferCount = movements.Count(m => m.MovementType == MovementTypes.Transfer);
            var lastTransfer = movements
                .Where(m => m.MovementType == MovementTypes.Transfer)
                .OrderByDescending(m => m.MovementDateTime)
                .FirstOrDefault();

            BalanceLabel.Text = totalBalance.ToString("F2");
            IncomeLabel.Text = totalCredit.ToString("F2");
            PaymentsTotalLabel.Text = $"Spent: {totalDebit:F2}";
            AccountCountLabel.Text = $"{accounts.Count} account{(accounts.Count == 1 ? string.Empty : "s")}";
            TransferCountLabel.Text = $"{transferCount} transfer{(transferCount == 1 ? string.Empty : "s")}";
            LastTransferLabel.Text = lastTransfer != null
                ? $"Last transfer: {lastTransfer.MovementDateTime:dd.MM}"
                : "Last transfer: -";
        }

        private static List<ChartBar> BuildChartData(List<Movement> movements)
        {
            var today = DateTime.Today;
            var lastSevenDays = Enumerable.Range(0, 7)
                .Select(i => today.AddDays(i - 6))
                .Select(day => new
                {
                    Day = day,
                    Amount = movements
                        .Where(m => MovementTypes.IsExpense(m.MovementType))
                        .Where(m => m.MovementDateTime.Date == day.Date)
                        .Sum(m => m.Amount)
                })
                .ToList();

            var maxAmount = lastSevenDays.Max(x => x.Amount);
            if (maxAmount <= 0) maxAmount = 1;

            return lastSevenDays
                .Select(x => new ChartBar(
                    x.Day.ToString("dd"),
                    (double)x.Amount,
                    x.Amount > 0 ? x.Amount.ToString("F0") : "0",
                    Math.Max(10, (double)x.Amount / (double)maxAmount * 130)))
                .ToList();
        }

        private sealed record ChartBar(string DayLabel, double Amount, string AmountText, double Height);

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

        private async void OnFilterChanged(object sender, EventArgs e)
        {
            if (!_isInitialized)
                return;

            await LoadMovementsAsync();
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
