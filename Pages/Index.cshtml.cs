// ...existing code...
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using MultiplicationGame.Models;
using MultiplicationGame.Services;
using System.Collections.Immutable;

namespace MultiplicationGame.Pages;

public class IndexModel : PageModel
{
    [BindProperty]
    public int LearningStep { get; set; } = 0;
    public int Step => LearningStep;
    private int _requiredCorrectAnswers = 10;

    public enum LearningMode
    {
        Normal,
        Learning
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
    private readonly ILogger<IndexModel>? _logger;
    private readonly MultiplicationGame.Services.GameSettings? _settings;

    [BindProperty]
    public LearningMode Mode { get; set; } = LearningMode.Normal;

    public IndexModel(IGameService gameService, IGameStateService gameStateService, ILogger<IndexModel>? logger = null, Microsoft.Extensions.Options.IOptions<MultiplicationGame.Services.GameSettings>? options = null)
    {
        _gameService = gameService;
        _gameStateService = gameStateService;
        _logger = logger;
        _settings = options?.Value;
        if (_settings is not null && _settings.RequiredCorrectAnswers > 0)
            _requiredCorrectAnswers = _settings.RequiredCorrectAnswers;
    }

    [BindProperty]
    public int Level { get; set; } = 100;
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
    [BindProperty]
    public int TotalAnswers { get; set; } = 0; // Licznik wszystkich przesłanych odpowiedzi (poprawnych i błędnych)
    [BindProperty]
    public int PerfectStreak { get; set; } = 0; // Licznik poprawnych odpowiedzi bez straty życia
    public bool BonusAwarded { get; set; } = false; // Flaga informująca czy właśnie przyznano bonus
    [BindProperty]
    public long GameStartTime { get; set; } = 0; // Unix timestamp w milisekundach
    public int GameElapsedSeconds { get; set; } = 0; // Czas gry w sekundach

    // Właściwości z logiką biznesową
    public bool IsGameWon => Streak >= _requiredCorrectAnswers;
    public bool ShouldContinueGame => Streak < _requiredCorrectAnswers;
    public int Progress => Math.Min(Streak, _requiredCorrectAnswers);
    public int RequiredAnswers => _requiredCorrectAnswers;
    public int InitialAttempts => _settings?.InitialAttempts ?? 3;
    public int BonusAttemptsThreshold => _settings?.BonusAttemptsThreshold ?? 5;

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

