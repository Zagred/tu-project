using BankAPP.Services;
using BankShared.DTOs;
using BankShared.Utilities;
using System.Linq;

namespace BankAPP
{
    public partial class AdminPage : ContentPage
    {
        private readonly AdminApiService _adminApiService;
        private List<AdminAccountDto> _allAccounts = new();
        private List<AdminCardDto> _allCards = new();

        public AdminPage(AdminApiService adminApiService)
        {
            InitializeComponent();
            NavigationPage.SetHasNavigationBar(this, false);
            _adminApiService = adminApiService;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadAdminDashboardAsync();
            await LoadPendingTransfersAsync();
        }

        private async Task LoadAdminDashboardAsync()
        {
            if (!SessionManager.IsAdmin)
            {
                await DisplayAlert("Access denied", "This page is only available for the admin user.", "OK");
                AccountsCollection.ItemsSource = new List<AdminAccountDto>();
                MerchantPicker.ItemsSource = new List<AdminMerchantDto>();
                LocationPicker.ItemsSource = new List<AdminLocationDto>();
                CardPicker.ItemsSource = new List<AdminCardDto>();
                TransferFromAccountPicker.ItemsSource = new List<AdminAccountDto>();
                TransferToAccountPicker.ItemsSource = new List<AdminAccountDto>();
                return;
            }

            StatusLabel.Text = string.Empty;
            _allAccounts = await _adminApiService.GetAllAccountsAsync();
            AccountsCollection.ItemsSource = _allAccounts;
            UpdateAdminTransferSelection();

            var users = _allAccounts
                .Select(a => a.Username)
                .Distinct()
                .OrderBy(u => u)
                .ToList();

            UserPicker.ItemsSource = users;
            UserPicker.SelectedIndex = users.Count > 0 ? 0 : -1;
            _allCards = await _adminApiService.GetUserCardsAsync();
            UpdateAccountSelection();

            MerchantPicker.ItemsSource = await _adminApiService.GetMerchantsAsync();
            MerchantPicker.SelectedIndex = MerchantPicker.ItemsSource is System.Collections.ICollection merchantCollection
                && merchantCollection.Count > 0 ? 0 : -1;
            await LoadLocationsAsync();
            await UpdateStats();
        }

        private void UpdateAdminTransferSelection()
        {
            var accounts = _allAccounts
                .OrderBy(a => a.Username)
                .ThenBy(a => a.Iban)
                .ToList();

            TransferFromAccountPicker.ItemsSource = accounts;
            TransferToAccountPicker.ItemsSource = accounts;

            TransferFromAccountPicker.SelectedIndex = accounts.Count > 0 ? 0 : -1;
            TransferToAccountPicker.SelectedIndex = accounts.Count > 1 ? 1 : -1;
        }

        private async Task UpdateStats()
        {
            try { UserCountLabel.Text = _allAccounts.Select(a => a.Username).Distinct().Count().ToString(); } catch { }
            try { AccountCountLabel.Text = _allAccounts.Count.ToString(); } catch { }
            var pending = await _adminApiService.GetPendingTransfersAsync();
            try { PendingCountLabel.Text = pending.Count.ToString(); } catch { }
            try { PendingBadgeLabel.Text = pending.Count.ToString(); } catch { }
        }

        private async Task LoadLocationsAsync()
        {
            if (MerchantPicker.SelectedItem is not AdminMerchantDto merchant)
            {
                LocationPicker.ItemsSource = new List<AdminLocationDto>();
                return;
            }

            LocationPicker.ItemsSource = await _adminApiService.GetLocationsByMerchantAsync(merchant.MerchantId);
            LocationPicker.SelectedIndex = LocationPicker.ItemsSource is System.Collections.ICollection locationCollection
                && locationCollection.Count > 0 ? 0 : -1;
        }

        private void UpdateAccountSelection()
        {
            if (UserPicker.SelectedItem is not string chosenUser)
            {
                AccountPicker.ItemsSource = new List<AdminAccountDto>();
                SelectedAccountBalanceLabel.Text = "Баланс: —";
                CardPicker.ItemsSource = new List<AdminCardDto>();
                return;
            }

            var accounts = _allAccounts
                .Where(a => a.Username == chosenUser)
                .OrderBy(a => a.Iban)
                .ToList();

            AccountPicker.ItemsSource = accounts;
            AccountPicker.SelectedIndex = accounts.Count > 0 ? 0 : -1;
            UpdateSelectedAccountBalance();
            UpdateCardSelection();
        }

