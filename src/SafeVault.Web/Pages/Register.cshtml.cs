using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SafeVault.Web.Services;

namespace SafeVault.Web.Pages;

public class RegisterModel : PageModel
{
    private readonly SafeVaultApiClient _api;

    public RegisterModel(SafeVaultApiClient api)
    {
        _api = api;
    }

    [BindProperty]
    public RegisterInput Input { get; set; } = new("", "", "");

    public string? ErrorMessage { get; private set; }
    public string? SuccessMessage { get; private set; }

    public async Task<IActionResult> OnPostAsync()
    {
        var result = await _api.RegisterAsync(Input);

        if (!result.Success)
        {
            ErrorMessage = result.Message;
            return Page();
        }

        SuccessMessage = result.Message;
        return Page();
    }
}
