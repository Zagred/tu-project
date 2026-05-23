namespace BankAPP
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            RefreshAdminVisibility();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            RefreshAdminVisibility();
        }

        private void RefreshAdminVisibility()
        {
            AdminShell.IsVisible = Services.SessionManager.IsAdmin;
        }

        public void NavigateTo(string route)
        {
            switch (route)
            {
                case "Transfers":
                    CurrentItem = TransfersShell;
                    break;
                case "Payments":
                    CurrentItem = PaymentsShell;
                    break;
                case "Accounts":
                    CurrentItem = AccountsShell;
                    break;
                case "Admin":
                    CurrentItem = AdminShell;
                    break;
            }
        }
    }
}