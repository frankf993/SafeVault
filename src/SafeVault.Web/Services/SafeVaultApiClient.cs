using System.Net.Http.Json;

namespace SafeVault.Web.Services;

public record RegisterInput(string Username, string Email, string Password);
public record LoginInput(string Username, string Password);

public record ApiResult(bool Success, string Message, string? Role = null)
{
    public static ApiResult Ok(string message, string? role = null) => new(true, message, role);
    public static ApiResult Fail(string message) => new(false, message);
}

public class SafeVaultApiClient
{
    private readonly HttpClient _http;

    public SafeVaultApiClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<ApiResult> RegisterAsync(RegisterInput input)
    {
        var response = await _http.PostAsJsonAsync("api/auth/register", input);

        if (response.IsSuccessStatusCode)
            return ApiResult.Ok("Registrazione completata.");

        var error = await ReadErrorAsync(response);
        return ApiResult.Fail(error);
    }

    public async Task<ApiResult> LoginAsync(LoginInput input)
    {
        var response = await _http.PostAsJsonAsync("api/auth/login", input);

        if (!response.IsSuccessStatusCode)
            return ApiResult.Fail(await ReadErrorAsync(response));

        var data = await response.Content.ReadFromJsonAsync<LoginResponse>();
        return ApiResult.Ok("Accesso effettuato.", data?.Role);
    }

    public async Task<ApiResult> LogoutAsync()
    {
        var response = await _http.PostAsync("api/auth/logout", null);

        return response.IsSuccessStatusCode
            ? ApiResult.Ok("Logout effettuato.")
            : ApiResult.Fail("Logout non riuscito.");
    }

    public async Task<MeResponse?> GetMeAsync()
    {
        return await _http.GetFromJsonAsync<MeResponse>("api/auth/me");
    }

    public async Task<ApiResult> GetAdminDashboardAsync()
    {
        var response = await _http.GetAsync("api/admin/dashboard");

        if (response.IsSuccessStatusCode)
            return ApiResult.Ok("Accesso all’area amministrativa consentito.");

        return ApiResult.Fail("Accesso negato all’area amministrativa.");
    }

    private static async Task<string> ReadErrorAsync(HttpResponseMessage response)
    {
        try
        {
            var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
            return error?.Error ?? "Si è verificato un errore.";
        }
        catch
        {
            return "Si è verificato un errore.";
        }
    }

    private record ErrorResponse(string Error);
    private record LoginResponse(string Message, string Username, string Role);
    public record MeResponse(string Username, string Role);
}
