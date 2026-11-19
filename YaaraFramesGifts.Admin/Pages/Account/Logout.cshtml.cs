using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net;
using System.Net.Http.Headers;

namespace YaaraFramesGifts.Admin.Pages.Account;

public sealed class LogoutModel(IHttpClientFactory httpClientFactory) : PageModel
{
    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        var token = User.FindFirst("admin_api_token")?.Value;
        try
        {
            if (!string.IsNullOrWhiteSpace(token))
            {
                var client = httpClientFactory.CreateClient("AdminApi");
                using var request = new HttpRequestMessage(HttpMethod.Post, "api/admin/auth/logout");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                using var response = await client.SendAsync(request, cancellationToken);
                if (!response.IsSuccessStatusCode && response.StatusCode != HttpStatusCode.Unauthorized)
                {
                    throw new InvalidOperationException(
                        $"The Admin logout API failed with HTTP {(int)response.StatusCode}.");
                }
            }
        }
        finally
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        }

        return Redirect("/Account/Login");
    }
}
