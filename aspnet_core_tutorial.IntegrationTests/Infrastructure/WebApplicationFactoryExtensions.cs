using Microsoft.AspNetCore.Mvc.Testing;

namespace aspnet_core_tutorial.IntegrationTests.Infrastructure;

/// <summary>
/// Convenience extension to build the <see cref="HttpClient"/> used across integration tests
/// with a consistent, sane configuration.
/// </summary>
/// <remarks>
/// Created by edgar.muhamyangabo on 7/13/26
/// Author : edgar.muhamyangabo
/// Date : 7/13/26
/// Project : aspnet_core_tutorial
/// </remarks>
public static class WebApplicationFactoryExtensions
{
    public static HttpClient CreateTestClient(this IntegrationTestWebApplicationFactory factory)
    {
        // BaseAddress uses https so app.UseHttpsRedirection() in the pipeline sees an
        // already-HTTPS request and does not issue a 307 redirect (TestServer has no real TLS
        // listener to redirect to). AllowAutoRedirect is disabled so tests can assert on 302
        // responses (e.g. successful Create/Edit/Delete redirects, anonymous-access redirects to
        // the login page) directly instead of transparently following them.
        return factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false
        });
    }
}
