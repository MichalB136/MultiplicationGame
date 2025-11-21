using System.Linq;
using System.Collections.Generic;
using Xunit;
using Microsoft.Extensions.Options;
using MultiplicationGame.Services;

namespace MultiplicationGame.Tests;

public class GameServiceTests
{
    [Fact]
    public void GetQuestion_ProductWithinLevel()
    {
        var settings = new GameSettings { Levels = new[] { 100 }, DefaultMaxMultiplier = 10 };
        var svc = new GameService(Options.Create(settings), Microsoft.Extensions.Logging.Abstractions.NullLogger<GameService>.Instance);

        var q = svc.GetQuestion(100, string.Empty);

        Assert.True(q.A >= 1 && q.A <= settings.DefaultMaxMultiplier, "A out of range");
        Assert.True(q.B >= 1 && q.B <= settings.DefaultMaxMultiplier, "B out of range");
        Assert.True(q.A * q.B <= 100, "Product exceeds level");
    }

    [Fact]
    public void GetQuestion_ReducesLowFactorFrequency()
    {
        var settings = new GameSettings
        {
            Levels = new[] { 100 },
            DefaultMaxMultiplier = 10,
            LowProbabilityFactors = new[] { 1, 2, 3, 4, 10 },
            LowFactorChancePercent = 10
        };

        var svc = new GameService(Options.Create(settings), Microsoft.Extensions.Logging.Abstractions.NullLogger<GameService>.Instance);

        const int runs = 3000;
        var lowSet = new HashSet<int>(settings.LowProbabilityFactors);
        int lowCount = 0;

        for (int i = 0; i < runs; i++)
        {
            var q = svc.GetQuestion(100, string.Empty);
            if (lowSet.Contains(q.A) || lowSet.Contains(q.B)) lowCount++;
        }

        var fraction = (double)lowCount / runs;
        // Expect low-factor fraction to be significantly under 50% when reduced (tolerance)
        Assert.True(fraction < 0.40, $"Low-factor fraction too high: {fraction}");
    }
}
