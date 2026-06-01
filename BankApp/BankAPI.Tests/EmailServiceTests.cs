using BankAPI.Models;
using BankAPI.Services;
using Microsoft.Extensions.Options;
using Xunit;

namespace BankAPI.Tests
{
    public class EmailServiceTests
    {
        [Fact]
        public async Task SendAsyncThrowsWhenEmailSettingsAreMissing()
        {
            var service = new EmailService(Options.Create(new EmailSettings()));

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.SendAsync("user@example.com", "Subject", "Body"));

            Assert.Equal("Email settings are not configured.", exception.Message);
        }

        [Fact]
        public async Task SendAsyncThrowsWhenPasswordIsMissing()
        {
            var service = new EmailService(Options.Create(new EmailSettings
            {
                SmtpServer = "smtp.gmail.com",
                Port = 587,
                SenderEmail = "sender@example.com",
                Username = "sender"
            }));

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.SendAsync("user@example.com", "Subject", "Body"));
        }
    }
}
