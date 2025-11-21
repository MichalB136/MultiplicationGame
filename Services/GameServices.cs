using MultiplicationGame.Models;
using System.Collections.Immutable;

namespace MultiplicationGame.Services;

public interface IGameService
{
    QuestionDto GetQuestion(int level, string? solvedQuestions);
    AnswerResultDto CheckAnswer(int userAnswer, int a, int b);
}

public sealed class GameService(
    Microsoft.Extensions.Options.IOptions<GameSettings> options,
    ILogger<GameService> logger) : IGameService
{
    private readonly Random _random = new();
    private readonly GameSettings _settings = options?.Value ?? new GameSettings();

    public QuestionDto GetQuestion(int level, string? solvedQuestions)
    {
        logger.LogInformation(
            "GetQuestion called: Level={Level}, SolvedQuestionsLength={Length}",
            level,
            solvedQuestions?.Length ?? 0);

        var configuredLevels = _settings.Levels ?? new[] { 100 };
        if (!configuredLevels.Contains(level))
        {
            logger.LogWarning(
                "Invalid level requested: {Level}. Available levels: {AvailableLevels}",
                level,
                string.Join(", ", configuredLevels));
            return new QuestionDto(0, 0, level);
        }

        var solved = ParseSolvedQuestions(solvedQuestions ?? string.Empty);
        logger.LogDebug(
            "Parsed {SolvedCount} solved questions for level {Level}",
            solved.Count,
            level);

        var allQuestions = GenerateAllQuestions(level);
        logger.LogDebug(
            "Generated {TotalQuestions} total questions for level {Level}",
            allQuestions.Count,
            level);

        var available = FilterAvailableQuestions(allQuestions, solved);
        logger.LogDebug(
            "Filtered to {AvailableQuestions} available questions (from {TotalQuestions})",
            available.Count,
            allQuestions.Count);

        if (available.Count == 0)
        {
            logger.LogWarning(
                "No available questions for level {Level} with {SolvedCount} solved",
                level,
                solved.Count);
            return new QuestionDto(0, 0, level);
        }

        var selected = SelectQuestion(available);
        logger.LogInformation(
            "Selected question: {A} × {B} for level {Level}",
            selected.a,
            selected.b,
            level);

        return new QuestionDto(selected.a, selected.b, level);
    }

    private static HashSet<string> ParseSolvedQuestions(string solvedQuestions) =>
        new((solvedQuestions ?? "").Split(';', StringSplitOptions.RemoveEmptyEntries));

    private List<(int a, int b)> GenerateAllQuestions(int level)
    {
        var maxNum = level == 1000 ? 1000 : _settings.DefaultMaxMultiplier;
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
        // Reduce frequency of configured low-probability factors
        var lowFactors = _settings.LowProbabilityFactors ?? System.Array.Empty<int>();
        var lowProbabilityFactors = new HashSet<int>(lowFactors);

        var undesired = available.Where(q => lowProbabilityFactors.Contains(q.a) || lowProbabilityFactors.Contains(q.b)).ToList();
        var desired = available.Where(q => !lowProbabilityFactors.Contains(q.a) && !lowProbabilityFactors.Contains(q.b)).ToList();

        logger.LogDebug(
            "Question pool: {DesiredCount} desired, {UndesiredCount} undesired (total: {TotalCount})",
            desired.Count,
            undesired.Count,
            available.Count);

        // If one group is empty, fall back to uniform random selection
        if (undesired.Count == 0 || desired.Count == 0)
        {
            var pick = available[_random.Next(available.Count)];
            logger.LogDebug("Selection: uniform fallback → {A}×{B}", pick.a, pick.b);
            return pick;
        }

        // Give undesired pairs a smaller chance (percentage configured in settings)
        var chance = Math.Clamp(_settings.LowFactorChancePercent, 0, 100);
        var roll = _random.Next(100);
        
        logger.LogDebug(
            "Selection roll: {Roll}/100 (threshold: {Threshold})",
            roll,
            chance);

        if (roll < chance)
        {
            var pick = undesired[_random.Next(undesired.Count)];
            logger.LogDebug("Selection: undesired bucket (low-probability factors) → {A}×{B}", pick.a, pick.b);
            return pick;
        }
        else
        {
            var pick = desired[_random.Next(desired.Count)];
            logger.LogDebug("Selection: desired bucket → {A}×{B}", pick.a, pick.b);
            return pick;
        }
    }

    public AnswerResultDto CheckAnswer(int userAnswer, int a, int b)
    {
        var correctAnswer = a * b;
        var isCorrect = userAnswer == correctAnswer;

        logger.LogInformation(
            "Answer checked: {A} × {B} = {CorrectAnswer}, User answered: {UserAnswer}, Result: {IsCorrect}",
            a,
            b,
            correctAnswer,
            userAnswer,
            isCorrect ? "✓ Correct" : "✗ Incorrect");

        return new AnswerResultDto(isCorrect, correctAnswer);
    }
}

