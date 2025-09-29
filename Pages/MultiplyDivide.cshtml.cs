using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MultiplicationGame.Services;

namespace MultiplicationGame.Pages;

public class MultiplyDivideModel : PageModel
{
    private readonly IGameService _gameService;

    public MultiplyDivideModel(IGameService gameService)
    {
        _gameService = gameService;
    }

    [BindProperty]
    public string Operation { get; set; } = "Multiply"; // Multiply or Divide
    [BindProperty]
    public string ViewMode { get; set; } = "Classic"; // Classic or Vertical
    [BindProperty]
    public int MaxFactor { get; set; } = 10;

    [BindProperty]
    public int QuestionA { get; set; }
    [BindProperty]
    public int QuestionB { get; set; }
    [BindProperty]
    public int UserAnswer { get; set; }

    public bool Checked { get; set; }
    public bool IsCorrect { get; set; }
    public int CorrectAnswer { get; set; }

    public void OnGet()
    {
        // Nothing special on GET
    }

    public void OnPost(bool New = false)
    {
        if (New || (QuestionA == 0 && QuestionB == 0))
        {
            GenerateQuestion();
            Checked = false;
            UserAnswer = 0;
            return;
        }

        // Evaluate
        if (Operation == "Multiply")
        {
            CorrectAnswer = QuestionA * QuestionB;
            IsCorrect = UserAnswer == CorrectAnswer;
        }
        else // Divide (integer division check)
        {
            if (QuestionB == 0) { CorrectAnswer = 0; IsCorrect = false; }
            else { CorrectAnswer = QuestionA / QuestionB; IsCorrect = UserAnswer == CorrectAnswer; }
        }
        Checked = true;
    }

    private void GenerateQuestion()
    {
        var rnd = new Random();
        if (Operation == "Divide")
        {
            // For division, create A as product so division is exact
            var b = rnd.Next(1, Math.Max(2, MaxFactor + 1));
            var a = b * rnd.Next(1, Math.Max(2, MaxFactor + 1));
            QuestionA = a;
            QuestionB = b;
        }
        else
        {
            QuestionA = rnd.Next(1, Math.Max(2, MaxFactor + 1));
            QuestionB = rnd.Next(1, Math.Max(2, MaxFactor + 1));
        }
    }
}
