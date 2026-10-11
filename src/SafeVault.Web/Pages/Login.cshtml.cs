using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SafeVault.Web.Services;

namespace SafeVault.Web.Pages;

public class LoginModel : PageModel
{
    private readonly SafeVaultApiClient _api;

    public LoginModel(SafeVaultApiClient api)
    {
        _api = api;
    }

    [BindProperty]
    public LoginInput Input { get; set; } = new("", "");

    public string? ErrorMessage { get; private set; }
    public string? SuccessMessage { get; private set; }

    public async Task<IActionResult> OnPostAsync()
    {
        var result = await _api.LoginAsync(Input);

        if (!result.Success)
        {
            ErrorMessage = result.Message;
            return Page();
        }

        SuccessMessage = result.Message;

        return result.Role == "Admin"
            ? RedirectToPage("/Admin/Index")
            : RedirectToPage("/Dashboard");
    }
}