public interface IGameStateService
{
    ImmutableList<string> ParseHistory(string? historyRaw);
    ImmutableList<HistoryEntryDto> ParseHistoryWithCorrectness(string? historyRaw);
    string SerializeHistory(ImmutableList<string> history);
    string UpdateSolvedQuestions(string? currentSolved, int a, int b);
    string CreateHistoryEntry(int a, int b, int correctAnswer, int userAnswer);
}

public sealed class GameStateService(ILogger<GameStateService> logger) : IGameStateService
{
    public ImmutableList<string> ParseHistory(string? historyRaw)
    {
        var isEmpty = string.IsNullOrEmpty(historyRaw);
        logger.LogDebug(
            "ParseHistory: Input {Status}, Length={Length}",
            isEmpty ? "empty" : "has data",
            historyRaw?.Length ?? 0);

        return isEmpty || historyRaw is null
            ? ImmutableList<string>.Empty
            : historyRaw.Split("||").ToImmutableList();
    }

    public ImmutableList<HistoryEntryDto> ParseHistoryWithCorrectness(string? historyRaw)
    {
        var historyItems = ParseHistory(historyRaw);
        var result = historyItems.Select(ParseHistoryEntry).ToImmutableList();
        
        logger.LogDebug(
            "ParseHistoryWithCorrectness: Parsed {Count} entries",
            result.Count);
        
        return result;
    }

    public string SerializeHistory(ImmutableList<string> history)
    {
        var result = string.Join("||", history);
        
        logger.LogDebug(
            "SerializeHistory: {Count} entries → {Length} characters",
            history.Count,
            result.Length);
        
        return result;
    }

    public string UpdateSolvedQuestions(string? currentSolved, int a, int b)
    {
        var solved = (currentSolved ?? "").Split(';', StringSplitOptions.RemoveEmptyEntries).ToList();
        var key = $"{a}-{b}";
        var wasAdded = false;
        
        if (!solved.Contains(key))
        {
            solved.Add(key);
            wasAdded = true;
        }

        var result = string.Join(";", solved);
        
        logger.LogDebug(
            "UpdateSolvedQuestions: Key={Key}, WasAdded={WasAdded}, TotalSolved={TotalCount}, ResultLength={Length}",
            key,
            wasAdded,
            solved.Count,
            result.Length);
        
        return result;
    }

    public string CreateHistoryEntry(int a, int b, int correctAnswer, int userAnswer)
    {
        var entry = $"{a} × {b} = {correctAnswer} (Twoja: {userAnswer})";
        
        logger.LogDebug(
            "CreateHistoryEntry: {Entry}",
            entry);
        
        return entry;
    }

    private HistoryEntryDto ParseHistoryEntry(string historyString)
    {
        if (!historyString.Contains("Twoja: ") || !historyString.Contains('='))
        {
            logger.LogWarning(
                "Invalid history entry format: {Entry}",
                historyString);
            return new HistoryEntryDto(historyString, false);
        }

        try
        {
            var userAnswerPart = historyString.Split("Twoja: ")[1].Split(')')[0].Trim();
            var correctAnswerPart = historyString.Split('=')[1].Split('(')[0].Trim();
            var isCorrect = userAnswerPart == correctAnswerPart;
            
            logger.LogDebug(
                "ParseHistoryEntry: {Entry} → IsCorrect={IsCorrect}",
                historyString,
                isCorrect);
            
            return new HistoryEntryDto(historyString, isCorrect);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Error parsing history entry: {Entry}",
                historyString);
            return new HistoryEntryDto(historyString, false);
        }
    }
}