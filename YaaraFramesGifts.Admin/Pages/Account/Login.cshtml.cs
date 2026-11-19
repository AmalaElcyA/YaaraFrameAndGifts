using System.ComponentModel.DataAnnotations;
using System.Net.Http.Json;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YaaraFramesGifts.DTOs;

namespace YaaraFramesGifts.Admin.Pages.Account;

public sealed class LoginModel(IHttpClientFactory httpClientFactory) : PageModel
{
    [BindProperty]
    [Required, StringLength(100, MinimumLength = 1)]
    public string Username { get; set; } = string.Empty;

    [BindProperty]
    [Required]
    public string Password { get; set; } = string.Empty;

    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var client = httpClientFactory.CreateClient("AdminApi");
        HttpResponseMessage loginResponse;
        try
        {
            loginResponse = await client.PostAsJsonAsync(
                "api/admin/auth/login",
                new AdminLoginRequest { Username = Username, Password = Password },
                cancellationToken);
        }
        catch (HttpRequestException)
        {
            ModelState.AddModelError(string.Empty, "The Admin authentication API could not be reached. Try again shortly.");
            return Page();
        }

        using (loginResponse)
        {
            if (!loginResponse.IsSuccessStatusCode)
            {
                ModelState.AddModelError(string.Empty, "Sign-in failed. Check your credentials and administrator access.");
                return Page();
            }

            var token = await loginResponse.Content.ReadFromJsonAsync<AdminLoginResponse>(cancellationToken);
            if (token is null || string.IsNullOrWhiteSpace(token.AccessToken) || token.ExpiresIn <= 60)
            {
                throw new InvalidOperationException("The Admin authentication API returned an invalid access token response.");
            }

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, token.User.Id.ToString()),
                new Claim(ClaimTypes.Name, token.User.Username),
                new Claim(ClaimTypes.GivenName, token.User.FullName),
                new Claim(ClaimTypes.Email, token.User.Email),
                new Claim(ClaimTypes.Role, "Admin"),
                new Claim("admin_api_token", token.AccessToken)
            };
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var properties = new AuthenticationProperties
            {
                IsPersistent = true,
                AllowRefresh = false,
                ExpiresUtc = DateTimeOffset.UtcNow.AddSeconds(
                    Math.Min(token.ExpiresIn - 60, TimeSpan.FromMinutes(55).TotalSeconds))
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity),
                properties);
            return LocalRedirect(Url.IsLocalUrl(ReturnUrl) ? ReturnUrl : "/");
        }
    }
}
