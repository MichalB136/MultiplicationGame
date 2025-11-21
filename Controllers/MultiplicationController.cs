using Microsoft.AspNetCore.Mvc;
using MultiplicationGame.Models;
using MultiplicationGame.Services;

namespace MultiplicationGame.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MultiplicationController : ControllerBase
    {
        private readonly IGameService _gameService;
        private readonly ILogger<MultiplicationController> _logger;

        public MultiplicationController(IGameService gameService, ILogger<MultiplicationController> logger)
        {
            _gameService = gameService;
            _logger = logger;
        }

        [HttpGet("question")]
        public IActionResult GetQuestion([FromQuery] int level = 100)
        {
            _logger.LogInformation(
                "Question requested: Level={Level}, IP={IP}",
                level,
                HttpContext.Connection.RemoteIpAddress);

            // Pobierz listę rozwiązanych pytań z query string (przekazujemy dalej do GameService)
            var solvedRaw = Request.Query["solved"].ToString();

            try
            {
                var q = _gameService.GetQuestion(level, solvedRaw);
                if (q is null)
                {
                    _logger.LogWarning("GameService returned null question for level {Level}", level);
                    return Ok(new { a = 0, b = 0, level });
                }

                _logger.LogInformation("Question selected: {A} × {B} for level {Level}", q.A, q.B, q.Level);
                return Ok(new { a = q.A, b = q.B, level = q.Level });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid level requested: {Level}", level);
                return BadRequest("Nieprawidłowy poziom trudności.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error selecting question for level {Level}", level);
                return StatusCode(500, "Błąd serwera");
            }
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
            try
            {
                var result = _gameService.CheckAnswer(request.UserAnswer, request.A, request.B);
                _logger.LogInformation(
                    "Answer checked via GameService: {A} × {B} = {Correct}, User: {UserAnswer}, Result: {IsCorrect}",
                    request.A,
                    request.B,
                    result.Correct,
                    request.UserAnswer,
                    result.IsCorrect ? "✓" : "✗");

                return Ok(new { isCorrect = result.IsCorrect, correct = result.Correct });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking answer {A}×{B}", request.A, request.B);
                return StatusCode(500, "Błąd serwera");
            }
        }
    }
}
