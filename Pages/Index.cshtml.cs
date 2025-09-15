// ...existing code...
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MultiplicationGame.Models;
using MultiplicationGame.Services;
using System.Collections.Immutable;

namespace MultiplicationGame.Pages;

public class IndexModel : PageModel
{
    [BindProperty]
    public int LearningStep { get; set; } = 0;
    public int Step => LearningStep;
    private const int REQUIRED_CORRECT_ANSWERS = 10;

    public enum LearningMode
    {
        Normal,
        Learning,
        Training,
        Mixed,
        Timed
    }

    [BindProperty]
    public string HistoryRaw { get; set; } = "";
    public ImmutableList<string> History { get; set; } = ImmutableList<string>.Empty;
    public ImmutableList<HistoryEntryDto> HistoryWithCorrectness { get; set; } = ImmutableList<HistoryEntryDto>.Empty;
    [BindProperty]
    public bool NextQuestion { get; set; }
    [BindProperty]
    public int Streak { get; set; } = 0;
    public bool GameWon { get; set; }
    [BindProperty]
    public bool GameLost { get; set; } = false;
    private readonly IGameService _gameService;
    private readonly IGameStateService _gameStateService;

    [BindProperty]
    public LearningMode Mode { get; set; } = LearningMode.Normal;

    public IndexModel(IGameService gameService, IGameStateService gameStateService)
    {
        _gameService = gameService;
        _gameStateService = gameStateService;
    }

    [BindProperty]
    public int Level { get; set; } = 20;
    [BindProperty]
    public int A { get; set; }
    [BindProperty]
    public int B { get; set; }
    [BindProperty]
    public int UserAnswer { get; set; }

    public QuestionDto? Question { get; set; }
    [BindProperty]
    public string SolvedQuestions { get; set; } = "";
    public bool AnswerChecked { get; set; }
    public bool IsCorrect { get; set; }
    public int CorrectAnswer { get; set; }
    [BindProperty]
    public int AttemptsLeft { get; set; } = 3;

    // Właściwości z logiką biznesową
    public bool IsGameWon => Streak >= REQUIRED_CORRECT_ANSWERS;
    public bool ShouldContinueGame => Streak < REQUIRED_CORRECT_ANSWERS;
    public int Progress => Math.Min(Streak, REQUIRED_CORRECT_ANSWERS);
    public static int RequiredAnswers => REQUIRED_CORRECT_ANSWERS;

    public void OnGet()
    {
        // Tryb można ustawić przez query string, domyślnie Normal
        if (Request.Query.TryGetValue("mode", out var modeVal) && Enum.TryParse(modeVal, out LearningMode mode))
        {
            Mode = mode;
        }

        // Also allow mode to be persisted via cookie when changed client-side
        if (Request.Cookies.TryGetValue("mg_mode", out var cookieModeVal) && Enum.TryParse(cookieModeVal, out LearningMode cookieMode))
        {
            Mode = cookieMode;
        }

        // If client requests only the interactive fragment, we'll let the Razor page
        // render and return only the fragment HTML in the response body. That logic
        // is implemented by checking Request.Query["partial"] in OnGet and then
        // using the Razor page to render only the fragment. We'll rely on the page
        // rendering pipeline — the fetch call on the client expects the full
        // fragment HTML string to replace innerHTML.
    }

    public void OnPost()
    {
        // If client used cookie-based mode toggling, prefer cookie value to avoid losing Level
        if (Request.Cookies.TryGetValue("mg_mode", out var cookieModeValPost) && Enum.TryParse(cookieModeValPost, out LearningMode cookieMode))
        {
            Mode = cookieMode;
        }
        // Obsługa kroków nauki w trybie Learning
        if (Mode == LearningMode.Learning)
        {
            // Jeśli użytkownik kliknął "Następny krok" zamiast "Sprawdź"
            if (Request.Form.ContainsKey("NextLearningStep"))
            {
                LearningStep++;
                // Nie sprawdzamy odpowiedzi, tylko przechodzimy do kolejnego kroku
                Question = new QuestionDto(A, B, Level);
                return;
            }
            else
            {
                LearningStep = 0; // Reset przy nowym pytaniu lub sprawdzeniu
            }
        }
        RestoreHistoryFromRaw();

        if (ShouldStartNewGame())
        {
            StartNewGame();
            return;
        }

        ProcessAnswer();
    }

    private void RestoreHistoryFromRaw()
    {
        History = _gameStateService.ParseHistory(HistoryRaw);
        HistoryWithCorrectness = _gameStateService.ParseHistoryWithCorrectness(HistoryRaw);
    }

    private bool ShouldStartNewGame() =>
        A == 0 && B == 0 || (AttemptsLeft == 0 && NextQuestion) || (NextQuestion && IsCorrect);

    private void StartNewGame()
    {
        ResetGameState();
        var question = _gameService.GetQuestion(Level, SolvedQuestions);
        ApplyQuestion(question);
        ResetQuestionState();
    }

    private void ResetGameState()
    {
        Streak = 0;
        AttemptsLeft = 3;
        SolvedQuestions = "";
        History = ImmutableList<string>.Empty;
        HistoryRaw = "";
        GameLost = false;
    }

    private void ApplyQuestion(QuestionDto question)
    {
        Question = question;
        Level = question.Level;
        A = question.A;
        B = question.B;
    }

    private void ResetQuestionState()
    {
        AnswerChecked = false;
        IsCorrect = false;
        CorrectAnswer = 0;
    }

    private void ProcessAnswer()
    {
        var result = _gameService.CheckAnswer(UserAnswer, A, B);
        ApplyAnswerResult(result);

        if (IsCorrect)
        {
            HandleCorrectAnswer();
        }
        else
        {
            HandleIncorrectAnswer();
        }

        if (IsGameWon)
        {
            GameWon = true;
        }
    }

    private void ApplyAnswerResult(AnswerResultDto? result)
    {
        AnswerChecked = true;
        IsCorrect = result?.IsCorrect ?? false;
        CorrectAnswer = result?.Correct ?? 0;
    }

    private void HandleCorrectAnswer()
    {
        Streak++;
        UpdateSolvedQuestions();
        AddToHistory();
        // Zachowaj tryb nauki
        if (Mode == LearningMode.Learning)
            Mode = LearningMode.Learning;

        if (ShouldContinueGame)
        {
            LoadNextQuestion();
        }
        else
        {
            GameWon = true;
        }
    }

    private void UpdateSolvedQuestions()
    {
        SolvedQuestions = _gameStateService.UpdateSolvedQuestions(SolvedQuestions, A, B);
    }

    private void AddToHistory()
    {
        var entry = _gameStateService.CreateHistoryEntry(A, B, CorrectAnswer, UserAnswer);
        History = History.Add(entry);
        HistoryRaw = _gameStateService.SerializeHistory(History);
    }

    private void LoadNextQuestion()
    {
        var question = _gameService.GetQuestion(Level, SolvedQuestions);
        ApplyQuestion(question);
        ResetQuestionState();
        UserAnswer = 0;
        // Zachowaj tryb nauki
        if (Mode == LearningMode.Learning)
            Mode = LearningMode.Learning;
    }

    private void HandleIncorrectAnswer()
    {
        AttemptsLeft--;
        AddToHistory();

        if (AttemptsLeft <= 0)
        {
            Streak = 0;  // Reset postępu tylko po utracie wszystkich żyć
            SolvedQuestions = "";
            GameLost = true;
            return;
        }

        Question ??= new QuestionDto(A, B, Level);
    }
}
