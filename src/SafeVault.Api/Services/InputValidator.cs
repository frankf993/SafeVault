using System.Text.RegularExpressions;

namespace SafeVault.Api.Services;

public static partial class InputValidator
{
    [GeneratedRegex(@"^[a-zA-Z0-9_.-]{3,50}$")]
    private static partial Regex UsernameRegex();

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]{2,}$")]
    private static partial Regex EmailRegex();

    public static bool IsValidUsername(string? value) =>
        !string.IsNullOrWhiteSpace(value) && UsernameRegex().IsMatch(value);

    public static bool IsValidEmail(string? value) =>
        !string.IsNullOrWhiteSpace(value) &&
        value.Length <= 100 &&
        EmailRegex().IsMatch(value);

    public static string NormalizeUsername(string value) =>
        value.Trim().ToLowerInvariant();
}
