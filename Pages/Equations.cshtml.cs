using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MultiplicationGame.Pages;

public class EquationsModel : PageModel
{
    private readonly ILogger<EquationsModel> _logger;
    private readonly IConfiguration _configuration;

    public EquationsModel(ILogger<EquationsModel> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    [BindProperty]
    public string Difficulty { get; set; } = "2"; // default to "2" (Klasa 2). Other valid value: "3"

    public int MaxRepetitions => _configuration.GetValue<int>("GameSettings:EquationsMaxRepetitions", 0); // 0 = bez limitu, >0 = maksymalna liczba powtórzeń tego samego równania

    [BindProperty]
    public List<EquationItem> Equations { get; set; } = new();

    [BindProperty]
    public List<string>? Answers { get; set; }

    public int? Score { get; set; }

    public void OnGet()
    {
        // default generate
        GenerateEquations();
    }

    public IActionResult OnPostGenerate()
    {
        GenerateEquations();
        return Page();
    }

    public IActionResult OnPostSubmit()
    {
        _logger.LogInformation("OnPostSubmit called. Answers count: {Count}, Equations count: {EqCount}", 
            Answers?.Count ?? 0, Equations?.Count ?? 0);
        
        // Equations are already bound from hidden fields via [BindProperty]
        if (Answers == null || Equations == null || Equations.Count == 0)
        {
            _logger.LogWarning("OnPostSubmit: Missing data - Answers or Equations null/empty");
            Score = 0;
            return Page();
        }

        int correct = 0;
        for (int i = 0; i < Equations.Count; i++)
        {
            var eq = Equations[i];
            if (i < Answers.Count)
            {
                var answerStr = Answers[i]?.Replace(',', '.');
                if (double.TryParse(answerStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var user))
                {
                    eq.UserAnswer = user;
                    eq.IsCorrect = Math.Abs(user - eq.Answer) < 0.0001;
                    if (eq.IsCorrect == true) correct++;
                    _logger.LogInformation("Equation {Index}: {Text} = {Answer}, User: {UserAnswer}, Correct: {IsCorrect}", 
                        i, eq.Text, eq.Answer, user, eq.IsCorrect);
                }
                else
                {
                    eq.UserAnswer = null;
                    eq.IsCorrect = false;
                    _logger.LogWarning("Equation {Index}: Failed to parse answer '{Answer}'", i, answerStr);
                }
            }
        }

        Score = correct;
        _logger.LogInformation("OnPostSubmit complete. Score: {Score}/{Total}", Score, Equations.Count);
        return Page();
    }

    private void GenerateEquations(bool replace = true)
    {
        var rng = new Random();
        var list = new List<EquationItem>(20);
        var equationCounts = new Dictionary<string, int>();
        
        // Ensure Difficulty has a valid value
        var difficulty = Difficulty ?? "2";

        for (int i = 0; i < 20; i++)
        {
            string text;
            double ans = 0;
            int attempts = 0;
            const int maxAttempts = 100;
            
            do
            {
                var op = rng.Next(4); // 0:+ 1:- 2:* 3:/
                int a = 0, b = 1;

            if (difficulty == "2")
            {
                // simpler ranges
                switch (op)
                {
                    case 0: // add
                        a = rng.Next(0, 51);
                        b = rng.Next(0, 51);
                        break;
                    case 1: // sub
                        a = rng.Next(0, 51);
                        b = rng.Next(0, a + 1);
                        break;
                    case 2: // mul
                        a = rng.Next(1, 6);
                        b = rng.Next(1, 11);
                        break;
                    case 3: // div - ensure integer
                        b = rng.Next(1, 6);
                        var prod = rng.Next(1, 11);
                        a = b * prod;
                        break;
                }
            }
            else
            {
                // class 3 - larger ranges
                switch (op)
                {
                    case 0:
                        a = rng.Next(0, 201);
                        b = rng.Next(0, 201);
                        break;
                    case 1:
                        a = rng.Next(0, 201);
                        b = rng.Next(0, a + 1);
                        break;
                    case 2:
                        a = rng.Next(2, 13);
                        b = rng.Next(2, 13);
                        break;
                    case 3:
                        b = rng.Next(2, 13);
                        var prod = rng.Next(1, 21);
                        a = b * prod;
                        break;
                }
            }

                text = string.Empty;
                switch (op)
                {
                    case 0:
                        ans = a + b;
                        text = $"{a} + {b}";
                        break;
                    case 1:
                        ans = a - b;
                        text = $"{a} - {b}";
                        break;
                    case 2:
                        ans = a * b;
                        text = $"{a} × {b}";
                        break;
                    case 3:
                        ans = (double)a / b;
                        text = $"{a} ÷ {b}";
                        break;
                }
                
                attempts++;
                
                // Sprawdź czy równanie spełnia warunek powtórzeń
                if (MaxRepetitions == 0) // bez limitu
                    break;
                    
                if (!equationCounts.ContainsKey(text) || equationCounts[text] < MaxRepetitions)
                    break;
                    
            } while (attempts < maxAttempts);
            
            // Dodaj równanie do słownika i listy
            if (!equationCounts.ContainsKey(text))
                equationCounts[text] = 0;
            equationCounts[text]++;
            
            list.Add(new EquationItem { Text = text, Answer = ans });
        }

        if (replace)
            Equations = list;
        else if (Equations == null || Equations.Count == 0)
            Equations = list;
    }

    public class EquationItem
    {
        public string Text { get; set; } = string.Empty;
        public double Answer { get; set; }
        public double? UserAnswer { get; set; }
        public bool? IsCorrect { get; set; }
    }
}
