using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MultiplicationGame.Services;
using System.Linq;

namespace MultiplicationGame.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class DiagnosticsController(
    IOptions<GameSettings> options,
    ILogger<DiagnosticsController> logger) : ControllerBase
{
    private readonly GameSettings _settings = options?.Value ?? new GameSettings();
    private readonly ILogger<DiagnosticsController> _logger = logger;

    [HttpGet("gamesettings")]
    public IActionResult GetGameSettings()
    {
        _logger.LogDebug("Returning GameSettings for diagnostics");

        var levels = (_settings.Levels ?? System.Array.Empty<int>())
            .Distinct()
            .OrderBy(x => x)
            .ToArray();

        var lowFactors = (_settings.LowProbabilityFactors ?? System.Array.Empty<int>())
            .Distinct()
            .OrderBy(x => x)
            .ToArray();

        return Ok(new
        {
            Levels = levels,
            DefaultMaxMultiplier = _settings.DefaultMaxMultiplier,
            LowProbabilityFactors = lowFactors,
            LowFactorChancePercent = _settings.LowFactorChancePercent,
            RequiredCorrectAnswers = _settings.RequiredCorrectAnswers
        });
    }
}
