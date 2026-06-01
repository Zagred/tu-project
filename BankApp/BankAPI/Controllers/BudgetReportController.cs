using System.Security.Claims;
using BankAPI.Services;
using BankAPP.Shared.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BankAPI.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class BudgetReportController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly EmailService _emailService;

        public BudgetReportController(AppDbContext context, EmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        [HttpPost("monthly-email")]
        public async Task<IActionResult> SendMonthlyReport()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (userIdClaim == null)
                return Unauthorized();

            var userId = int.Parse(userIdClaim);
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                return NotFound();

            if (string.IsNullOrWhiteSpace(user.Email))
                return BadRequest(new { message = "User does not have an email address." });

            var accountIds = await _context.UserAccounts
                .Where(ua => ua.UserId == userId)
                .Select(ua => ua.AccountId)
                .ToListAsync();

            var now = DateTime.UtcNow;
            var monthMovements = await _context.Movements
                .Where(m =>
                    accountIds.Contains(m.AccountId) &&
                    m.MovementDateTime.Month == now.Month &&
                    m.MovementDateTime.Year == now.Year)
                .ToListAsync();

            var summary = BudgetReportBuilder.CreateSummary(monthMovements);
            var body = BudgetReportBuilder.BuildMonthlyEmailBody(summary);

            try
            {
                await _emailService.SendAsync(user.Email, "Your Monthly Budget Report", body);
            }
            catch (Exception)
            {
                return StatusCode(503, new { message = "Email service is not available." });
            }

            return Ok(new { message = "Monthly report sent successfully." });
        }
    }
}
