using BankAPP.Services;
using BankShared.DTOs;
using BankShared.Models;
using BankShared.Utilities;

namespace BankAPP
{
    public partial class TransfersPage : ContentPage
    {
        private readonly TransferApiService _transferApiService;
        private readonly AccountApiService _accountApiService;
        private readonly MovementApiService _movementApiService;
        private readonly IServiceProvider _serviceProvider;

        public TransfersPage(
            TransferApiService transferApiService,
            AccountApiService accountApiService,
            MovementApiService movementApiService,
            IServiceProvider serviceProvider)
        {
            InitializeComponent();
            NavigationPage.SetHasNavigationBar(this, false);
            _transferApiService = transferApiService;
            _accountApiService = accountApiService;
            _movementApiService = movementApiService;
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
            var accounts = await _accountApiService.GetMyAccountsAsync();
            var transfers = await _movementApiService.GetMovementsByUserAndTypeAsync("transfer");
            var username = SessionManager.CurrentUsername ?? "?";
            var initial = username.Length > 0 ? username[0].ToString().ToUpper() : "?";

            try { FromAccountPicker.ItemsSource = accounts; if (accounts.Count > 0) FromAccountPicker.SelectedIndex = 0; } catch { }
            try { FromAccountPickerM.ItemsSource = accounts; if (accounts.Count > 0) FromAccountPickerM.SelectedIndex = 0; } catch { }
            try { TransfersCollection.ItemsSource = transfers; } catch { }
            try { TransfersCollectionM.ItemsSource = transfers; } catch { }
            try { AvatarLabel.Text = initial; SidebarUsernameLabel.Text = username; } catch { }
            try { AvatarLabelM.Text = initial; } catch { }
        }

        private async void OnTransferClicked(object sender, EventArgs e)
        {
            // Взимаме от десктоп или мобилен picker
            AccountDto? selectedAccount = null;
            try { if (FromAccountPicker.SelectedItem is AccountDto a) selectedAccount = a; } catch { }
            try { if (selectedAccount == null && FromAccountPickerM.SelectedItem is AccountDto b) selectedAccount = b; } catch { }

            if (selectedAccount == null)
            {
                await DisplayAlert("Error", "Please select source account.", "OK");
                return;
            }

            // Взимаме от десктоп или мобилен Entry
            var toIbanText = string.Empty;
            try { toIbanText = ToIbanEntry.Text; } catch { }
            if (string.IsNullOrWhiteSpace(toIbanText))
                try { toIbanText = ToIbanEntryM.Text; } catch { }

            if (string.IsNullOrWhiteSpace(toIbanText))
            {
                await DisplayAlert("Error", "Please enter recipient IBAN.", "OK");
                return;
            }

            var toIban = BankInputNormalizer.NormalizeIban(toIbanText);
            if (string.Equals(toIban, BankInputNormalizer.NormalizeIban(selectedAccount.Iban), StringComparison.OrdinalIgnoreCase))
            {
                await DisplayAlert("Error", "Recipient IBAN cannot be the same as the source account.", "OK");
                return;
            }

            var amountText = string.Empty;
            try { amountText = AmountEntry.Text; } catch { }
            if (string.IsNullOrWhiteSpace(amountText))
                try { amountText = AmountEntryM.Text; } catch { }

            if (!BankInputNormalizer.TryParseAmount(amountText, out decimal amount) || amount <= 0)
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

            var descText = string.Empty;
            try { descText = DescriptionEntry.Text; } catch { }
            if (string.IsNullOrWhiteSpace(descText))
                try { descText = DescriptionEntryM.Text; } catch { }

            var request = new TransferRequest
            {
                FromAccountId = selectedAccount.Id,
                ToAccountId = toAccount.Id,
                Amount = amount,
                Description = descText?.Trim() ?? string.Empty
            };

            var (success, errorMessage) = await _transferApiService.TransferAsync(request);
            if (!success)
            {
                await DisplayAlert("Error", BankInputNormalizer.ToDisplayError(errorMessage), "OK");
                return;
            }

            await DisplayAlert("Success", "Transfer submitted for admin approval.", "OK");

            try { ToIbanEntry.Text = string.Empty; AmountEntry.Text = string.Empty; DescriptionEntry.Text = string.Empty; } catch { }
            try { ToIbanEntryM.Text = string.Empty; AmountEntryM.Text = string.Empty; DescriptionEntryM.Text = string.Empty; } catch { }

            await LoadDataAsync();
        }

        private void OnNavigateToAccounts(object sender, EventArgs e) =>
            ((AppShell)Shell.Current).NavigateTo("Accounts");

        private void OnNavigateToPayments(object sender, EventArgs e) =>
            ((AppShell)Shell.Current).NavigateTo("Payments");

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
