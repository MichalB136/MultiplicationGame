using System;
using System.Collections.Generic;
using System.Linq;
using MultiplicationGame.Models;
using MultiplicationGame.Services;
using Xunit;

namespace MultiplicationGame.Tests;

public class TrainingServiceTests
{
    private readonly TrainingService _service = new();

    [Fact]
    public void CreateQuestions_ForSingleTable_ReturnsTenUniqueQuestionsFromThatTable()
    {
        var questions = _service.CreateQuestions([7], 10);

        Assert.Equal(10, questions.Count);
        Assert.All(questions, question => Assert.Equal(7, question.A));
        Assert.Equal(10, questions.Select(question => question.B).Distinct().Count());
    }

    [Fact]
    public void CreateQuestions_ForMultipleTables_UsesOnlySelectedTables()
    {
        int[] selectedTables = [3, 8];

        var questions = _service.CreateQuestions(selectedTables, 10);

        Assert.Equal(10, questions.Count);
        Assert.All(
            questions,
            question => Assert.Contains(question.A, selectedTables));
        Assert.Equal(
            questions.Count,
            questions.Select(question => (question.A, question.B)).Distinct().Count());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(11)]
    public void CreateQuestions_WithInvalidTable_ThrowsArgumentException(int table)
    {
        Assert.Throws<ArgumentException>(
            () => _service.CreateQuestions([table], 10));
    }

    [Fact]
    public void CreateAdaptiveQuestions_PrioritizesPreviouslyIncorrectFacts()
    {
        var progress = new Dictionary<string, TrainingFactProgress>
        {
            ["3-1"] = new(1, 0, 0)
        };
        var includedCount = 0;

        for (var i = 0; i < 100; i++)
        {
            var questions = _service.CreateAdaptiveQuestions([3, 8], 10, progress);
            if (questions.Contains(new TrainingQuestionDto(3, 1)))
            {
                includedCount++;
            }
        }

        Assert.True(
            includedCount > 80,
            $"Expected the difficult fact to appear frequently, but got {includedCount} rounds.");
    }
}
