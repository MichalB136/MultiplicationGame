using Microsoft.AspNetCore.Mvc;

namespace MultiplicationGame.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MultiplicationController : ControllerBase
    {
    private static readonly int[] Levels = { 20, 40, 60, 80, 100, 1000 };
        private readonly Random _random = new();

        [HttpGet("question")]
        public IActionResult GetQuestion([FromQuery] int level = 100)
        {
            if (!Levels.Contains(level))
                return BadRequest("Nieprawidłowy poziom trudności.");

            // Pobierz listę rozwiązanych pytań z query string
            var solvedRaw = Request.Query["solved"].ToString();
            var solved = new HashSet<string>((solvedRaw ?? "").Split(';', StringSplitOptions.RemoveEmptyEntries));


            // Zakres liczb zależny od poziomu
            int maxNum = (level == 1000) ? 1000 : 10;
            var allQuestions = new List<(int a, int b)>();
            for (int i = 1; i <= maxNum; i++)
                for (int j = 1; j <= maxNum; j++)
                    if (i * j <= level)
                        allQuestions.Add((i, j));

            // Odfiltruj już rozwiązane
            var available = allQuestions.Where(q => !solved.Contains($"{q.a}-{q.b}")).ToList();
            if (available.Count == 0)
                return Ok(new { a = 0, b = 0, level }); // Brak pytań

            // Zmniejsz szansę na pojawienie się mnożenia przez 1
            // 80% szans na "normalne" pytanie, 20% na pytanie z 1
            var withOne = available.Where(q => q.a == 1 || q.b == 1).ToList();
            var withoutOne = available.Where(q => q.a != 1 && q.b != 1).ToList();
            int roll = _random.Next(100);
            (int a, int b) selected;
            if (withOne.Count > 0 && withoutOne.Count > 0)
            {
                if (roll < 20)
                {
                    // 20% szans na pytanie z 1
                    selected = withOne[_random.Next(withOne.Count)];
                }
                else
                {
                    // 80% szans na pytanie bez 1
                    selected = withoutOne[_random.Next(withoutOne.Count)];
                }
            }
            else
            {
                // Jeśli nie ma wyboru, losuj z dostępnych
                selected = available[_random.Next(available.Count)];
            }
            var (a, b) = selected;
            return Ok(new { a, b, level });
        }

        public class AnswerRequest
        {
            public int A { get; set; }
            public int B { get; set; }
            public int UserAnswer { get; set; }
        }

        [HttpPost("answer")]
        public IActionResult CheckAnswer([FromBody] AnswerRequest request)
        {
            int correct = request.A * request.B;
            bool isCorrect = request.UserAnswer == correct;
            return Ok(new { isCorrect, correct });
        }
    }
}
