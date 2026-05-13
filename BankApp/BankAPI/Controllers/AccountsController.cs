using BankAPI.Helpers;
using BankAPP.Shared.Data;
using BankAPP.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace BankAPI.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class AccountsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AccountsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("me")]
        public async Task<IActionResult> GetMyAccounts()
        {
            var userId = GetUserId();

            var accounts = await
                (from ua in _context.UserAccounts
                 join a in _context.Accounts on ua.AccountId equals a.Id
                 where ua.UserId == userId
                 select new
                 {
                     a.Id,
                     a.IBAN,
                     a.Balance,
                     a.Currency,
                     ua.Role
                 })
                .ToListAsync();

            return Ok(accounts);
        }

        [HttpPost("user/{userId:int}")]
        public async Task<IActionResult> CreateAccount(int userId)
        {
            var userExists = await _context.Users.AnyAsync(u => u.Id == userId);

            if (!userExists)
                return NotFound("User not found");

            var account = new Account
            {
                IBAN = IbanGenerator.Generate(),
                Balance = 0,
                Currency = "BGN"
            };

            _context.Accounts.Add(account);
            await _context.SaveChangesAsync();

            var userAccount = new UserAccount
            {
                UserId = userId,
                AccountId = account.Id,
                Role = "owner"
            };

            _context.UserAccounts.Add(userAccount);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                account.Id,
                account.IBAN,
                account.Balance,
                account.Currency
            });
        }

        [HttpPost("me")]
        public async Task<IActionResult> CreateMyAccount()
        {
            var userId = GetUserId();

            var account = new Account
            {
                IBAN = IbanGenerator.Generate(),
                Balance = 0,
                Currency = "BGN"
            };

            _context.Accounts.Add(account);
            await _context.SaveChangesAsync();

            var userAccount = new UserAccount
            {
                UserId = userId,
                AccountId = account.Id,
                Role = "owner"
            };

            _context.UserAccounts.Add(userAccount);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                account.Id,
                account.IBAN,
                account.Balance,
                account.Currency
            });
        }
        private int GetUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (userIdClaim == null)
                throw new UnauthorizedAccessException();

            return int.Parse(userIdClaim);
        }
        [HttpGet("by-iban/{iban}")]
        public async Task<IActionResult> GetByIban(string iban)
        {
            var account = await _context.Accounts
                .FirstOrDefaultAsync(a => a.IBAN == iban);

            if (account == null)
                return NotFound();

            return Ok(account);
        }
    }
}