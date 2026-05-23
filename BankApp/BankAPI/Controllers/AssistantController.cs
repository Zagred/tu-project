using System.Security.Claims;
using BankAPI.Services;
using BankAPP.Shared.Data;
using BankAPP.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BankAPI.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class AssistantController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly OllamaAdviceService _ollamaAdviceService;

        public AssistantController(
            AppDbContext context,
            OllamaAdviceService ollamaAdviceService)
        {
            _context = context;
            _ollamaAdviceService = ollamaAdviceService;
        }

        private int GetUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (userIdClaim == null)
                throw new UnauthorizedAccessException();

            return int.Parse(userIdClaim);
        }

        [HttpGet("advice")]
        public async Task<IActionResult> GetAdvice()
        {
            var userId = GetUserId();

            var accountIds = await _context.UserAccounts
                .Where(ua => ua.UserId == userId)
                .Select(ua => ua.AccountId)
                .ToListAsync();

            var movements = await _context.Movements
                .Where(m => accountIds.Contains(m.AccountId))
                .OrderByDescending(m => m.MovementDateTime)
                .Take(20)
                .ToListAsync();

            if (!movements.Any())
            {
                return Ok(new FinancialAdviceResponse
                {
                    Advice = "There are not enough transactions yet to generate financial advice."
                });
            }

            var totalSpent = movements
                .Where(m => m.MovementType == "card_payment" ||
                            m.MovementType == "cash_withdrawal" ||
                            m.MovementType == "fee")
                .Sum(m => m.Amount);

            var totalIncome = movements
                .Where(m => m.MovementType == "deposit" ||
                            m.MovementType == "transfer")
                .Sum(m => m.Amount);

            var groupedExpenses = movements
                .GroupBy(m => m.MovementType)
                .Select(g => $"{g.Key}: {g.Sum(x => x.Amount):F2} BGN")
                .ToList();

            var prompt = $"""
            You are a personal banking assistant.
            Give short, practical and non-investment financial advice.

            User financial summary:
            Total income: {totalIncome:F2} BGN
            Total spending: {totalSpent:F2} BGN
            Spending by type:
            {string.Join("\n", groupedExpenses)}

            Recent transactions:
            {string.Join("\n", movements.Select(m => $"- {m.MovementDateTime:yyyy-MM-dd}: {m.MovementType}, {m.Amount:F2} {m.Currency}, {m.Description}"))}

            Requirements:
            - Answer in simple English.
            - Maximum 5 sentences.
            - Do not recommend stocks, crypto or investments.
            - Focus on budgeting, spending habits and saving money.
            """;

            var advice = await _ollamaAdviceService.GetAdviceAsync(prompt);

            return Ok(new FinancialAdviceResponse
            {
                Advice = advice
            });
        }
    }
}