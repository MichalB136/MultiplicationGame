using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace MultiplicationGame.Tests;

public class PlayerSettingsIntegrationTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task SettingsPost_PersistsBrowserPreferencesAcrossRequests()
    {
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            HandleCookies = true
        });

        using var initialResponse = await client.GetAsync("/");
        Assert.Equal(HttpStatusCode.OK, initialResponse.StatusCode);
        var initialHtml = await initialResponse.Content.ReadAsStringAsync();
        var antiforgeryToken = ReadInputValue(initialHtml, "__RequestVerificationToken");

        using var settingsContent = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = antiforgeryToken,
            ["ApplySettings"] = "true",
            ["Level"] = "100",
            ["InitialAttempts"] = "4",
            ["BonusAttemptsThreshold"] = "2",
            ["RequiredAnswers"] = "7"
        });

        using var settingsResponse = await client.PostAsync("/", settingsContent);
        Assert.Equal(HttpStatusCode.OK, settingsResponse.StatusCode);
        Assert.Contains(
            settingsResponse.Headers.GetValues("Set-Cookie"),
            cookie => cookie.StartsWith("multiplication-game-settings=4-2-7", StringComparison.Ordinal));

        var updatedHtml = await settingsResponse.Content.ReadAsStringAsync();
        AssertInputValue(updatedHtml, "InitialAttempts", "4");
        AssertInputValue(updatedHtml, "BonusAttemptsThreshold", "2");
        AssertInputValue(updatedHtml, "RequiredAnswers", "7");

        using var reloadedResponse = await client.GetAsync("/");
        Assert.Equal(HttpStatusCode.OK, reloadedResponse.StatusCode);
        var reloadedHtml = await reloadedResponse.Content.ReadAsStringAsync();
        AssertInputValue(reloadedHtml, "InitialAttempts", "4");
        AssertInputValue(reloadedHtml, "BonusAttemptsThreshold", "2");
        AssertInputValue(reloadedHtml, "RequiredAnswers", "7");
    }

    private static void AssertInputValue(string html, string name, string expectedValue)
    {
        Assert.Equal(expectedValue, ReadInputValue(html, name));
    }

    private static string ReadInputValue(string html, string name)
    {
        var pattern = $@"<input\b(?=[^>]*\bname=""{Regex.Escape(name)}"")(?=[^>]*\bvalue=""(?<value>[^""]*)"")[^>]*>";
        var match = Regex.Match(html, pattern, RegexOptions.CultureInvariant);

        Assert.True(match.Success, $"Input '{name}' was not rendered.");
        return WebUtility.HtmlDecode(match.Groups["value"].Value);
    }
}
