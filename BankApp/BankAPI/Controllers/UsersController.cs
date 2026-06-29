using BankAPI.Helpers;
using BankShared.Data;
using BankShared.DTOs;
using BankShared.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using BankAPI.Services;
using System.Security.Claims;

namespace BankAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _context;

        private readonly JwtService _jwtService;
        private readonly IPasswordHasher<User> _passwordHasher;

        public UsersController(
            AppDbContext context,
            JwtService jwtService,
            IPasswordHasher<User> passwordHasher)
        {
            _context = context;
            _jwtService = jwtService;
            _passwordHasher = passwordHasher;
        }

        [HttpGet("{username}")]
        public async Task<IActionResult> GetByUsername(string username)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == username);

            if (user == null)
                return NotFound();

            return Ok(user);
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login(LoginRequest request)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == request.Username);

            if (user == null)
                return Unauthorized(new { message = "Invalid credentials" });

            var verificationResult = VerifyPassword(user, request.Password);
            var isLegacyPlainTextMatch = user.PasswordHash == request.Password;

            if (verificationResult == PasswordVerificationResult.Failed && !isLegacyPlainTextMatch)
                return Unauthorized(new { message = "Invalid credentials" });

            if (verificationResult == PasswordVerificationResult.SuccessRehashNeeded ||
                isLegacyPlainTextMatch)
            {
                user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);
                await _context.SaveChangesAsync();
            }

            var token = _jwtService.GenerateToken(user);

            return Ok(new LoginResponse
            {
                Token = token,
                User = user
            });
        }

        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register(RegisterRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Username) ||
                string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new { message = "Invalid data" });
            }

            bool exists = await _context.Users
                .AnyAsync(u => u.Username == request.Username);

            if (exists)
                return BadRequest(new { message = "User already exists" });

            var user = new User
            {
                Name = request.Name,
                Username = request.Username,
                Email = request.Email,
                Egn = request.Egn,
                RegistrationDate = DateTime.Now
            };

            user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Create the user's first account.
            var account = new Account
            {
                IBAN = IbanGenerator.Generate(),
                Balance = 0,
                Currency = "BGN"
            };

            _context.Accounts.Add(account);
            await _context.SaveChangesAsync();

            // Link the user to the account.
            var userAccount = new UserAccount
            {
                UserId = user.Id,
                AccountId = account.Id,
                Role = "owner"
            };

            _context.UserAccounts.Add(userAccount);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                user.Id,
                user.Username,
                account.IBAN,
                account.Balance
            });
        }

        private PasswordVerificationResult VerifyPassword(User user, string password)
        {
            try
            {
                return _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);
            }
            catch (FormatException)
            {
                return PasswordVerificationResult.Failed;
            }
        }
    }
}
