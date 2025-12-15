using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;

namespace MultiplicationGame.Pages;

public class EquationsModel : PageModel
{
    private readonly ILogger<EquationsModel> _logger;

    public EquationsModel(ILogger<EquationsModel> logger)
    {
        _logger = logger;
    }

    // Difficulty: "1", "2", "3"
    [BindProperty]
    public string Difficulty { get; set; } = "1";

    // Single-question state
    [BindProperty]
    public int A { get; set; }
    [BindProperty]
    public int B { get; set; }
    [BindProperty]
    public string Operator { get; set; } = "+"; // + - × ÷
    [BindProperty]
    public string? UserAnswer { get; set; }
    public double CorrectAnswer { get; set; }
    public bool AnswerChecked { get; set; }
    public bool IsCorrect { get; set; }

    // Progress
    [BindProperty]
    public int TotalAnswered { get; set; } = 0; // licznik wszystkich odpowiedzi (niezależnie od poprawności)
    public int RequiredAnswers { get; set; } = 20; // stała jak w grze mnożenia

    [BindProperty]
    public bool NextQuestion { get; set; }

    [BindProperty]
    public string HistoryRaw { get; set; } = string.Empty;
    public List<global::MultiplicationGame.Models.HistoryEntry> History { get; set; } = new();
    public bool GameFinished => History.Count >= RequiredAnswers;

    public void OnGet()
    {
        if (A == 0 && B == 0)
        {
            GenerateQuestion();
        }
    }

    public IActionResult OnPostGenerate()
    {
        // difficulty changed
        ResetAll();
        GenerateQuestion();
        return Page();
    }

    public IActionResult OnPostSubmit()
    {
        RestoreHistory();

        // If advancing to next question, just generate new question without modifying history
        if (NextQuestion && !GameFinished)
        {
            ResetForNext();
            GenerateQuestion();
            return Page();
        }

        // Compute correct answer for current question
        CorrectAnswer = Compute(A, B, Operator);

        // Evaluate user's answer
        var ok = TryParseNumber(UserAnswer, out var userVal);
        AnswerChecked = true;
        IsCorrect = ok && Math.Abs(userVal - CorrectAnswer) < 0.0001;
        
        // Prevent duplicate submissions (e.g., double-click)
        var currentText = $"{A} {Operator} {B}";
        var last = History.Count > 0 ? History[^1] : null;
        var isDuplicate = last is not null
            && string.Equals(last.Text, currentText, StringComparison.Ordinal)
            && string.Equals(last.User, UserAnswer ?? string.Empty, StringComparison.Ordinal)
            && Math.Abs(last.Correct - CorrectAnswer) < 0.0001
            && last.IsCorrect == IsCorrect;

        if (isDuplicate)
        {
            // No-op, keep state as-is
            _logger.LogDebug("Duplicate submission ignored: {Text} User={User}", currentText, UserAnswer);
            return Page();
        }

        TotalAnswered++;

        _logger.LogInformation("Equation checked: {A} {Op} {B} = {Ans}, User={User}, OK={OK}", A, Operator, B, CorrectAnswer, UserAnswer, IsCorrect);

        // Append to history
        var entry = new global::MultiplicationGame.Models.HistoryEntry(
            $"{A} {Operator} {B}",
            CorrectAnswer,
            UserAnswer ?? string.Empty,
            IsCorrect);
        History.Add(entry);
        HistoryRaw = JsonSerializer.Serialize(History);

        return Page();
    }

    private void RestoreHistory()
    {
        History = new List<global::MultiplicationGame.Models.HistoryEntry>();
        if (!string.IsNullOrWhiteSpace(HistoryRaw))
        {
            try
            {
                var parsed = JsonSerializer.Deserialize<List<global::MultiplicationGame.Models.HistoryEntry>>(HistoryRaw);
                if (parsed != null) History = parsed;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse HistoryRaw");
            }
        }
    }

