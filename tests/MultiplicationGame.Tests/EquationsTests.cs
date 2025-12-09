using System.Globalization;
using Xunit;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Configuration;
using MultiplicationGame.Pages;
using System.Linq;
using System.Collections.Generic;

namespace MultiplicationGame.Tests;

public class EquationsTests
{
    private static IConfiguration GetTestConfiguration()
    {
        var configData = new Dictionary<string, string?>
        {
            {"GameSettings:EquationsMaxRepetitions", "0"}
        };
        return new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();
    }

    [Fact]
    public void Generate_YieldsTwentyEquations()
    {
        var model = new EquationsModel(NullLogger<EquationsModel>.Instance, GetTestConfiguration());
        model.Difficulty = "2";
        model.OnGet();

        Assert.Equal(20, model.Equations.Count);
    }

    [Fact]
    public void Submit_CorrectAnswers_YieldsFullScore()
    {
        var model = new EquationsModel(NullLogger<EquationsModel>.Instance, GetTestConfiguration());
        model.Difficulty = "3";
        model.OnGet();

        // prepare answers matching generated equations
        model.Answers = model.Equations.Select(e =>
            e.Answer.ToString(CultureInfo.InvariantCulture)).ToList();

        var result = model.OnPostSubmit();

        Assert.NotNull(model.Score);
        Assert.Equal(20, model.Score.Value);
    }

    [Fact]
    public void Submit_IncorrectAnswers_YieldsLowerScore()
    {
        var model = new EquationsModel(NullLogger<EquationsModel>.Instance, GetTestConfiguration());
        model.Difficulty = "2";
        model.OnGet();

        // submit all zeros (unlikely all correct)
        model.Answers = Enumerable.Repeat("0", 20).ToList();
        model.OnPostSubmit();

        Assert.NotNull(model.Score);
        Assert.InRange(model.Score.Value, 0, 19);
    }
}
