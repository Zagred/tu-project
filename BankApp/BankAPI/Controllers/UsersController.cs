using BankAPI.Helpers;
using BankAPP.Shared.Data;
using BankAPP.Shared.DTOs;
using BankAPP.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
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

        public UsersController(AppDbContext context, JwtService jwtService)
        {
            _context = context;
            _jwtService = jwtService;
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

            if (user == null || user.PasswordHash != request.Password)
                return Unauthorized(new { message = "Invalid credentials" });

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
                PasswordHash = request.Password,
                Email = request.Email,
                Egn = request.Egn,
                RegistrationDate = DateTime.Now
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // 👉 Създаваме account
            var account = new Account
            {
                IBAN = IbanGenerator.Generate(),
                Balance = 0,
                Currency = "BGN"
            };

            _context.Accounts.Add(account);
            await _context.SaveChangesAsync();

            // 👉 Връзка User ↔ Account (M:N)
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
    }
}