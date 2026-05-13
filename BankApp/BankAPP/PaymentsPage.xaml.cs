using BankAPP.Services;
using BankAPP.Shared.Constants;
using BankAPP.Shared.Models;

namespace BankAPP
{
    public partial class PaymentsPage : ContentPage
    {
        private readonly MovementApiService _movementApiService;
        private List<Movement> _allMovements = new();

        public PaymentsPage(MovementApiService movementApiService)
        {
            InitializeComponent();
            _movementApiService = movementApiService;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            _allMovements = await _movementApiService.GetMyMovementsAsync();
            LoadPayments(_allMovements);
            ChartCollectionView.ItemsSource = BuildChartData(_allMovements);
        }

        private void LoadPayments(List<Movement> movements)
        {
            var filter = FilterPicker.SelectedItem?.ToString() ?? MovementTypes.All;

            if (filter != MovementTypes.All)
            {
                movements = movements.Where(m => m.MovementType == filter).ToList();
            }

            PaymentsCollection.ItemsSource = movements;

            var totalSpent = movements.Where(m => m.IsExpense).Sum(m => m.Amount);
            var totalIncome = movements.Where(m => !m.IsExpense).Sum(m => m.Amount);

            TotalSpentLabel.Text = totalSpent.ToString("F2");
            TotalIncomeLabel.Text = totalIncome.ToString("F2");
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
                    Math.Max(10, (double)x.Amount / (double)maxAmount * 130)))
                .ToList();
        }

        private sealed record ChartBar(string DayLabel, double Amount, string AmountText, double Height);

        private void OnFilterChanged(object sender, EventArgs e)
        {
            LoadPayments(_allMovements);
        }
    }
}
