using Xunit;
using MultiplicationGame.Services;
using System.Collections.Immutable;
using System.Linq;

namespace MultiplicationGame.Tests;

public class GameStateTests
{
    [Fact]
    public void UpdateSolvedQuestions_AddsUniqueKey()
    {
        var svc = new GameStateService(Microsoft.Extensions.Logging.Abstractions.NullLogger<GameStateService>.Instance);
        var s1 = svc.UpdateSolvedQuestions(null, 3, 4);
        Assert.Equal("3-4", s1);

        var s2 = svc.UpdateSolvedQuestions(s1, 3, 4);
        Assert.Equal("3-4", s2); // still only one

        var s3 = svc.UpdateSolvedQuestions(s2, 2, 5);
        Assert.Contains("3-4", s3);
        Assert.Contains("2-5", s3);
    }

    [Fact]
    public void SerializeAndParseHistory_Works()
    {
        var svc = new GameStateService(Microsoft.Extensions.Logging.Abstractions.NullLogger<GameStateService>.Instance);
        var list = ImmutableList.Create("a","b","c");
        var raw = svc.SerializeHistory(list);
        var parsed = svc.ParseHistory(raw);
        Assert.Equal(list, parsed);
    }
}