        private void UpdateSelectedAccountBalance()
        {
            if (AccountPicker.SelectedItem is AdminAccountDto account)
                SelectedAccountBalanceLabel.Text = $"Баланс: {account.Balance:F2} {account.Currency}";
            else
                SelectedAccountBalanceLabel.Text = "Баланс: —";
        }

        private void UpdateCardSelection()
        {
            if (AccountPicker.SelectedItem is not AdminAccountDto account)
            {
                CardPicker.ItemsSource = new List<AdminCardDto>();
                CardPicker.SelectedIndex = -1;
                return;
            }

            var cards = _allCards
                .Where(c => c.AccountId == account.AccountId)
                .OrderBy(c => c.MaskedCardNumber)
                .ToList();

            CardPicker.ItemsSource = cards;
            CardPicker.SelectedIndex = cards.Count > 0 ? 0 : -1;
        }

        private void OnUserChanged(object sender, EventArgs e) => UpdateAccountSelection();

        private void OnAccountChanged(object sender, EventArgs e)
        {
            UpdateSelectedAccountBalance();
            UpdateCardSelection();
        }

        private async void OnOpenCreateCardWindowClicked(object sender, EventArgs e)
        {
            if (AccountPicker.SelectedItem is not AdminAccountDto account)
            {
                await DisplayAlert("Validation error", "Please select an account first.", "OK");
                return;
            }

            var createCardPage = new CreateCardPage(_adminApiService);
            createCardPage.InitializeForAccount(account);

            createCardPage.CardCreated += async () =>
            {
                _allCards = await _adminApiService.GetUserCardsAsync();
                UpdateCardSelection();
            };

            await Navigation.PushAsync(createCardPage);
        }

        private async void OnMerchantChanged(object sender, EventArgs e) => await LoadLocationsAsync();

        private async void OnAddFundsClicked(object sender, EventArgs e)
        {
            if (!SessionManager.IsAdmin)
            {
                await DisplayAlert("Access denied", "Only admin can add money to accounts.", "OK");
                return;
            }

            if (AccountPicker.SelectedItem is not AdminAccountDto account)
            {
                await DisplayAlert("Validation error", "Please select an account.", "OK");
                return;
            }

            if (!BankInputNormalizer.TryParseAmount(AddFundsAmountEntry.Text, out var amount) || amount <= 0)
            {
                await DisplayAlert("Validation error", "Please enter a valid amount.", "OK");
                return;
            }

            var request = new AdminAddFundsRequest
            {
                AccountId = account.AccountId,
                Amount = amount,
                Description = AddFundsDescriptionEntry.Text?.Trim() ?? string.Empty
            };

            var (success, errorMessage) = await _adminApiService.AddFundsAsync(request);
            if (success)
            {
                StatusLabel.TextColor = Microsoft.Maui.Graphics.Colors.Green;
                StatusLabel.Text = "Money added successfully.";
                AddFundsAmountEntry.Text = string.Empty;
                AddFundsDescriptionEntry.Text = string.Empty;
                await LoadAdminDashboardAsync();
                await LoadPendingTransfersAsync();
            }
            else
            {
                StatusLabel.TextColor = Microsoft.Maui.Graphics.Colors.Red;
                StatusLabel.Text = $"Error: {BankInputNormalizer.ToDisplayError(errorMessage)}";
            }
        }

        private async void OnAdminTransferClicked(object sender, EventArgs e)
        {
            if (!SessionManager.IsAdmin)
            {
                await DisplayAlert("Access denied", "Only admin can transfer between accounts.", "OK");
                return;
            }

            if (TransferFromAccountPicker.SelectedItem is not AdminAccountDto fromAccount)
            {
                await DisplayAlert("Validation error", "Please select source account.", "OK");
                return;
            }

            if (TransferToAccountPicker.SelectedItem is not AdminAccountDto toAccount)
            {
                await DisplayAlert("Validation error", "Please select destination account.", "OK");
                return;
            }

            if (fromAccount.AccountId == toAccount.AccountId)
            {
                await DisplayAlert("Validation error", "Source and destination accounts must be different.", "OK");
                return;
            }

            if (!BankInputNormalizer.TryParseAmount(AdminTransferAmountEntry.Text, out var amount) || amount <= 0)
            {
                await DisplayAlert("Validation error", "Please enter a valid amount.", "OK");
                return;
            }

            var request = new AdminAccountTransferRequest
            {
                FromAccountId = fromAccount.AccountId,
                ToAccountId = toAccount.AccountId,
                Amount = amount,
                Description = AdminTransferDescriptionEntry.Text?.Trim() ?? string.Empty
            };

            var (success, errorMessage) = await _adminApiService.TransferBetweenAccountsAsync(request);
            if (success)
            {
                StatusLabel.TextColor = Microsoft.Maui.Graphics.Colors.Green;
                StatusLabel.Text = "Transfer completed successfully.";
                AdminTransferAmountEntry.Text = string.Empty;
                AdminTransferDescriptionEntry.Text = string.Empty;
                await LoadAdminDashboardAsync();
                await LoadPendingTransfersAsync();
            }
            else
            {
                StatusLabel.TextColor = Microsoft.Maui.Graphics.Colors.Red;
                StatusLabel.Text = $"Error: {BankInputNormalizer.ToDisplayError(errorMessage)}";
            }
        }

