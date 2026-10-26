using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MultiplicationGame.Pages;
using MultiplicationGame.Services;
using Xunit;

namespace MultiplicationGame.Tests;

public class PlayerGameSettingsTests
{
    [Fact]
    public void OnGet_LoadsSettingsSavedInBrowserCookie()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers.Cookie = "multiplication-game-settings=2-4-8";
        var model = CreateModel(httpContext);

        model.OnGet();

        Assert.Equal(2, model.InitialAttempts);
        Assert.Equal(4, model.BonusAttemptsThreshold);
        Assert.Equal(8, model.RequiredAnswers);
    }

    [Fact]
    public void OnPost_ApplySettings_SavesSettingsAndStartsNewGame()
    {
        var httpContext = new DefaultHttpContext();
        var model = CreateModel(httpContext);
        model.ApplySettings = true;
        model.InitialAttempts = 4;
        model.BonusAttemptsThreshold = 2;
        model.RequiredAnswers = 7;

        model.OnPost();

        Assert.Equal(4, model.AttemptsLeft);
        Assert.Equal(4, model.InitialAttempts);
        Assert.Equal(2, model.BonusAttemptsThreshold);
        Assert.Equal(7, model.RequiredAnswers);
        Assert.Contains(
            "multiplication-game-settings=4-2-7",
            httpContext.Response.Headers.SetCookie.ToString());
    }

    [Fact]
    public void OnPost_UsesPlayerBonusThresholdAndQuestionCount()
    {
        var model = CreateModel(new DefaultHttpContext());
        model.InitialAttempts = 3;
        model.BonusAttemptsThreshold = 1;
        model.RequiredAnswers = 1;
        model.AttemptsLeft = 3;
        model.A = 2;
        model.B = 3;
        model.UserAnswerText = "6";

        model.OnPost();

        Assert.True(model.BonusAwarded);
        Assert.Equal(4, model.AttemptsLeft);
        Assert.True(model.GameWon);
    }

    [Fact]
    public void OnPost_RejectsSettingsOutsideSupportedRanges()
    {
        var httpContext = new DefaultHttpContext();
        var model = CreateModel(httpContext);
        model.ApplySettings = true;
        model.InitialAttempts = 11;
        model.BonusAttemptsThreshold = 5;
        model.RequiredAnswers = 10;

        model.OnPost();

        Assert.False(model.ModelState.IsValid);
        Assert.False(httpContext.Response.Headers.ContainsKey("Set-Cookie"));
        Assert.NotNull(model.Question);
    }

    private static IndexModel CreateModel(HttpContext httpContext)
    {
        var settings = new GameSettings
        {
            Levels = [100],
            DefaultMaxMultiplier = 10,
            InitialAttempts = 3,
            BonusAttemptsThreshold = 5,
            RequiredCorrectAnswers = 20
        };

        return new IndexModel(
            new GameService(
                Options.Create(settings),
                NullLogger<GameService>.Instance),
            new GameStateService(NullLogger<GameStateService>.Instance),
            NullLogger<IndexModel>.Instance,
            Options.Create(settings))
        {
            PageContext = new PageContext { HttpContext = httpContext }
        };
    }
}
