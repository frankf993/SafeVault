using Microsoft.AspNetCore.Identity;
using NUnit.Framework;
using SafeVault.Api.Models;
using SafeVault.Api.Services;

namespace SafeVault.Tests;

[TestFixture]
public class SafeVaultSecurityTests
{
    private static readonly PasswordHasher<User> Hasher = new();

    [TestCase("admin' OR '1'='1")]
    [TestCase("'; DROP TABLE Users; --")]
    [TestCase("user<script>alert(1)</script>")]
    public void Username_MaliciousInput_IsRejected(string username)
    {
        Assert.That(InputValidator.IsValidUsername(username), Is.False);
    }

    [TestCase("<script>alert('XSS')</script>")]
    [TestCase("\"><img src=x onerror=alert(1)>")]
    [TestCase("javascript:alert(1)")]
    public void Email_XssPayload_IsRejected(string email)
    {
        Assert.That(InputValidator.IsValidEmail(email), Is.False);
    }

    [Test]
    public void PasswordHash_DoesNotContainPlainTextPassword()
    {
        var user = new User { Username = "francesco" };
        var password = "SuperSecurePassword123!";

        var hash = Hasher.HashPassword(user, password);

        Assert.That(hash, Is.Not.EqualTo(password));
        Assert.That(hash, Does.Not.Contain(password));
    }

    [Test]
    public void PasswordVerification_ValidPassword_Succeeds()
    {
        var user = new User { Username = "francesco" };
        var password = "SuperSecurePassword123!";
        var hash = Hasher.HashPassword(user, password);

        var result = Hasher.VerifyHashedPassword(user, hash, password);

        Assert.That(result, Is.Not.EqualTo(PasswordVerificationResult.Failed));
    }

    [Test]
    public void PasswordVerification_WrongPassword_Fails()
    {
        var user = new User { Username = "francesco" };
        var hash = Hasher.HashPassword(user, "SuperSecurePassword123!");

        var result = Hasher.VerifyHashedPassword(user, hash, "WrongPassword123!");

        Assert.That(result, Is.EqualTo(PasswordVerificationResult.Failed));
    }
}
