using System.Security.Claims;
using BankAPP.Shared.Data;
using BankAPP.Shared.DTOs;
using BankAPP.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BankAPI.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class MovementsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public MovementsController(AppDbContext context)
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

        [HttpGet("me")]
        public async Task<IActionResult> GetMyMovements()
        {
            var userId = GetUserId();

            var accountIds = await _context.UserAccounts
                .Where(ua => ua.UserId == userId)
                .Select(ua => ua.AccountId)
                .ToListAsync();

            var movements = await _context.Movements
                .Where(m => accountIds.Contains(m.AccountId))
                .OrderByDescending(m => m.MovementDateTime)
                .ToListAsync();

            return Ok(movements);
        }

        [HttpPost("me")]
        public async Task<IActionResult> AddMyMovement(CreateMovementRequest request)
        {
            var userId = GetUserId();
            var userName = GetUserName();
            var isAdmin = string.Equals(userName, "admin", StringComparison.OrdinalIgnoreCase);

            if (!isAdmin)
                return Forbid("Direct movement creation is only available for admin POS terminal. Regular users should use transfers.");

            var account = await _context.Accounts.FirstOrDefaultAsync(a => a.Id == request.AccountId);
            if (account == null)
                return BadRequest(new { message = "Account not found" });

            var movement = new Movement
            {
                AccountId = request.AccountId,
                Amount = request.Amount,
                MovementType = request.MovementType,
                Description = request.Description,
                Currency = "BGN",
                Status = "completed",
                ReferenceNumber = Guid.NewGuid().ToString("N"),
                MovementDateTime = DateTime.Now
            };

            _context.Movements.Add(movement);

            if (movement.MovementType == BankAPP.Shared.Constants.MovementTypes.Deposit)
                account.Balance += movement.Amount;
            else if (BankAPP.Shared.Constants.MovementTypes.IsExpense(movement.MovementType))
            {
                if (account.Balance < movement.Amount)
                    return BadRequest(new { message = "Insufficient funds" });
                account.Balance -= movement.Amount;
            }

            await _context.SaveChangesAsync();

            return Ok(movement);
        }

        [HttpGet("account/{accountId:int}")]
        public async Task<IActionResult> GetByAccount(int accountId)
        {
            var userId = GetUserId();

            var hasAccess = await _context.UserAccounts
                .AnyAsync(ua => ua.UserId == userId && ua.AccountId == accountId);

            if (!hasAccess)
                return Forbid();

            var movements = await _context.Movements
                .Where(m => m.AccountId == accountId)
                .OrderByDescending(m => m.MovementDateTime)
                .ToListAsync();

            return Ok(movements);
        }
    }
}
