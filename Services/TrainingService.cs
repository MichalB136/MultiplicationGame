using MultiplicationGame.Models;

namespace MultiplicationGame.Services;

public interface ITrainingService
{
    List<TrainingQuestionDto> CreateQuestions(
        IEnumerable<int> tables,
        int questionCount);

    List<TrainingQuestionDto> CreateAdaptiveQuestions(
        IEnumerable<int> tables,
        int questionCount,
        IReadOnlyDictionary<string, TrainingFactProgress> progress);
}

public sealed class TrainingService : ITrainingService
{
    private const int MaximumTable = 10;

    public List<TrainingQuestionDto> CreateQuestions(
        IEnumerable<int> tables,
        int questionCount)
    {
        var questions = CreateCandidates(tables, questionCount);

        for (var i = questions.Count - 1; i > 0; i--)
        {
            var swapIndex = Random.Shared.Next(i + 1);
            (questions[i], questions[swapIndex]) = (questions[swapIndex], questions[i]);
        }

        return questions.Take(questionCount).ToList();
    }

    public List<TrainingQuestionDto> CreateAdaptiveQuestions(
        IEnumerable<int> tables,
        int questionCount,
        IReadOnlyDictionary<string, TrainingFactProgress> progress)
    {
        ArgumentNullException.ThrowIfNull(progress);

        var available = CreateCandidates(tables, questionCount);
        var selected = new List<TrainingQuestionDto>(questionCount);

        while (selected.Count < questionCount)
        {
            var weights = available
                .Select(question => GetQuestionWeight(question, progress))
                .ToArray();
            var totalWeight = weights.Sum();
            var roll = Random.Shared.NextDouble() * totalWeight;

            for (var i = 0; i < available.Count; i++)
            {
                roll -= weights[i];
                if (roll < 0)
                {
                    selected.Add(available[i]);
                    available.RemoveAt(i);
                    break;
                }
            }
        }

        return selected;
    }

    private static List<TrainingQuestionDto> CreateCandidates(
        IEnumerable<int> tables,
        int questionCount)
    {
        ArgumentNullException.ThrowIfNull(tables);

        if (questionCount <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(questionCount),
                "Question count must be greater than zero.");
        }

        var selectedTables = tables.Distinct().ToArray();
        if (selectedTables.Length == 0 || selectedTables.Any(table => table is < 1 or > MaximumTable))
        {
            throw new ArgumentException(
                "Select at least one multiplication table from 1 to 10.",
                nameof(tables));
        }

        var questions = selectedTables
            .SelectMany(table => Enumerable.Range(1, MaximumTable)
                .Select(multiplier => new TrainingQuestionDto(table, multiplier)))
            .ToList();

        if (questionCount > questions.Count)
        {
            throw new ArgumentOutOfRangeException(
                nameof(questionCount),
                "Question count cannot exceed the number of available questions.");
        }

        return questions;
    }

    private static double GetQuestionWeight(
        TrainingQuestionDto question,
        IReadOnlyDictionary<string, TrainingFactProgress> progress)
    {
        if (!progress.TryGetValue($"{question.A}-{question.B}", out var fact) ||
            fact.Attempts <= 0)
        {
            return 1;
        }

        var incorrectRate = (double)(fact.Attempts - fact.Correct) / fact.Attempts;
        var assistedRate = (double)fact.AssistedAnswers / fact.Attempts;
        return 1 + (incorrectRate * 4) + (assistedRate * 2);
    }
}
