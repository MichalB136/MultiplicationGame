using System.Globalization;
using System.Text.Json;
using MultiplicationGame.Models;
using MultiplicationGame.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MultiplicationGame.Pages;

public sealed class TrainingModel(ITrainingService trainingService) : PageModel
{
    private const string SessionStateKey = "MultiplicationTrainingState";
    private const int QuestionCount = 10;

    [BindProperty]
    public List<int> SelectedTables { get; set; } = [];

    [BindProperty]
    public string? UserAnswerText { get; set; } = "";

    [BindProperty]
    public bool AdaptiveMode { get; set; }

    [BindProperty]
    public bool HintsEnabled { get; set; }

    [BindProperty]
    public bool UsedHint { get; set; }

    [BindProperty]
    public string? KnowledgeRaw { get; set; } = "{}";

    public TrainingSessionState? SessionState { get; private set; }

    public bool IsComplete =>
        SessionState is not null &&
        SessionState.Answers.Count >= SessionState.Questions.Count;

    public TrainingQuestionDto? CurrentQuestion =>
        SessionState is not null && !IsComplete
            ? SessionState.Questions[SessionState.Answers.Count]
            : null;

    public TrainingAnswerDto? LastAnswer =>
        SessionState?.Answers.LastOrDefault();

    public int CorrectAnswers =>
        SessionState?.Answers.Count(answer => answer.IsCorrect) ?? 0;

    public string? CurrentHint =>
        SessionState is { HintsEnabled: true } && CurrentQuestion is { } question
            ? CreateHint(question)
            : null;

    public int ElapsedSeconds =>
        SessionState is null
            ? 0
            : (int)Math.Max(
                0,
                ((SessionState.CompletedAtUnixMilliseconds ??
                  DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()) -
                 SessionState.StartedAtUnixMilliseconds) / 1000);

    public void OnGet()
    {
        SessionState = ReadSessionState();
    }

    public IActionResult OnPostStart()
    {
        if (SelectedTables is null ||
            SelectedTables.Count == 0 ||
            SelectedTables.Any(table => table is < 1 or > 10))
        {
            ModelState.AddModelError(
                nameof(SelectedTables),
                "Wybierz co najmniej jedną tabliczkę od 1 do 10.");
            return Page();
        }

        IReadOnlyDictionary<string, TrainingFactProgress> progress =
            new Dictionary<string, TrainingFactProgress>();
        if (AdaptiveMode && !TryReadKnowledge(out progress))
        {
            return Page();
        }

        var questions = AdaptiveMode
            ? trainingService.CreateAdaptiveQuestions(SelectedTables, QuestionCount, progress)
            : trainingService.CreateQuestions(SelectedTables, QuestionCount);
        var state = new TrainingSessionState(
            questions,
            [],
            [],
            DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            RoundId: Guid.NewGuid(),
            IsAdaptive: AdaptiveMode,
            HintsEnabled: HintsEnabled);

        SaveSessionState(state);
        return RedirectToPage();
    }

    public IActionResult OnPostAnswer()
    {
        var state = ReadSessionState();
        SessionState = state;
        var question = state is not null && state.Answers.Count < state.Questions.Count
            ? state.Questions[state.Answers.Count]
            : null;

        if (state is null || question is null)
        {
            ModelState.AddModelError(
                string.Empty,
                "Nie ma aktywnej rundy treningowej. Rozpocznij nowy trening.");
            return Page();
        }

        if (string.IsNullOrWhiteSpace(UserAnswerText) ||
            !System.Text.RegularExpressions.Regex.IsMatch(UserAnswerText, @"^-?\d+$") ||
            !int.TryParse(
                UserAnswerText,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out var userAnswer))
        {
            ModelState.AddModelError(
                nameof(UserAnswerText),
                "Wpisz liczbę całkowitą.");
            return Page();
        }

        var isCorrect = userAnswer == question.A * question.B;
        var answers = new List<TrainingAnswerDto>(state.Answers)
        {
            new(question, userAnswer, isCorrect, state.HintsEnabled && UsedHint)
        };
        var mistakes = new List<TrainingQuestionDto>(state.Mistakes);
        if (!isCorrect)
        {
            mistakes.Add(question);
        }

        var updatedState = state with
        {
            Answers = answers,
            Mistakes = mistakes,
            CompletedAtUnixMilliseconds = answers.Count == state.Questions.Count
                ? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                : null
        };

        SessionState = updatedState;
        SaveSessionState(updatedState);
        return RedirectToPage();
    }

