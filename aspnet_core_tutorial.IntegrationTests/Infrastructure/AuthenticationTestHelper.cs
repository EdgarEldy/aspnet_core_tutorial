using System.Net;

namespace aspnet_core_tutorial.IntegrationTests.Infrastructure;

/// <summary>
/// Drives the real Identity UI Razor Pages (Register/Login) over HTTP, including their own
/// `[ValidateAntiForgeryToken]` protection, so integration tests can obtain an authenticated
/// <see cref="HttpClient"/> the same way a real browser would rather than forging a cookie
/// directly.
/// </summary>
/// <remarks>
/// Created by edgar.muhamyangabo on 7/31/26
/// Author : edgar.muhamyangabo
/// Date : 7/31/26
/// Project : aspnet_core_tutorial
/// </remarks>
public static class AuthenticationTestHelper
{
    // Satisfies the password policy configured in Program.cs (digit, lowercase, uppercase,
    // non-alphanumeric, length >= 6, at least 1 unique char).
    private const string DefaultPassword = "Test#User1Pass";

    /// <summary>
    /// Logs in as the Admin account seeded at test-host startup (see
    /// <see cref="IntegrationTestWebApplicationFactory.SeededAdminEmail"/>) via a real POST to
    /// the Login page, returning an <see cref="HttpClient"/> whose cookie container now carries
    /// a valid authentication cookie for that Admin account.
    /// </summary>
    public static async Task<HttpClient> CreateAdminAuthenticatedClientAsync(this IntegrationTestWebApplicationFactory factory)
    {
        var client = factory.CreateTestClient();
        await client.LoginAsync(IntegrationTestWebApplicationFactory.SeededAdminEmail, IntegrationTestWebApplicationFactory.SeededAdminPassword);
        return client;
    }

    /// <summary>
    /// Registers a brand-new account via a real POST to the Register page, which (per
    /// <c>RegisterModel.OnPostAsync</c>) both assigns it the baseline "User" role and signs it
    /// in immediately, returning an authenticated <see cref="HttpClient"/> plus the email used so
    /// callers can assert on the account afterward (e.g. its role membership).
    /// </summary>
    public static async Task<(HttpClient Client, string Email)> CreateUserAuthenticatedClientAsync(this IntegrationTestWebApplicationFactory factory)
    {
        var client = factory.CreateTestClient();
        var email = $"user-{Guid.NewGuid():N}@example.com";
        await client.RegisterAsync(email, DefaultPassword);
        return (client, email);
    }

    public static async Task RegisterAsync(this HttpClient client, string email, string password)
    {
        var getResponse = await client.GetAsync("/Identity/Account/Register");
        var getBody = await getResponse.Content.ReadAsStringAsync();
        var token = AntiforgeryHtmlHelper.ExtractAntiforgeryToken(getBody);

        var response = await client.PostAsync("/Identity/Account/Register", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Input.Email"] = email,
            ["Input.Password"] = password,
            ["Input.ConfirmPassword"] = password,
            ["__RequestVerificationToken"] = token
        }));

        if (response.StatusCode != HttpStatusCode.Redirect)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException(
                $"Registration for '{email}' did not redirect as expected (status {response.StatusCode}). Body: {body}");
        }
    }

    private static async Task LoginAsync(this HttpClient client, string email, string password)
    {
        var getResponse = await client.GetAsync("/Identity/Account/Login");
        var getBody = await getResponse.Content.ReadAsStringAsync();
        var token = AntiforgeryHtmlHelper.ExtractAntiforgeryToken(getBody);

        var response = await client.PostAsync("/Identity/Account/Login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Input.Email"] = email,
            ["Input.Password"] = password,
            ["__RequestVerificationToken"] = token
        }));

        if (response.StatusCode != HttpStatusCode.Redirect)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException(
                $"Login for '{email}' did not redirect as expected (status {response.StatusCode}). Body: {body}");
        }
    }
}
