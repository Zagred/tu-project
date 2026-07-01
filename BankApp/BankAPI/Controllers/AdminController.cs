using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BankShared.Constants;
using BankShared.DTOs;
using BankShared.Data;
using BankShared.Models;
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
                    MovementType = BankShared.Constants.MovementTypes.CardPayment,
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
        /// Add money to a user account as an admin deposit.
        /// </summary>
        [HttpPost("accounts/add-funds")]
        public async Task<IActionResult> AddFundsToAccount([FromBody] AdminAddFundsRequest request)
        {
            try
            {
                var userName = User.FindFirst("sub")?.Value ?? User.Identity?.Name;
                if (string.IsNullOrEmpty(userName) || userName != "admin")
                    return Forbid("Only admin can add funds");

                if (request.AccountId <= 0)
                    return BadRequest("Account is required");

                if (request.Amount <= 0)
                    return BadRequest("Amount must be greater than zero");

                var account = await _context.Accounts.FirstOrDefaultAsync(a => a.Id == request.AccountId);
                if (account == null)
                    return NotFound("Account not found");

                account.Balance += request.Amount;

                var movement = new Movement
                {
                    AccountId = account.Id,
                    Amount = request.Amount,
                    Currency = account.Currency,
                    MovementType = BankShared.Constants.MovementTypes.Deposit,
                    Status = "completed",
                    Description = string.IsNullOrWhiteSpace(request.Description)
                        ? "Admin balance top-up"
                        : request.Description.Trim(),
                    ReferenceNumber = $"ADM-DEP-{DateTime.UtcNow.Ticks}",
                    MovementDateTime = DateTime.UtcNow
                };

                _context.Movements.Add(movement);
                _context.Accounts.Update(account);
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Admin {UserName} added {Amount} {Currency} to account {AccountId}",
                    userName,
                    request.Amount,
                    account.Currency,
                    account.Id);

                return Ok(new
                {
                    success = true,
                    movementId = movement.MovementId,
                    message = $"Funds added successfully. New balance: {account.Balance:F2} {account.Currency}"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding funds to account");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Transfer money between any two accounts as an admin operation.
        /// </summary>
        [HttpPost("accounts/transfer")]
        public async Task<IActionResult> TransferBetweenAccounts([FromBody] AdminAccountTransferRequest request)
        {
            try
            {
                var userName = User.FindFirst("sub")?.Value ?? User.Identity?.Name;
                if (string.IsNullOrEmpty(userName) || userName != "admin")
                    return Forbid("Only admin can transfer between accounts");

                if (request.FromAccountId <= 0 || request.ToAccountId <= 0)
                    return BadRequest("Both source and destination accounts are required");

                if (request.FromAccountId == request.ToAccountId)
                    return BadRequest("Source and destination accounts must be different");

                if (request.Amount <= 0)
                    return BadRequest("Amount must be greater than zero");

                var fromAccount = await _context.Accounts.FirstOrDefaultAsync(a => a.Id == request.FromAccountId);
                var toAccount = await _context.Accounts.FirstOrDefaultAsync(a => a.Id == request.ToAccountId);

                if (fromAccount == null || toAccount == null)
                    return NotFound("Account not found");

                if (!string.Equals(fromAccount.Currency, toAccount.Currency, StringComparison.OrdinalIgnoreCase))
                    return BadRequest("Cannot transfer between accounts with different currencies");

                if (fromAccount.Balance < request.Amount)
                    return BadRequest("Insufficient funds");

                await using var transaction = await _context.Database.BeginTransactionAsync();

                var description = string.IsNullOrWhiteSpace(request.Description)
                    ? "Admin account transfer"
                    : request.Description.Trim();
                var reference = $"ADMTRF{DateTime.UtcNow.Ticks}";
                var now = DateTime.UtcNow;

                fromAccount.Balance -= request.Amount;
                toAccount.Balance += request.Amount;

                var debitMovement = new Movement
                {
                    AccountId = fromAccount.Id,
                    Amount = request.Amount,
                    Currency = fromAccount.Currency,
                    MovementType = MovementTypes.Transfer,
                    Status = "completed",
                    Description = $"Transfer to account {toAccount.Id}: {description}",
                    ReferenceNumber = $"{reference}D",
                    MovementDateTime = now
                };

                var creditMovement = new Movement
                {
                    AccountId = toAccount.Id,
                    Amount = request.Amount,
                    Currency = toAccount.Currency,
                    MovementType = MovementTypes.Transfer,
                    Status = "completed",
                    Description = $"Transfer from account {fromAccount.Id}: {description}",
                    ReferenceNumber = $"{reference}C",
                    MovementDateTime = now
                };

                _context.Movements.AddRange(debitMovement, creditMovement);
                _context.Accounts.UpdateRange(fromAccount, toAccount);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation(
                    "Admin {UserName} transferred {Amount} {Currency} from account {FromAccountId} to account {ToAccountId}",
                    userName,
                    request.Amount,
                    fromAccount.Currency,
                    fromAccount.Id,
                    toAccount.Id);

                return Ok(new
                {
                    success = true,
                    debitMovementId = debitMovement.MovementId,
                    creditMovementId = creditMovement.MovementId,
                    message = $"Transfer completed. Source balance: {fromAccount.Balance:F2} {fromAccount.Currency}"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error transferring between accounts");
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
                    .Where(m => m.MovementType == MovementTypes.Transfer &&
                                m.Status == "pending" &&
                                m.Description != null &&
                                m.Description.StartsWith("Transfer to account"))
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

                var selectedMovement = _context.Movements.FirstOrDefault(m => m.MovementId == movementId);
                if (selectedMovement == null)
                    return NotFound("Transfer not found");

                var debitMovement = IsTransferDebit(selectedMovement)
                    ? selectedMovement
                    : _context.Movements.FirstOrDefault(m =>
                        m.ReferenceNumber == selectedMovement.ReferenceNumber &&
                        m.MovementType == MovementTypes.Transfer &&
                        m.Description != null &&
                        m.Description.StartsWith("Transfer to account"));

                if (debitMovement == null)
                    return BadRequest("Transfer debit movement not found");

                if (debitMovement.Status != "pending")
                    return BadRequest("Transfer is not pending");

                // Get the corresponding credit movement by reference number
                var creditMovement = _context.Movements
                    .FirstOrDefault(m => m.ReferenceNumber == debitMovement.ReferenceNumber && 
                                       m.MovementId != debitMovement.MovementId &&
                                       m.MovementType == MovementTypes.Transfer &&
                                       m.Description != null &&
                                       m.Description.StartsWith("Transfer from account"));
                var toAccountId = creditMovement?.AccountId ?? GetTransferTargetAccountId(debitMovement);

                if (toAccountId == null)
                    return BadRequest("Transfer destination account not found");

                // Get accounts
                var fromAccount = _context.Accounts.FirstOrDefault(a => a.Id == debitMovement.AccountId);
                if (fromAccount == null)
                    return NotFound("From account not found");

                var toAccount = _context.Accounts.FirstOrDefault(a => a.Id == toAccountId.Value);
                if (toAccount == null)
                    return NotFound("To account not found");

                // Check balance
                if (fromAccount.Balance < debitMovement.Amount)
                    return BadRequest("Insufficient funds for approved transfer");

                // Update balances
                fromAccount.Balance -= debitMovement.Amount;
                toAccount.Balance += debitMovement.Amount;

                if (creditMovement != null)
                {
                    creditMovement.Status = "completed";
                    _context.Movements.Update(creditMovement);
                }
                else
                {
                    creditMovement = new Movement
                    {
                        AccountId = toAccount.Id,
                        Amount = debitMovement.Amount,
                        Currency = toAccount.Currency,
                        MovementType = MovementTypes.Transfer,
                        Status = "completed",
                        Description = $"Transfer from account {fromAccount.Id}: {GetTransferDescription(debitMovement)}",
                        ReferenceNumber = $"TRF{DateTime.UtcNow.Ticks}C",
                        MovementDateTime = DateTime.UtcNow
                    };
                    _context.Movements.Add(creditMovement);
                }

                // Update movements to completed
                debitMovement.Status = "completed";
                _context.Movements.Update(debitMovement);
                _context.Accounts.Update(fromAccount);
                _context.Accounts.Update(toAccount);

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

                var selectedMovement = _context.Movements.FirstOrDefault(m => m.MovementId == movementId);
                if (selectedMovement == null)
                    return NotFound("Transfer not found");

                var debitMovement = IsTransferDebit(selectedMovement)
                    ? selectedMovement
                    : _context.Movements.FirstOrDefault(m =>
                        m.ReferenceNumber == selectedMovement.ReferenceNumber &&
                        m.MovementType == MovementTypes.Transfer &&
                        m.Description != null &&
                        m.Description.StartsWith("Transfer to account"));

                if (debitMovement == null)
                    return BadRequest("Transfer debit movement not found");

                if (debitMovement.Status != "pending")
                    return BadRequest("Transfer is not pending");

                // Get the corresponding credit movement
                var creditMovement = _context.Movements
                    .FirstOrDefault(m => m.ReferenceNumber == debitMovement.ReferenceNumber && 
                                       m.MovementId != debitMovement.MovementId &&
                                       m.MovementType == MovementTypes.Transfer &&
                                       m.Description != null &&
                                       m.Description.StartsWith("Transfer from account"));

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

        private static bool IsTransferDebit(Movement movement) =>
            movement.MovementType == MovementTypes.Transfer &&
            (movement.Description?.StartsWith("Transfer to account", StringComparison.OrdinalIgnoreCase) ?? false);

        private static int? GetTransferTargetAccountId(Movement movement)
        {
            const string prefix = "Transfer to account ";
            var description = movement.Description ?? string.Empty;
            if (!description.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return null;

            var remainder = description[prefix.Length..];
            var separatorIndex = remainder.IndexOf(':', StringComparison.Ordinal);
            var accountIdText = separatorIndex >= 0
                ? remainder[..separatorIndex]
                : remainder;

            return int.TryParse(accountIdText.Trim(), out var accountId)
                ? accountId
                : null;
        }

        private static string GetTransferDescription(Movement movement)
        {
            var description = movement.Description ?? string.Empty;
            var separatorIndex = description.IndexOf(':', StringComparison.Ordinal);
            return separatorIndex >= 0
                ? description[(separatorIndex + 1)..].Trim()
                : "Transfer";
        }
    }
}
