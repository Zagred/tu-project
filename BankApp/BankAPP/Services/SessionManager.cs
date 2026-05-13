namespace BankAPP.Services
{
    public static class SessionManager
    {
        public static int CurrentUserId { get; set; }
        public static string CurrentUsername { get; set; } = string.Empty;
        public static string Token { get; set; } = string.Empty;
        public static bool IsAdmin =>
            string.Equals(CurrentUsername, "admin", StringComparison.OrdinalIgnoreCase);

        public static void Logout()
        {
            CurrentUserId = 0;
            CurrentUsername = string.Empty;
            Token = string.Empty;
        }
    }
}
