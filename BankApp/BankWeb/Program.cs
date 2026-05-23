using BankWeb.Components;
using BankWeb.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var bankApiBaseAddress = builder.Configuration["BankApi:BaseAddress"] ?? "http://localhost:5000/";

builder.Services.AddHttpClient("BankApi", client =>
{
    client.BaseAddress = new Uri(bankApiBaseAddress);
});

builder.Services.AddHttpClient("BankApiAnonymous", client =>
{
    client.BaseAddress = new Uri(bankApiBaseAddress);
});

builder.Services.AddScoped<ApiAuthService>();
builder.Services.AddScoped<AccountApiService>();
builder.Services.AddScoped<MovementApiService>();
builder.Services.AddScoped<TransferApiService>();
builder.Services.AddScoped<AdminApiService>();
builder.Services.AddScoped<AssistantApiService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}
else
{
    app.UseHttpsRedirection();
}

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
