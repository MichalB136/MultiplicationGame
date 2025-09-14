using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MultiplicationGame.Pages;

public class IndexModel : PageModel
{
    [BindProperty]
    public string HistoryRaw { get; set; } = "";
    public List<string> History { get; set; } = new List<string>();
    [BindProperty]
    public bool NextQuestion { get; set; }
    [BindProperty]
    public int Streak { get; set; } = 0;
    public bool GameWon { get; set; }
    [BindProperty]
    public bool GameLost { get; set; } = false;
    private readonly ILogger<IndexModel> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    public IndexModel(ILogger<IndexModel> logger, IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
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

    public async Task OnGetAsync()
    {
        // Nic nie rób, czekaj na wybór poziomu
    }

    public async Task OnPostAsync()
    {
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var client = _httpClientFactory.CreateClient();
        client.BaseAddress = new Uri(baseUrl);

        // Odtwórz historię z pola tekstowego
        History = string.IsNullOrEmpty(HistoryRaw)
            ? new List<string>()
            : HistoryRaw.Split("||").ToList();

        // Nowe równanie tylko po poprawnej odpowiedzi lub po 3 błędnych próbach lub na starcie
        if (A == 0 && B == 0 || (AttemptsLeft == 0 && NextQuestion) || (NextQuestion && IsCorrect))
        {
            // Resetuj całą próbę po 3 błędnych odpowiedziach lub na starcie
            Streak = 0;
            AttemptsLeft = 3;
            SolvedQuestions = "";
            History = new List<string>();
            HistoryRaw = "";
            GameLost = false;
            var url = $"/api/Multiplication/question?level={Level}&solved={SolvedQuestions}";
            var q = await client.GetFromJsonAsync<QuestionDto>(url);
            if (q != null)
            {
                Question = q;
                Level = q.Level;
                A = q.A;
                B = q.B;
            }
            AnswerChecked = false;
            IsCorrect = false;
            CorrectAnswer = 0;
            return;
        }

    // Usunięto automatyczne pobieranie nowego pytania przy sprawdzaniu odpowiedzi

        // Sprawdź odpowiedź
        var response = await client.PostAsJsonAsync("/api/Multiplication/answer", new { A, B, UserAnswer });
        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<AnswerResultDto>();
            AnswerChecked = true;
            IsCorrect = result?.IsCorrect ?? false;
            CorrectAnswer = result?.Correct ?? 0;
            if (IsCorrect)
            {
                Streak++;
                // Dodaj pytanie do listy rozwiązanych
                var solved = (SolvedQuestions ?? "").Split(';', StringSplitOptions.RemoveEmptyEntries).ToList();
                var key = $"{A}-{B}";
                if (!solved.Contains(key))
                    solved.Add(key);
                SolvedQuestions = string.Join(";", solved);
                // Nie resetuj AttemptsLeft po poprawnej odpowiedzi
                // Dodaj do historii
                History.Add($"{A} × {B} = {CorrectAnswer} (Twoja: {UserAnswer})");
                HistoryRaw = string.Join("||", History);
                if (Streak < 3)
                {
                    var url = $"/api/Multiplication/question?level={Level}&solved={SolvedQuestions}";
                    var q = await client.GetFromJsonAsync<QuestionDto>(url);
                    if (q != null)
                    {
                        Question = q;
                        Level = q.Level;
                        A = q.A;
                        B = q.B;
                    }
                    AnswerChecked = false;
                    IsCorrect = false;
                    CorrectAnswer = 0;
                    UserAnswer = 0;
                }
                else
                {
                    GameWon = true;
                }
            }
            else
            {
                // Każda błędna odpowiedź odejmuje życie i resetuje streak
                AttemptsLeft--;
                Streak = 0;
                // Dodaj do historii
                History.Add($"{A} × {B} = {CorrectAnswer} (Twoja: {UserAnswer})");
                HistoryRaw = string.Join("||", History);
                if (AttemptsLeft <= 0)
                {
                    // Po 3 błędnych próbach resetuj solved, wyczyść historię po kliknięciu dalej
                    SolvedQuestions = "";
                    GameLost = true;
                    // Wyświetl poprawny wynik, nie pobieraj nowego pytania
                    return;
                }
                // Po każdej błędnej odpowiedzi wyświetl komunikat, nie resetuj całej próby
                // Jeśli Question jest nullem, ustaw je na podstawie A i B
                if (Question == null)
                {
                    Question = new QuestionDto { A = A, B = B, Level = Level };
                }
            }
            if (Streak >= 3)
            {
                GameWon = true;
            }
        }
    }

    public class QuestionDto
    {
        public int A { get; set; }
        public int B { get; set; }
        public int Level { get; set; }
    }
    public class AnswerResultDto
    {
        public bool IsCorrect { get; set; }
        public int Correct { get; set; }
    }
}
