namespace BankAPI.Helpers
{
    public static class IbanGenerator
    {
        public static string Generate()
        {
            var random = new Random();

            string country = "BG";
            string bankCode = "BANK"; // demo
            string accountNumber = random.Next(10000000, 99999999).ToString();

            return $"{country}{random.Next(10, 99)}{bankCode}{accountNumber}";
        }
    }
}