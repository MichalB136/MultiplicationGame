using MultiplicationGame.Models;
using System.Collections.Immutable;

namespace MultiplicationGame.Services;

public interface IGameService
{
    QuestionDto GetQuestion(int level, string solvedQuestions);
    AnswerResultDto CheckAnswer(int userAnswer, int a, int b);
}

public sealed class GameService : IGameService
{
    private static readonly int[] Levels = { 20, 40, 60, 80, 100, 1000 };
    private readonly Random _random = new();

    public QuestionDto GetQuestion(int level, string solvedQuestions)
    {
        if (!Levels.Contains(level))
            return new QuestionDto(0, 0, level);

        var solved = ParseSolvedQuestions(solvedQuestions);
        var allQuestions = GenerateAllQuestions(level);
        var available = FilterAvailableQuestions(allQuestions, solved);
        
        if (available.Count == 0)
            return new QuestionDto(0, 0, level);

        var selected = SelectQuestion(available);
        return new QuestionDto(selected.a, selected.b, level);
    }

    private static HashSet<string> ParseSolvedQuestions(string solvedQuestions) =>
        new((solvedQuestions ?? "").Split(';', StringSplitOptions.RemoveEmptyEntries));

    private static List<(int a, int b)> GenerateAllQuestions(int level)
    {
        var maxNum = level == 1000 ? 1000 : 10;
        var allQuestions = new List<(int a, int b)>();
        
        for (var i = 1; i <= maxNum; i++)
        {
            for (var j = 1; j <= maxNum; j++)
            {
                if (i * j <= level)
                    allQuestions.Add((i, j));
            }
        }
        
        return allQuestions;
    }

    private static List<(int a, int b)> FilterAvailableQuestions(
        List<(int a, int b)> allQuestions, 
        HashSet<string> solved) =>
        allQuestions.Where(q => !solved.Contains($"{q.a}-{q.b}")).ToList();

    private (int a, int b) SelectQuestion(List<(int a, int b)> available)
    {
        var withOne = available.Where(q => q.a == 1 || q.b == 1).ToList();
        var withoutOne = available.Where(q => q.a != 1 && q.b != 1).ToList();
        
        if (withOne.Count == 0 || withoutOne.Count == 0)
            return available[_random.Next(available.Count)];
            
        var roll = _random.Next(100);
        return roll < 20 
            ? withOne[_random.Next(withOne.Count)]
            : withoutOne[_random.Next(withoutOne.Count)];
    }

    public AnswerResultDto CheckAnswer(int userAnswer, int a, int b)
    {
        var correctAnswer = a * b;
        var isCorrect = userAnswer == correctAnswer;
        return new AnswerResultDto(isCorrect, correctAnswer);
    }
}

public interface IGameStateService
{
    ImmutableList<string> ParseHistory(string historyRaw);
    ImmutableList<HistoryEntryDto> ParseHistoryWithCorrectness(string historyRaw);
    string SerializeHistory(ImmutableList<string> history);
    string UpdateSolvedQuestions(string currentSolved, int a, int b);
    string CreateHistoryEntry(int a, int b, int correctAnswer, int userAnswer);
}

public sealed class GameStateService : IGameStateService
{
    public ImmutableList<string> ParseHistory(string historyRaw) =>
        string.IsNullOrEmpty(historyRaw)
            ? ImmutableList<string>.Empty
            : historyRaw.Split("||").ToImmutableList();

    public ImmutableList<HistoryEntryDto> ParseHistoryWithCorrectness(string historyRaw)
    {
        var historyItems = ParseHistory(historyRaw);
        return historyItems.Select(ParseHistoryEntry).ToImmutableList();
    }

    public string SerializeHistory(ImmutableList<string> history) =>
        string.Join("||", history);

    public string UpdateSolvedQuestions(string currentSolved, int a, int b)
    {
        var solved = (currentSolved ?? "").Split(';', StringSplitOptions.RemoveEmptyEntries).ToList();
        var key = $"{a}-{b}";
        if (!solved.Contains(key))
            solved.Add(key);
        return string.Join(";", solved);
    }

    public string CreateHistoryEntry(int a, int b, int correctAnswer, int userAnswer) =>
        $"{a} × {b} = {correctAnswer} (Twoja: {userAnswer})";

    private static HistoryEntryDto ParseHistoryEntry(string historyString)
    {
        if (!historyString.Contains("Twoja: ") || !historyString.Contains('='))
            return new HistoryEntryDto(historyString, false);

        try
        {
            var userAnswerPart = historyString.Split("Twoja: ")[1].Split(')')[0].Trim();
            var correctAnswerPart = historyString.Split('=')[1].Split('(')[0].Trim();
            var isCorrect = userAnswerPart == correctAnswerPart;
            
            return new HistoryEntryDto(historyString, isCorrect);
        }
        catch
        {
            return new HistoryEntryDto(historyString, false);
        }
    }
}