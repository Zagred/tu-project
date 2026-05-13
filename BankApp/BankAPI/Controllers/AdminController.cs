using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BankAPP.Shared.DTOs;
using BankAPP.Shared.Data;
using BankAPP.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace BankAPI.Controllers
{
    [ApiController]
    [Route("api/admin")]
    [Authorize]
    public class AdminController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<AdminController> _logger;

        public AdminController(AppDbContext context, ILogger<AdminController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Admin POS Transaction - simulates a payment from a card at a merchant location
        /// Only accessible to admin user
        /// </summary>
        [HttpPost("pos-transaction")]
        public async Task<IActionResult> CreatePosTransaction([FromBody] PosTransactionRequest request)
        {
            try
            {
                // Get current user
                var userName = User.FindFirst("sub")?.Value ?? User.Identity?.Name;
                if (string.IsNullOrEmpty(userName))
                    return Unauthorized("User not found");

                // Check if user is admin
                var user = _context.Users.FirstOrDefault(u => u.Username == userName);
                if (user == null || user.Username != "admin")
                    return Forbid("Only admin can create POS transactions");

                // Get the card
                var card = _context.Cards.FirstOrDefault(c => c.CardId == request.CardId);
                if (card == null)
                    return NotFound("Card not found");

                // Get the account
                var account = _context.Accounts.FirstOrDefault(a => a.Id == card.AccountId);
                if (account == null)
                    return NotFound("Account not found");

                // Check balance
                if (account.Balance < request.Amount)
                    return BadRequest("Insufficient funds");

                // Get location and merchant info
                var location = _context.Locations.Include(l => l.Merchant)
                    .FirstOrDefault(l => l.LocationId == request.LocationId);
                if (location == null)
                    return NotFound("Location not found");

                // Deduct from account
                account.Balance -= request.Amount;

                // Create movement record
                var movement = new Movement
                {
                    AccountId = account.Id,
                    CardId = card.CardId,
                    MerchantId = location.MerchantId,
                    LocationId = request.LocationId,
                    Amount = request.Amount,
                    Currency = account.Currency,
                    MovementType = BankAPP.Shared.Constants.MovementTypes.CardPayment,
                    Status = "completed",
                    Description = $"POS Payment at {location?.Merchant?.MerchantName ?? "Unknown Merchant"}, {location!.City}",
                    ReferenceNumber = $"POS-{DateTime.UtcNow.Ticks}",
                    MovementDateTime = DateTime.UtcNow
                };

                _context.Movements.Add(movement);
                _context.Accounts.Update(account);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Admin {userName} created POS transaction: {movement.MovementId}");

                return Ok(new { 
                    success = true, 
                    movementId = movement.MovementId,
                    message = $"POS transaction completed. New balance: {account.Balance:F2}"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating POS transaction");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Get all bank accounts with owner information
        /// </summary>
        [HttpGet("accounts")]
        public IActionResult GetAccountsWithOwners()
        {
            try
            {
                var userName = User.FindFirst("sub")?.Value ?? User.Identity?.Name;
                if (string.IsNullOrEmpty(userName) || userName != "admin")
                    return Forbid("Only admin can access account information");

                var accounts = _context.UserAccounts
                    .Join(_context.Users,
                        ua => ua.UserId,
                        u => u.Id,
                        (ua, u) => new { ua, u })
                    .Join(_context.Accounts,
                        combined => combined.ua.AccountId,
                        a => a.Id,
                        (combined, a) => new AdminAccountDto
                        {
                            AccountId = a.Id,
                            Iban = a.IBAN,
                            Balance = a.Balance,
                            Currency = a.Currency,
                            Status = a.Status,
                            OwnerName = combined.u.Name,
                            Username = combined.u.Username,
                            Role = combined.ua.Role
                        })
                    .ToList();

                return Ok(accounts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching accounts");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Get all merchants for POS selection
        /// </summary>
        [HttpGet("merchants")]
        public IActionResult GetMerchants()
        {
            try
            {
                var userName = User.FindFirst("sub")?.Value ?? User.Identity?.Name;
                if (string.IsNullOrEmpty(userName) || userName != "admin")
                    return Forbid("Only admin can access merchants list");

                var merchants = _context.Merchants.Select(m => new AdminMerchantDto
                {
                    MerchantId = m.MerchantId,
                    MerchantName = m.MerchantName,
                    MerchantCategory = m.MerchantCategory
                }).ToList();

                return Ok(merchants);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching merchants");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Get locations by merchant
        /// </summary>
        [HttpGet("merchants/{merchantId}/locations")]
        public IActionResult GetLocationsByMerchant(int merchantId)
        {
            try
            {
                var userName = User.FindFirst("sub")?.Value ?? User.Identity?.Name;
                if (string.IsNullOrEmpty(userName) || userName != "admin")
                    return Forbid("Only admin can access locations list");

                var locations = _context.Locations
                    .Where(l => l.MerchantId == merchantId)
                    .Select(l => new AdminLocationDto
                    {
                        LocationId = l.LocationId,
                        Address = l.Address,
                        City = l.City
                    }).ToList();

                return Ok(locations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching locations");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Get user cards (for selecting which card to charge)
        /// </summary>
        [HttpGet("user-cards")]
        public IActionResult GetUserCards()
        {
            try
            {
                var userName = User.FindFirst("sub")?.Value ?? User.Identity?.Name;
                if (string.IsNullOrEmpty(userName) || userName != "admin")
                    return Forbid("Only admin can access user cards");

                var cards = _context.Cards.Select(c => new AdminCardDto
                {
                    CardId = c.CardId,
                    AccountId = c.AccountId,
                    MaskedCardNumber = c.MaskedCardNumber,
                    CardType = c.CardType,
                    AccountIban = _context.Accounts
                        .Where(a => a.Id == c.AccountId)
                        .Select(a => a.IBAN)
                        .FirstOrDefault() ?? string.Empty
                }).ToList();

                return Ok(cards);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching user cards");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Create a card for an account.
        /// </summary>
        [HttpPost("cards")]
        public async Task<IActionResult> CreateCard([FromBody] CreateCardRequest request)
        {
            try
            {
                var userName = User.FindFirst("sub")?.Value ?? User.Identity?.Name;
                if (string.IsNullOrEmpty(userName) || userName != "admin")
                    return Forbid("Only admin can create cards");

                var account = _context.Accounts.FirstOrDefault(a => a.Id == request.AccountId);
                if (account == null)
                    return NotFound("Account not found");

                var userAccount = _context.UserAccounts.FirstOrDefault(ua => ua.AccountId == request.AccountId);
                if (userAccount == null)
                    return BadRequest("Account is not linked to a user");

                var card = new Card
                {
                    AccountId = request.AccountId,
                    UserId = userAccount.UserId,
                    MaskedCardNumber = GenerateMaskedCardNumber(),
                    CardType = string.IsNullOrWhiteSpace(request.CardType) ? "Debit" : request.CardType,
                    ExpirationDate = DateTime.UtcNow.AddYears(3),
                    Status = "active"
                };

                _context.Cards.Add(card);
                await _context.SaveChangesAsync();

                var result = new AdminCardDto
                {
                    CardId = card.CardId,
                    AccountId = card.AccountId,
                    MaskedCardNumber = card.MaskedCardNumber,
                    CardType = card.CardType,
                    AccountIban = account.IBAN
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating card");
                return StatusCode(500, "Internal server error");
            }
        }

        private static string GenerateMaskedCardNumber()
        {
            var random = new Random();
            var last4 = random.Next(1000, 10000);
            return $"**** **** **** {last4}";
        }

        /// <summary>
        /// Get pending transfer approvals
        /// </summary>
        [HttpGet("pending-transfers")]
        public IActionResult GetPendingTransfers()
        {
            try
            {
                var userName = User.FindFirst("sub")?.Value ?? User.Identity?.Name;
                if (string.IsNullOrEmpty(userName) || userName != "admin")
                    return Forbid("Only admin can view transfer approvals");

                var pendingTransfers = _context.Movements
                    .Where(m => m.MovementType == BankAPP.Shared.Constants.MovementTypes.Transfer && m.Status == "pending")
                    .Include(m => m.Account)
                    .Select(m => new PendingTransferDto
                    {
                        MovementId = m.MovementId,
                        Amount = m.Amount,
                        Currency = m.Currency,
                        Description = m.Description ?? string.Empty,
                        MovementDateTime = m.MovementDateTime,
                        FromAccount = m.Account == null ? string.Empty : m.Account.IBAN
                    })
                    .ToList();

                return Ok(pendingTransfers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching pending transfers");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Approve a pending transfer
        /// </summary>
        [HttpPost("transfers/{movementId}/approve")]
        public async Task<IActionResult> ApproveTransfer(int movementId)
        {
            try
            {
                var userName = User.FindFirst("sub")?.Value ?? User.Identity?.Name;
                if (string.IsNullOrEmpty(userName) || userName != "admin")
                    return Forbid("Only admin can approve transfers");

                var debitMovement = _context.Movements.FirstOrDefault(m => m.MovementId == movementId);
                if (debitMovement == null)
                    return NotFound("Transfer not found");

                if (debitMovement.Status != "pending")
                    return BadRequest("Transfer is not pending");

                // Get the corresponding credit movement by reference number
                var creditMovement = _context.Movements
                    .FirstOrDefault(m => m.ReferenceNumber == debitMovement.ReferenceNumber && 
                                       m.MovementId != movementId && 
                                       m.MovementType == BankAPP.Shared.Constants.MovementTypes.Transfer);

                // Get accounts
                var fromAccount = _context.Accounts.FirstOrDefault(a => a.Id == debitMovement.AccountId);
                if (fromAccount == null)
                    return NotFound("From account not found");

                // Check balance
                if (fromAccount.Balance < debitMovement.Amount)
                    return BadRequest("Insufficient funds for approved transfer");

                // Update balances
                fromAccount.Balance -= debitMovement.Amount;

                if (creditMovement != null)
                {
                    var toAccount = _context.Accounts.FirstOrDefault(a => a.Id == creditMovement.AccountId);
                    if (toAccount != null)
                    {
                        toAccount.Balance += creditMovement.Amount;
                        creditMovement.Status = "completed";
                        _context.Accounts.Update(toAccount);
                    }
                }

                // Update movements to completed
                debitMovement.Status = "completed";
                _context.Movements.Update(debitMovement);
                _context.Accounts.Update(fromAccount);

                await _context.SaveChangesAsync();

                _logger.LogInformation($"Admin {userName} approved transfer {movementId}");

                return Ok(new { success = true, message = "Transfer approved and completed" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving transfer");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Reject a pending transfer
        /// </summary>
        [HttpPost("transfers/{movementId}/reject")]
        public async Task<IActionResult> RejectTransfer(int movementId)
        {
            try
            {
                var userName = User.FindFirst("sub")?.Value ?? User.Identity?.Name;
                if (string.IsNullOrEmpty(userName) || userName != "admin")
                    return Forbid("Only admin can reject transfers");

                var debitMovement = _context.Movements.FirstOrDefault(m => m.MovementId == movementId);
                if (debitMovement == null)
                    return NotFound("Transfer not found");

                if (debitMovement.Status != "pending")
                    return BadRequest("Transfer is not pending");

                // Get the corresponding credit movement
                var creditMovement = _context.Movements
                    .FirstOrDefault(m => m.ReferenceNumber == debitMovement.ReferenceNumber && 
                                       m.MovementId != movementId && 
                                       m.MovementType == BankAPP.Shared.Constants.MovementTypes.Transfer);

                // Mark both as rejected
                debitMovement.Status = "rejected";
                if (creditMovement != null)
                {
                    creditMovement.Status = "rejected";
                    _context.Movements.Update(creditMovement);
                }

                _context.Movements.Update(debitMovement);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Admin {userName} rejected transfer {movementId}");

                return Ok(new { success = true, message = "Transfer rejected" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting transfer");
                return StatusCode(500, "Internal server error");
            }
        }
    }
}
