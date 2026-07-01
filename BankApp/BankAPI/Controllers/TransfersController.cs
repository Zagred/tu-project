using System.Security.Claims;
using BankShared.Data;
using BankShared.DTOs;
using BankShared.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace BankAPI.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class TransfersController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<TransfersController> _logger;

        public TransfersController(AppDbContext context, ILogger<TransfersController> logger)
        {
            _context = context;
            _logger = logger;
        }

        private int GetUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null)
                throw new UnauthorizedAccessException();
            return int.Parse(userIdClaim);
        }

        private string GetUserName()
        {
            return User.FindFirst(ClaimTypes.Name)?.Value ?? User.Identity?.Name ?? string.Empty;
        }

        private bool IsAdmin()
        {
            return string.Equals(GetUserName(), "admin", StringComparison.OrdinalIgnoreCase);
        }

        [HttpPost]
        public async Task<IActionResult> Transfer(TransferRequest request)
        {
            try
            {
                if (request.Amount <= 0)
                    return BadRequest("Invalid amount");

                if (request.FromAccountId == request.ToAccountId)
                    return BadRequest("Cannot transfer to the same account");

                var userId = GetUserId();
                if (!IsAdmin())
                {
                    var hasAccess = await _context.UserAccounts
                        .AnyAsync(ua => ua.UserId == userId && ua.AccountId == request.FromAccountId);

                    if (!hasAccess)
                        return Forbid("You can only submit transfers from your own account");
                }

                var fromAccount = await _context.Accounts
                    .FirstOrDefaultAsync(a => a.Id == request.FromAccountId);

                var toAccount = await _context.Accounts
                    .FirstOrDefaultAsync(a => a.Id == request.ToAccountId);

                if (fromAccount == null || toAccount == null)
                    return NotFound("Account not found");

                if (!string.Equals(fromAccount.Currency, toAccount.Currency, StringComparison.OrdinalIgnoreCase))
                    return BadRequest("Cannot transfer between accounts with different currencies");

                if (fromAccount.Balance < request.Amount)
                    return BadRequest("Insufficient funds");

                var description = string.IsNullOrWhiteSpace(request.Description)
                    ? "Transfer"
                    : request.Description.Trim();

                var debit = new Movement
                {
                    AccountId = fromAccount.Id,
                    Amount = request.Amount,
                    MovementType = BankShared.Constants.MovementTypes.Transfer,
                    Description = $"Transfer to account {toAccount.Id}: {description}",
                    Currency = fromAccount.Currency,
                    Status = "pending",
                    ReferenceNumber = $"TRF{DateTime.UtcNow.Ticks}",
                    MovementDateTime = DateTime.UtcNow
                };

                _context.Movements.Add(debit);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Transfer submitted for approval. Please wait for admin confirmation." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting transfer from account {FromAccountId} to account {ToAccountId}",
                    request.FromAccountId,
                    request.ToAccountId);

                return StatusCode(500, $"Transfer failed: {ex.GetBaseException().Message}");
            }
        }
    }
}
