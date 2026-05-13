using BankAPP.Services;
using BankAPP.Shared.DTOs;
using Microsoft.Maui.Graphics;

namespace BankAPP
{
    public partial class CreateCardPage : ContentPage
    {
        private readonly AdminApiService _adminApiService;
        private AdminAccountDto? _selectedAccount;

        public CreateCardPage(AdminApiService adminApiService)
        {
            InitializeComponent();
            _adminApiService = adminApiService;
            CardTypePicker.ItemsSource = new List<string> { "Debit", "Credit" };
            CardTypePicker.SelectedIndex = 0;
        }

        public event Func<Task>? CardCreated;

        public void InitializeForAccount(AdminAccountDto account)
        {
            _selectedAccount = account;
            UserLabel.Text = account.Username;
            AccountLabel.Text = account.DisplayText;
            CardStatusLabel.Text = string.Empty;
            CardStatusLabel.TextColor = Colors.Green;
        }

        private async void OnCreateCardClicked(object sender, EventArgs e)
        {
            if (_selectedAccount is null)
            {
                await DisplayAlert("Validation error", "No account selected.", "OK");
                return;
            }

            if (CardTypePicker.SelectedItem is not string cardType)
            {
                await DisplayAlert("Validation error", "Please choose a card type.", "OK");
                return;
            }

            var request = new CreateCardRequest
            {
                AccountId = _selectedAccount.AccountId,
                CardType = cardType
            };

            var card = await _adminApiService.CreateCardAsync(request);
            if (card is null)
            {
                CardStatusLabel.TextColor = Colors.Red;
                CardStatusLabel.Text = "Failed to create card.";
                return;
            }

            CardStatusLabel.TextColor = Colors.Green;
            CardStatusLabel.Text = $"Card created: {card.DisplayText}";
            await DisplayAlert("Success", $"Card created: {card.DisplayText}", "OK");
            
            if (CardCreated != null)
                await CardCreated();
            
            await Navigation.PopAsync();
        }
    }
}
