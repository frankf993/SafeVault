using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SafeVault.Web.Services;

namespace SafeVault.Web.Pages.Admin;

public class IndexModel : PageModel
{
    private readonly SafeVaultApiClient _api;

    public IndexModel(SafeVaultApiClient api)
    {
        _api = api;
    }

    public string Message { get; private set; } = string.Empty;
    public bool IsSuccess { get; private set; }

    public async Task OnGetAsync()
    {
        var result = await _api.GetAdminDashboardAsync();

        Message = result.Message;
        IsSuccess = result.Success;
    }
}