        private async void OnCreatePosPaymentClicked(object sender, EventArgs e)
        {
            if (!SessionManager.IsAdmin)
            {
                await DisplayAlert("Access denied", "Only admin can create POS payments.", "OK");
                return;
            }

            if (CardPicker.SelectedItem is not AdminCardDto card)
            {
                await DisplayAlert("Validation error", "Please select a card.", "OK");
                return;
            }

            if (LocationPicker.SelectedItem is not AdminLocationDto location)
            {
                await DisplayAlert("Validation error", "Please select a location.", "OK");
                return;
            }

            if (!BankInputNormalizer.TryParseAmount(AmountEntry.Text, out var amount) || amount <= 0)
            {
                await DisplayAlert("Validation error", "Please enter a valid amount.", "OK");
                return;
            }

            var request = new PosTransactionRequest
            {
                CardId = card.CardId,
                LocationId = location.LocationId,
                Amount = amount
            };

            var (success, errorMessage) = await _adminApiService.CreatePosTransactionAsync(request);
            if (success)
            {
                StatusLabel.TextColor = Microsoft.Maui.Graphics.Colors.Green;
                StatusLabel.Text = "POS транзакцията е създадена успешно.";
                AmountEntry.Text = string.Empty;
                await Task.Delay(2000);
                await LoadAdminDashboardAsync();
            }
            else
            {
                StatusLabel.TextColor = Microsoft.Maui.Graphics.Colors.Red;
                StatusLabel.Text = $"Грешка: {errorMessage}";
            }
        }

        private async Task LoadPendingTransfersAsync()
        {
            if (!SessionManager.IsAdmin)
            {
                PendingTransfersCollection.ItemsSource = new List<PendingTransferDto>();
                return;
            }

            var pending = await _adminApiService.GetPendingTransfersAsync();
            PendingTransfersCollection.ItemsSource = pending;
            try { PendingCountLabel.Text = pending.Count.ToString(); } catch { }
            try { PendingBadgeLabel.Text = pending.Count.ToString(); } catch { }
        }

        private async void OnApproveTransferClicked(object sender, EventArgs e)
        {
            if (sender is not Button button || button.CommandParameter is not int movementId)
                return;

            var success = await _adminApiService.ApproveTransferAsync(movementId);
            if (success)
            {
                await DisplayAlert("Успех", "Преводът е одобрен.", "OK");
                await LoadPendingTransfersAsync();
            }
            else
            {
                await DisplayAlert("Грешка", "Неуспешно одобрение.", "OK");
            }
        }

        private async void OnRejectTransferClicked(object sender, EventArgs e)
        {
            if (sender is not Button button || button.CommandParameter is not int movementId)
                return;

            var success = await _adminApiService.RejectTransferAsync(movementId);
            if (success)
            {
                await DisplayAlert("Успех", "Преводът е отказан.", "OK");
                await LoadPendingTransfersAsync();
            }
            else
            {
                await DisplayAlert("Грешка", "Неуспешно отказване.", "OK");
            }
        }

        private void OnNavigateToAccounts(object sender, EventArgs e) =>
            ((AppShell)Shell.Current).NavigateTo("Accounts");

        private void OnNavigateToTransfers(object sender, EventArgs e) =>
            ((AppShell)Shell.Current).NavigateTo("Transfers");

        private void OnNavigateToPayments(object sender, EventArgs e) =>
            ((AppShell)Shell.Current).NavigateTo("Payments");

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