    public IActionResult OnPostRepeatMistakes()
    {
        var state = ReadSessionState();
        SessionState = state;
        if (state is null ||
            state.Answers.Count < state.Questions.Count ||
            state.Mistakes.Count == 0)
        {
            ModelState.AddModelError(
                string.Empty,
                "Brak błędnych działań do powtórzenia.");
            return Page();
        }

        var repeatedState = new TrainingSessionState(
            [.. state.Mistakes],
            [],
            [],
            DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            RoundId: Guid.NewGuid(),
            IsAdaptive: state.IsAdaptive,
            HintsEnabled: state.HintsEnabled);

        SessionState = repeatedState;
        SaveSessionState(repeatedState);
        return RedirectToPage();
    }

    public IActionResult OnPostNewTraining()
    {
        HttpContext.Session.Remove(SessionStateKey);
        return RedirectToPage();
    }

    private TrainingSessionState? ReadSessionState()
    {
        var serializedState = HttpContext.Session.GetString(SessionStateKey);
        return serializedState is null
            ? null
            : JsonSerializer.Deserialize<TrainingSessionState>(serializedState);
    }

    private void SaveSessionState(TrainingSessionState state)
    {
        HttpContext.Session.SetString(
            SessionStateKey,
            JsonSerializer.Serialize(state));
    }

    private bool TryReadKnowledge(
        out IReadOnlyDictionary<string, TrainingFactProgress> progress)
    {
        progress = new Dictionary<string, TrainingFactProgress>();
        if (string.IsNullOrWhiteSpace(KnowledgeRaw) || KnowledgeRaw.Length > 20_000)
        {
            ModelState.AddModelError(
                nameof(KnowledgeRaw),
                "Zapisane postępy są zbyt duże lub nieprawidłowe.");
            return false;
        }

        Dictionary<string, TrainingFactProgress>? parsed;
        try
        {
            parsed = JsonSerializer.Deserialize<Dictionary<string, TrainingFactProgress>>(KnowledgeRaw);
        }
        catch (JsonException)
        {
            ModelState.AddModelError(
                nameof(KnowledgeRaw),
                "Nie można odczytać lokalnych postępów. Wyczyść zapisane dane i spróbuj ponownie.");
            return false;
        }

        if (parsed is null || parsed.Any(entry =>
                !System.Text.RegularExpressions.Regex.IsMatch(
                    entry.Key,
                    @"^(?:[1-9]|10)-(?:[1-9]|10)$") ||
                entry.Value is null ||
                entry.Value.Attempts is < 0 or > 1_000_000 ||
                entry.Value.Correct < 0 ||
                entry.Value.Correct > entry.Value.Attempts ||
                entry.Value.AssistedAnswers < 0 ||
                entry.Value.AssistedAnswers > entry.Value.Attempts))
        {
            ModelState.AddModelError(
                nameof(KnowledgeRaw),
                "Zapisane postępy mają nieprawidłowy format. Wyczyść lokalny zapis i spróbuj ponownie.");
            return false;
        }

        progress = parsed;
        return true;
    }

    private static string CreateHint(TrainingQuestionDto question)
    {
        if (question.B == 1)
        {
            return $"Mnożenie przez 1 nie zmienia liczby: {question.A} × 1 = {question.A}.";
        }

        if (question.B <= 5)
        {
            var repeatedAddition = string.Join(
                " + ",
                Enumerable.Repeat(question.A, question.B));
            return $"{question.A} × {question.B} to {repeatedAddition}. Dodaj te liczby.";
        }

        var remainder = question.B - 5;
        return $"Rozbij działanie: {question.A} × {question.B} = " +
               $"{question.A} × 5 + {question.A} × {remainder} = " +
               $"{question.A * 5} + {question.A * remainder}.";
    }
}
