using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SafeVault.Web.Services;

namespace SafeVault.Web.Pages;

public class DashboardModel : PageModel
{
    private readonly SafeVaultApiClient _api;

    public DashboardModel(SafeVaultApiClient api)
    {
        _api = api;
    }

    public SafeVaultApiClient.MeResponse? Profile { get; private set; }

    public async Task OnGetAsync()
    {
        Profile = await _api.GetMeAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await _api.LogoutAsync();
        return RedirectToPage("/Index");
    }
}
