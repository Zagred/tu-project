using BankAPP.Services;
using BankShared.Constants;
using BankShared.Models;

namespace BankAPP
{
    public partial class PaymentsPage : ContentPage
    {
        private readonly MovementApiService _movementApiService;
        private readonly AssistantApiService _assistantApiService;
        private readonly BudgetReportApiService _budgetReportApiService;
        private readonly IServiceProvider _serviceProvider;
        private List<Movement> _allMovements = new();

        public PaymentsPage(
            MovementApiService movementApiService,
            AssistantApiService assistantApiService,
            BudgetReportApiService budgetReportApiService,
            IServiceProvider serviceProvider)
        {
            InitializeComponent();
            NavigationPage.SetHasNavigationBar(this, false);
            _movementApiService = movementApiService;
            _assistantApiService = assistantApiService;
            _budgetReportApiService = budgetReportApiService;
            _serviceProvider = serviceProvider;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            UpdateAdminNavigation();
            await LoadDataAsync();
        }

        private void UpdateAdminNavigation()
        {
            var isAdmin = SessionManager.IsAdmin;

            try { SidebarAdminButton.IsVisible = isAdmin; } catch { }
            try
            {
                MobileNavAdminItem.IsVisible = isAdmin;
                MobileNavGrid.ColumnDefinitions.Clear();

                for (var i = 0; i < (isAdmin ? 5 : 4); i++)
                    MobileNavGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });

                Grid.SetColumn(MobileNavLogoutItem, isAdmin ? 4 : 3);
                Grid.SetColumnSpan(MobileNavSeparator, isAdmin ? 5 : 4);
            }
            catch { }
        }

        private async Task LoadDataAsync()
        {
            _allMovements = await _movementApiService.GetMyMovementsAsync();
            var chartData = BuildChartData(_allMovements);

            try { ChartCollectionView.ItemsSource = chartData; } catch { }
            try { ChartCollectionViewM.ItemsSource = chartData; } catch { }

            var username = SessionManager.CurrentUsername ?? "?";
            var initial = username.Length > 0 ? username[0].ToString().ToUpper() : "?";
            try { AvatarLabel.Text = initial; SidebarUsernameLabel.Text = username; } catch { }

            LoadPayments(_allMovements);
        }

        private void LoadPayments(List<Movement> movements)
        {
            var filter = string.Empty;
            try { filter = FilterPicker.SelectedItem?.ToString() ?? MovementTypes.All; } catch { }
            if (string.IsNullOrEmpty(filter))
                try { filter = FilterPickerM.SelectedItem?.ToString() ?? MovementTypes.All; } catch { }
            if (string.IsNullOrEmpty(filter)) filter = MovementTypes.All;

            var filtered = filter != MovementTypes.All
                ? movements.Where(m => m.MovementType == filter).ToList()
                : movements;

            try { PaymentsCollection.ItemsSource = filtered; } catch { }
            try { PaymentsCollectionM.ItemsSource = filtered; } catch { }

            var totalSpent = filtered.Where(m => m.IsExpense).Sum(m => m.Amount);
            var totalIncome = filtered.Where(m => !m.IsExpense).Sum(m => m.Amount);

            try { TotalSpentLabel.Text = totalSpent.ToString("F2"); } catch { }
            try { TotalIncomeLabel.Text = totalIncome.ToString("F2"); } catch { }
            try { TotalSpentLabelM.Text = totalSpent.ToString("F2"); } catch { }
            try { TotalIncomeLabelM.Text = totalIncome.ToString("F2"); } catch { }
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
                        .Where(m => m.IsExpense && m.MovementDateTime.Date == day.Date)
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
                    Math.Max(8, (double)x.Amount / (double)maxAmount * 60)))
                .ToList();
        }

        private sealed record ChartBar(string DayLabel, double Amount, string AmountText, double Height);

        private void OnFilterChanged(object sender, EventArgs e) => LoadPayments(_allMovements);

        private async void OnAiAdviceClicked(object sender, EventArgs e)
        {
            var advice = await _assistantApiService.GetAdviceAsync();
            await DisplayAlert("AI Финансов Асистент", advice, "OK");
        }

        private async void OnEmailReportClicked(object sender, EventArgs e)
        {
            var message = await _budgetReportApiService.SendMonthlyReportAsync();
            await DisplayAlertAsync("Monthly Report", message, "OK");
        }

        private void OnNavigateToAccounts(object sender, EventArgs e) =>
            ((AppShell)Shell.Current).NavigateTo("Accounts");

        private void OnNavigateToTransfers(object sender, EventArgs e) =>
            ((AppShell)Shell.Current).NavigateTo("Transfers");

        private void OnNavigateToAdmin(object sender, EventArgs e) =>
            ((AppShell)Shell.Current).NavigateTo("Admin");

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
