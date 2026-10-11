using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using SafeVault.Api.Models;

namespace SafeVault.Api.Services;

public record RegisterRequest(string Username, string Email, string Password);
public record LoginRequest(string Username, string Password);
public record AuthResult(bool Success, string Error, string? Role = null)
{
    public static AuthResult Ok(string? role = null) => new(true, string.Empty, role);
    public static AuthResult Fail(string error) => new(false, error);
}

public class AuthService
{
    private readonly UserRepository _repository;
    private readonly PasswordHasher<User> _passwordHasher = new();

    public AuthService(UserRepository repository)
    {
        _repository = repository;
    }

    public async Task<AuthResult> RegisterAsync(RegisterRequest request)
    {
        if (!InputValidator.IsValidUsername(request.Username))
            return AuthResult.Fail("Username non valido.");

        if (!InputValidator.IsValidEmail(request.Email))
            return AuthResult.Fail("Email non valida.");

        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 12)
            return AuthResult.Fail("La password deve contenere almeno 12 caratteri.");

        var username = InputValidator.NormalizeUsername(request.Username);

        if (await _repository.ExistsAsync(username))
            return AuthResult.Fail("Username già registrato.");

        var user = new User
        {
            Username = username,
            Email = request.Email.Trim(),
            Role = "User"
        };

        user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);

        await _repository.CreateAsync(user);

        return AuthResult.Ok(user.Role);
    }

    public async Task<User?> LoginAsync(LoginRequest request, HttpContext httpContext)
    {
        if (!InputValidator.IsValidUsername(request.Username))
            return null;

        var username = InputValidator.NormalizeUsername(request.Username);
        var user = await _repository.GetByUsernameAsync(username);

        if (user is null)
            return null;

        var verification = _passwordHasher.VerifyHashedPassword(
            user,
            user.PasswordHash,
            request.Password);

        if (verification == PasswordVerificationResult.Failed)
            return null;

        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Role, user.Role)
        };

        var identity = new ClaimsIdentity(
            claims,
            CookieAuthenticationDefaults.AuthenticationScheme);

        await httpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity));

        return user;
    }

    public async Task LogoutAsync(HttpContext httpContext)
    {
        await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    }
}
