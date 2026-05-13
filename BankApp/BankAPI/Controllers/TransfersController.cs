using System.Security.Claims;
using BankAPP.Shared.Data;
using BankAPP.Shared.DTOs;
using BankAPP.Shared.Models;
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

        public TransfersController(AppDbContext context)
        {
            _context = context;
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

            if (fromAccount.Balance < request.Amount)
                return BadRequest("Insufficient funds");

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Create pending debit movement (not updating balance yet)
                var debit = new Movement
                {
                    AccountId = fromAccount.Id,
                    Amount = request.Amount,
                    MovementType = BankAPP.Shared.Constants.MovementTypes.Transfer,
                    Description = $"Transfer to account {toAccount.Id}: {request.Description}",
                    Currency = "BGN",
                    Status = "pending",  // Pending admin approval
                    ReferenceNumber = Guid.NewGuid().ToString("N"),
                    MovementDateTime = DateTime.Now
                };

                // Create pending credit movement
                var credit = new Movement
                {
                    AccountId = toAccount.Id,
                    Amount = request.Amount,
                    MovementType = BankAPP.Shared.Constants.MovementTypes.Transfer,
                    Description = $"Transfer from account {fromAccount.Id}: {request.Description}",
                    Currency = "BGN",
                    Status = "pending",  // Pending admin approval
                    ReferenceNumber = debit.ReferenceNumber,  // Link debit and credit
                    MovementDateTime = DateTime.Now
                };

                _context.Movements.AddRange(debit, credit);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new { message = "Transfer submitted for approval. Please wait for admin confirmation." });
            }
            catch
            {
                await transaction.RollbackAsync();
                return StatusCode(500, "Transfer failed");
            }
        }
    }
}
