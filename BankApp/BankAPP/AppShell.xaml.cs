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
            AdminTab.IsVisible = Services.SessionManager.IsAdmin;
        }
    }
}
