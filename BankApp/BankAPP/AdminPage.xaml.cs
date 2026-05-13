using BankAPP.Services;
using BankAPP.Shared.DTOs;
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
                return;
            }

            StatusLabel.Text = string.Empty;
            _allAccounts = await _adminApiService.GetAllAccountsAsync();
            AccountsCollection.ItemsSource = _allAccounts;

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
            MerchantPicker.SelectedIndex = MerchantPicker.ItemsSource is System.Collections.ICollection merchantCollection && merchantCollection.Count > 0 ? 0 : -1;
            await LoadLocationsAsync();
        }

        private async Task LoadLocationsAsync()
        {
            if (MerchantPicker.SelectedItem is not AdminMerchantDto merchant)
            {
                LocationPicker.ItemsSource = new List<AdminLocationDto>();
                return;
            }

            LocationPicker.ItemsSource = await _adminApiService.GetLocationsByMerchantAsync(merchant.MerchantId);
            LocationPicker.SelectedIndex = LocationPicker.ItemsSource is System.Collections.ICollection locationCollection && locationCollection.Count > 0 ? 0 : -1;
        }

        private void UpdateAccountSelection()
        {
            if (UserPicker.SelectedItem is not string chosenUser)
            {
                AccountPicker.ItemsSource = new List<AdminAccountDto>();
                SelectedAccountBalanceLabel.Text = "Balance:";
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
            {
                SelectedAccountBalanceLabel.Text = $"Balance: {account.Balance:F2} {account.Currency}";
            }
            else
            {
                SelectedAccountBalanceLabel.Text = "Balance:";
            }
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

        private void OnUserChanged(object sender, EventArgs e)
        {
            UpdateAccountSelection();
        }

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
            
            // Hook up event to reload cards after creation
            createCardPage.CardCreated += async () =>
            {
                _allCards = await _adminApiService.GetUserCardsAsync();
                UpdateCardSelection();
            };
            
            await Navigation.PushAsync(createCardPage);
        }

        private async void OnMerchantChanged(object sender, EventArgs e)
        {
            await LoadLocationsAsync();
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

            if (!decimal.TryParse(AmountEntry.Text, out var amount) || amount <= 0)
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
                StatusLabel.Text = "POS transaction created successfully.";
                AmountEntry.Text = string.Empty;
                await Task.Delay(2000);
                await LoadAdminDashboardAsync();
            }
            else
            {
                StatusLabel.TextColor = Microsoft.Maui.Graphics.Colors.Red;
                StatusLabel.Text = $"Failed: {errorMessage}";
            }
        }

        private async Task LoadPendingTransfersAsync()
        {
            if (!SessionManager.IsAdmin)
            {
                PendingTransfersCollection.ItemsSource = new List<PendingTransferDto>();
                return;
            }

            PendingTransfersCollection.ItemsSource = await _adminApiService.GetPendingTransfersAsync();
        }

        private async void OnApproveTransferClicked(object sender, EventArgs e)
        {
            if (sender is not Button button || button.CommandParameter is not int movementId)
                return;

            var success = await _adminApiService.ApproveTransferAsync(movementId);
            if (success)
            {
                await DisplayAlert("Success", "Transfer approved successfully.", "OK");
                await LoadPendingTransfersAsync();
            }
            else
            {
                await DisplayAlert("Error", "Failed to approve transfer.", "OK");
            }
        }

        private async void OnRejectTransferClicked(object sender, EventArgs e)
        {
            if (sender is not Button button || button.CommandParameter is not int movementId)
                return;

            var success = await _adminApiService.RejectTransferAsync(movementId);
            if (success)
            {
                await DisplayAlert("Success", "Transfer rejected successfully.", "OK");
                await LoadPendingTransfersAsync();
            }
            else
            {
                await DisplayAlert("Error", "Failed to reject transfer.", "OK");
            }
        }
    }
}
