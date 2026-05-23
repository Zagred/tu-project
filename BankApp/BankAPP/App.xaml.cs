using Microsoft.Extensions.DependencyInjection;

namespace BankAPP
{
    public partial class App : Application
    {
        public App(IServiceProvider serviceProvider)
        {
            InitializeComponent();

            var loginPage = serviceProvider.GetRequiredService<LoginPage>();
            var navPage = new NavigationPage(loginPage);
            NavigationPage.SetHasNavigationBar(loginPage, false);
            MainPage = navPage;
        }
    }
}