    private void ResetForNext()
    {
        AnswerChecked = false;
        IsCorrect = false;
        CorrectAnswer = 0;
        UserAnswer = string.Empty;
    }

    private void ResetAll()
    {
        ResetForNext();
        TotalAnswered = 0;
        History = new List<global::MultiplicationGame.Models.HistoryEntry>();
        HistoryRaw = string.Empty;
    }

    private static bool TryParseNumber(string? raw, out double value)
    {
        var s = (raw ?? string.Empty).Trim().Replace(',', '.');
        return double.TryParse(s, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out value);
    }

    private void GenerateQuestion()
    {
        var rng = new Random();
        var op = rng.Next(4); // 0:+ 1:- 2:* 3:/
        int a = 0, b = 1;

        var diff = Difficulty ?? "1";
        if (diff == "1")
        {
            // Klasa 1: Podstawy (6-7 lat) - małe liczby, łatwa tabliczka
            switch (op)
            {
                case 0: a = rng.Next(0, 11); b = rng.Next(0, 11); break; // dodawanie 0-10
                case 1: a = rng.Next(0, 11); b = rng.Next(0, a + 1); break; // odejmowanie 0-10
                case 2: // mnożenie: tylko 2,3,4,5 (łatwiejsza tabliczka)
                    var easyFactors = new[] { 2, 3, 4, 5 };
                    a = easyFactors[rng.Next(easyFactors.Length)];
                    b = rng.Next(1, 6);
                    break;
                case 3: // dzielenie: tylko 2,3,4,5
                    var easyDivisors = new[] { 2, 3, 4, 5 };
                    b = easyDivisors[rng.Next(easyDivisors.Length)];
                    var q1 = rng.Next(1, 6);
                    a = b * q1;
                    break;
            }
        }
        else if (diff == "2")
        {
            // Klasa 2: Średni poziom (8-9 lat) - pełna tabliczka mnożenia
            switch (op)
            {
                case 0: a = rng.Next(0, 51); b = rng.Next(0, 51); break; // dodawanie 0-50
                case 1: a = rng.Next(0, 51); b = rng.Next(0, a + 1); break; // odejmowanie 0-50
                case 2: a = rng.Next(2, 11); b = rng.Next(2, 11); break; // mnożenie: pełna tabliczka 2-10
                case 3: b = rng.Next(2, 11); var q2 = rng.Next(1, 11); a = b * q2; break; // dzielenie 2-10
            }
        }
        else // "3"
        {
            // Klasa 3: Zaawansowany (10+ lat) - większe liczby, trudniejsze mnożenie
            switch (op)
            {
                case 0: a = rng.Next(0, 201); b = rng.Next(0, 201); break; // dodawanie 0-200
                case 1: a = rng.Next(0, 201); b = rng.Next(0, a + 1); break; // odejmowanie 0-200
                case 2: 
                    // Mnożenie: jeden czynnik z zakresu 2-20, drugi 2-12
                    if (rng.Next(2) == 0)
                    {
                        a = rng.Next(2, 21); // 2-20
                        b = rng.Next(2, 13); // 2-12
                    }
                    else
                    {
                        a = rng.Next(2, 13); // 2-12
                        b = rng.Next(2, 21); // 2-20
                    }
                    break;
                case 3: 
                    // Dzielenie: dzielnik 2-12, iloraz do 20
                    b = rng.Next(2, 13);
                    var q3 = rng.Next(1, 21);
                    a = b * q3;
                    break;
            }
        }

        A = a; B = b; Operator = op switch { 0 => "+", 1 => "-", 2 => "×", _ => "÷" };
        CorrectAnswer = Compute(A, B, Operator);
    }

    private static double Compute(int a, int b, string op) => op switch
    {
        "+" => a + b,
        "-" => a - b,
        "×" => a * b,
        "*" => a * b,
        "÷" => (double)a / b,
        "/" => (double)a / b,
        _ => 0
    };

    // HistoryEntry moved to Models\GameModels.cs
}
