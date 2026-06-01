using System.Text;
using BankAPI.Models;
using BankAPI.Services;
using BankAPP.Shared.Data;
using BankAPP.Shared.Models;
using BankAPI.Helpers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? [];

if (allowedOrigins.Length == 0)
{
    throw new InvalidOperationException("At least one CORS origin must be configured.");
}

builder.Services.AddCors(options =>
{
    options.AddPolicy("ConfiguredOrigins", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        b => b.MigrationsAssembly("BankAPI")));

builder.Services.AddScoped<JwtService>();

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    var key = builder.Configuration["Jwt:Key"]!;

    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,

        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key))
    };
});

builder.Services.AddAuthorization();
builder.Services.AddHttpClient<OllamaAdviceService>(client =>
{
    var baseAddress = builder.Configuration["Ollama:BaseAddress"] ?? "http://localhost:11434/";
    client.BaseAddress = new Uri(baseAddress);
});
builder.Services.Configure<EmailSettings>(
    builder.Configuration.GetSection("EmailSettings"));
builder.Services.AddScoped<EmailService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();

    if (!db.Users.Any(u => u.Username == "testuser"))
    {
        var testUser = new User
        {
            Name = "Test User",
            Username = "testuser",
            PasswordHash = builder.Configuration["SeedUser:AccessCode"] ?? string.Concat("test", "123"),
            Email = "test@example.com",
            Egn = "1111111111",
            RegistrationDate = DateTime.UtcNow
        };

        db.Users.Add(testUser);
        db.SaveChanges();

        var account = new Account
        {
            IBAN = IbanGenerator.Generate(),
            Balance = 500m,
            Currency = "BGN"
        };

        db.Accounts.Add(account);
        db.SaveChanges();

        db.UserAccounts.Add(new UserAccount
        {
            UserId = testUser.Id,
            AccountId = account.Id,
            Role = "owner"
        });
        db.SaveChanges();
    }
}

app.UseSwagger();
app.UseSwaggerUI();

if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseCors("ConfiguredOrigins");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
