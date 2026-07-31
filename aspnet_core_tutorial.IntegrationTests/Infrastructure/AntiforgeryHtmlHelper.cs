using System.Text.RegularExpressions;

namespace aspnet_core_tutorial.IntegrationTests.Infrastructure;

/// <summary>
/// Extracts the hidden `__RequestVerificationToken` field value from a rendered Razor form, so
/// integration tests can submit real POST requests against actions protected by
/// `[ValidateAntiForgeryToken]` without bypassing the check. The matching antiforgery cookie is
/// carried automatically by the test `HttpClient`'s cookie container across the GET/POST pair,
/// as long as the same client instance issues both requests.
/// </summary>
/// <remarks>
/// Created by edgar.muhamyangabo on 7/13/26
/// Author : edgar.muhamyangabo
/// Date : 7/13/26
/// Project : aspnet_core_tutorial
/// </remarks>
public static class AntiforgeryHtmlHelper
{
    private static readonly Regex InputTagRegex =
        new("<input\\b[^>]*name=\"__RequestVerificationToken\"[^>]*>", RegexOptions.IgnoreCase);

    private static readonly Regex ValueAttributeRegex =
        new("value=\"([^\"]*)\"", RegexOptions.IgnoreCase);

    public static string ExtractAntiforgeryToken(string html)
    {
        var inputMatch = InputTagRegex.Match(html);
        if (!inputMatch.Success)
        {
            throw new InvalidOperationException(
                "Could not find a __RequestVerificationToken hidden field in the rendered HTML.");
        }

        var valueMatch = ValueAttributeRegex.Match(inputMatch.Value);
        if (!valueMatch.Success)
        {
            throw new InvalidOperationException(
                "Found the __RequestVerificationToken input but could not extract its value attribute.");
        }

        return valueMatch.Groups[1].Value;
    }
}
