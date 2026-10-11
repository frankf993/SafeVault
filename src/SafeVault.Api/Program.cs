using System.Text;
using Microsoft.AspNetCore.Authentication.Cookies;
using SafeVault.Api.Data;
using SafeVault.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

builder.Services.AddCors(options =>
{
    options.AddPolicy("SafeVaultWeb", policy =>
        policy.WithOrigins("https://localhost:5001")
              .AllowCredentials()
              .AllowAnyHeader()
              .AllowAnyMethod());
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
});

builder.Services.AddScoped<UserRepository>();
builder.Services.AddScoped<AuthService>();

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
        options.SlidingExpiration = true;

        options.Cookie.Name = "SafeVault.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.None;
    });

var app = builder.Build();

await SafeVaultDatabase.InitializeAsync(
    app.Configuration.GetConnectionString("SafeVault")!);

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors("SafeVaultWeb");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
