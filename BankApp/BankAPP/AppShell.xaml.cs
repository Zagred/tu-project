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

        public void RefreshAdminVisibility()
        {
            AdminShell.IsVisible = Services.SessionManager.IsAdmin;
        }

        public void NavigateTo(string route)
        {
            RefreshAdminVisibility();

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
                    if (Services.SessionManager.IsAdmin)
                        CurrentItem = AdminShell;
                    break;
            }
        }
    }
}
