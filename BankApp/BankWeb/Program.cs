using BankWeb.Components;
using BankWeb.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddHttpClient("BankApi", client =>
{
    client.BaseAddress = new Uri("https://localhost:7083/");
});

builder.Services.AddHttpClient("BankApiAnonymous", client =>
{
    client.BaseAddress = new Uri("https://localhost:7083/");
});

builder.Services.AddScoped<ApiAuthService>();
builder.Services.AddScoped<AccountApiService>();
builder.Services.AddScoped<MovementApiService>();
builder.Services.AddScoped<TransferApiService>();
builder.Services.AddScoped<AdminApiService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