        // Zainicjalizuj timer przy pierwszym załadowaniu gry
        if (GameStartTime == 0)
        {
            GameStartTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        // Ensure a question is available for the initial page render so the
        // interactive fragment shows the standard question area (product boxes)
        // instead of only the progress panel. Use the game service to get a
        // question for the current Level and solved questions state.
        if (Question == null)
        {
            try
            {
                var question = _gameService.GetQuestion(Level, SolvedQuestions ?? string.Empty);
                if (question != null)
                {
                    ApplyQuestion(question);
                    _logger?.LogDebug("[OnGet] Initial question loaded: {A}×{B} (Level={Level})", question.A, question.B, question.Level);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "[OnGet] Failed to load initial question for level {Level}", Level);
                // fallback: leave Question null
            }
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
        
        // Zainicjalizuj timer jeśli nie jest ustawiony (nowa gra lub reload)
        if (GameStartTime == 0)
        {
            GameStartTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }
        
        // Obsługa kroków nauki w trybie Learning
        if (Mode == LearningMode.Learning)
        {
            // Jeśli użytkownik kliknął "Następny krok" zamiast "Sprawdź"
            if (Request.Form.ContainsKey("NextLearningStep"))
            {
                    LearningStep++;
                    // Restore history so the UI continues showing previous attempts
                    RestoreHistoryFromRaw();
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
    _logger?.LogDebug("[OnPost] Restored history: Count={Count}", History.Count);

        if (ShouldStartNewGame())
        {
            _logger?.LogInformation("[OnPost] Starting new game (NextQuestion={NextQuestion}, AttemptsLeft={AttemptsLeft}, PrevIsCorrect={IsCorrect}, AnswerChecked={AnswerChecked})", NextQuestion, AttemptsLeft, IsCorrect, AnswerChecked);
            StartNewGame();
            return;
        }

        ProcessAnswer();
    }

    private void RestoreHistoryFromRaw()
    {
        // Ensure HistoryRaw and SolvedQuestions are not null (can happen if binding fails or form doesn't include them)
        HistoryRaw ??= "";
        SolvedQuestions ??= "";
        History = _gameStateService.ParseHistory(HistoryRaw);
        HistoryWithCorrectness = _gameStateService.ParseHistoryWithCorrectness(HistoryRaw);
    }

    private bool ShouldStartNewGame() =>
        // Start new game ONLY when previous answer cycle is complete (AnswerChecked)
        // This prevents skipping processing of a newly submitted answer if NextQuestion is still true.
        (A == 0 && B == 0) ||
        (AnswerChecked && AttemptsLeft == 0 && NextQuestion) ||
        (AnswerChecked && NextQuestion && IsCorrect);

    private void StartNewGame()
    {
        ResetGameState();
        var question = _gameService.GetQuestion(Level, SolvedQuestions);
        ApplyQuestion(question);
        ResetQuestionState();
        _logger?.LogInformation("[StartNewGame] New question: {A}×{B} Level={Level}", A, B, Level);
    }

    private void ResetGameState()
    {
        Streak = 0;
        PerfectStreak = 0;
        AttemptsLeft = _settings?.InitialAttempts ?? 3;
        SolvedQuestions = "";
        History = ImmutableList<string>.Empty;
        HistoryRaw = "";
        GameLost = false;
        GameStartTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        GameElapsedSeconds = 0;
    }

    private void ApplyQuestion(QuestionDto question)
    {
        Question = question;
        Level = question.Level;
        A = question.A;
        B = question.B;
        _logger?.LogDebug("[ApplyQuestion] Applied question {A}×{B} (Level={Level})", A, B, Level);
    }

    private void ResetQuestionState()
    {
        AnswerChecked = false;
        IsCorrect = false;
        CorrectAnswer = 0;
    }

    private void ProcessAnswer()
    {
        // Oblicz czas gry jeśli gra się rozpoczęła
        if (GameStartTime > 0)
        {
            var elapsed = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - GameStartTime;
            GameElapsedSeconds = (int)(elapsed / 1000);
        }
        
        var result = _gameService.CheckAnswer(UserAnswer, A, B);
        ApplyAnswerResult(result);
        _logger?.LogInformation("[ProcessAnswer] User={UserAnswer} Correct={CorrectAnswer} IsCorrect={IsCorrect} Streak={Streak} AttemptsLeft={AttemptsLeft}", UserAnswer, CorrectAnswer, IsCorrect, Streak, AttemptsLeft);
        TotalAnswers++;

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

        // Podsumowanie po obsłudze odpowiedzi
        var solvedCount = SolvedQuestions.Split(';', StringSplitOptions.RemoveEmptyEntries).Length;
        if (History.Count != TotalAnswers)
        {
            _logger?.LogWarning("[ProcessAnswer-End] History.Count={HistoryCount} != TotalAnswers={TotalAnswers}. SolvedCount={SolvedCount} IsCorrect={IsCorrect}", History.Count, TotalAnswers, solvedCount, IsCorrect);
        }
        else
        {
            _logger?.LogDebug("[ProcessAnswer-End] Counts aligned: History={HistoryCount} TotalAnswers={TotalAnswers} Solved={SolvedCount}", History.Count, TotalAnswers, solvedCount);
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
        PerfectStreak++;
        UpdateSolvedQuestions();
        AddToHistory();
        
        // Sprawdź czy gracz zasłużył na dodatkową szansę
        var bonusThreshold = _settings?.BonusAttemptsThreshold ?? 5;
        var initialAttempts = _settings?.InitialAttempts ?? 3;
        
        if (initialAttempts > 0 && bonusThreshold > 0 && PerfectStreak >= bonusThreshold)
        {
            // Przyznaj bonus - dodaj życie (bez ograniczenia do initialAttempts)
            AttemptsLeft++;
            BonusAwarded = true;
            _logger?.LogInformation("[HandleCorrectAnswer] Bonus awarded! PerfectStreak={PerfectStreak}, AttemptsLeft increased to {AttemptsLeft}", PerfectStreak, AttemptsLeft);
            PerfectStreak = 0; // Reset licznika po przyznaniu bonusu
        }
        
        // Zachowaj tryb nauki
        if (Mode == LearningMode.Learning)
            Mode = LearningMode.Learning;

        _logger?.LogInformation("[HandleCorrectAnswer] Streak={Streak} PerfectStreak={PerfectStreak} HistoryCount={HistoryCount} Solved={SolvedCount}", Streak, PerfectStreak, History.Count, SolvedQuestions.Split(';', StringSplitOptions.RemoveEmptyEntries).Length);

        if (ShouldContinueGame)
        {
            LoadNextQuestion();
        }
        else
        {
            GameWon = true;
            _logger?.LogInformation("[HandleCorrectAnswer] Game won! Total history entries={HistoryCount}", History.Count);
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
        if (History.Count > 0)
        {
            _logger?.LogDebug("[AddToHistory] Entry added: {Entry}. NewCount={Count}", entry, History.Count);
        }
        // Validation: if streak + failures != history count, log warning
        var solvedCount = (SolvedQuestions ?? "").Split(';', StringSplitOptions.RemoveEmptyEntries).Length;
        if (History.Count < solvedCount)
        {
            _logger?.LogWarning("[AddToHistory] History count ({HistoryCount}) < solved questions count ({SolvedCount}). Possible missing entry.", History.Count, solvedCount);
        }
        // Re-parse correctness list so GameWon/GameLost views show latest entry
        // Ensure HistoryRaw is not null before parsing
        HistoryWithCorrectness = _gameStateService.ParseHistoryWithCorrectness(HistoryRaw ?? "");
        _logger?.LogDebug("[AddToHistory] HistoryWithCorrectness refreshed. Count={CorrectnessCount}", HistoryWithCorrectness.Count);
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
        _logger?.LogDebug("[LoadNextQuestion] Next question loaded: {A}×{B} Level={Level}", A, B, Level);
    }

    private void HandleIncorrectAnswer()
    {
        var initialAttempts = _settings?.InitialAttempts ?? 3;
        
        // Reset perfect streak when losing a life
        PerfectStreak = 0;
        
        // Only decrement if there's a limit (InitialAttempts > 0)
        if (initialAttempts > 0)
        {
            AttemptsLeft--;
        }
        
        AddToHistory();
        _logger?.LogInformation("[HandleIncorrectAnswer] AttemptsLeft={AttemptsLeft} HistoryCount={HistoryCount}", AttemptsLeft, History.Count);

        // Game is lost only if there's a limit and attempts are exhausted
        if (initialAttempts > 0 && AttemptsLeft <= 0)
        {
            Streak = 0;  // Reset postępu tylko po utracie wszystkich żyć
            SolvedQuestions = "";
            GameLost = true;
            _logger?.LogWarning("[HandleIncorrectAnswer] Game lost. HistoryCount={HistoryCount}", History.Count);
            return;
        }

        Question ??= new QuestionDto(A, B, Level);
    }
}
