using Microsoft.AspNetCore.Mvc;
using MultiplicationGame.Services;
using System.Linq;
using System.Collections.Generic;

namespace MultiplicationGame.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class ProbabilityController(
    IGameService gameService,
    Microsoft.Extensions.Options.IOptions<GameSettings> options,
    ILogger<ProbabilityController> logger) : ControllerBase
{
    private readonly IGameService _gameService = gameService;
    private readonly GameSettings _settings = options?.Value ?? new GameSettings();
    private readonly ILogger<ProbabilityController> _logger = logger;

    [HttpGet("sample")]
    public IActionResult Sample([FromQuery] int level = 100, [FromQuery] int iterations = 1000)
    {
        if (iterations <= 0 || iterations > 20000)
        {
            return BadRequest("Iterations must be between 1 and 20000.");
        }

        var levels = _settings.Levels ?? System.Array.Empty<int>();
        if (!levels.Contains(level))
        {
            return BadRequest($"Invalid level: {level}. Available: {string.Join(", ", levels)}");
        }

        var lowFactors = new HashSet<int>(_settings.LowProbabilityFactors ?? System.Array.Empty<int>());
        var configuredChance = Math.Clamp(_settings.LowFactorChancePercent, 0, 100);

        var lowCount = 0;
        for (var i = 0; i < iterations; i++)
        {
            // We don't track solved questions here to keep pool size constant for unbiased sampling
            var q = _gameService.GetQuestion(level, string.Empty);
            if (lowFactors.Contains(q.A) || lowFactors.Contains(q.B))
            {
                lowCount++;
            }
        }

        var lowPercent = iterations == 0 ? 0 : (double)lowCount / iterations * 100.0;
        var desiredCount = iterations - lowCount;

        _logger.LogInformation(
            "Probability sample completed: Level={Level}, Iterations={Iterations}, ConfigChance={ConfigChance}%, ObservedLow={LowCount} ({ObservedPercent:F2}%)",
            level,
            iterations,
            configuredChance,
            lowCount,
            lowPercent);

        return Ok(new
        {
            level,
            iterations,
            lowProbabilityFactors = lowFactors.ToArray(),
            configuredChancePercent = configuredChance,
            observedLowFactorCount = lowCount,
            observedLowFactorPercent = lowPercent,
            observedDesiredCount = desiredCount
        });
    }

    [HttpGet("pair-stats")]
    public IActionResult PairStats([FromQuery] int level = 100, [FromQuery] int iterations = 10000)
    {
        if (iterations <= 0 || iterations > 20000)
            return BadRequest("Iterations must be between 1 and 20000.");

        var levels = _settings.Levels ?? System.Array.Empty<int>();
        if (!levels.Contains(level))
            return BadRequest($"Invalid level: {level}. Available: {string.Join(", ", levels)}");

        var pairStats = new Dictionary<(int a, int b), (int total, int aFirst, int bFirst)>();

        for (var i = 0; i < iterations; i++)
        {
            var q = _gameService.GetQuestion(level, string.Empty);
            var a = q.A;
            var b = q.B;
            var key = a <= b ? (a, b) : (b, a);

            if (!pairStats.TryGetValue(key, out var stats))
                stats = (0, 0, 0);

            stats.total++;
            if (a <= b)
                stats.aFirst++;
            else
                stats.bFirst++;

            pairStats[key] = stats;
        }

        var bothOrientations = pairStats.Count(p => p.Value.aFirst > 0 && p.Value.bFirst > 0);
        var singleOrientation = pairStats.Count - bothOrientations;

        var topPairs = pairStats
            .OrderByDescending(p => p.Value.total)
            .Take(20)
            .Select(p => new
            {
                pair = $"{p.Key.a}x{p.Key.b}",
                total = p.Value.total,
                aFirst = p.Value.aFirst,
                bFirst = p.Value.bFirst
            })
            .ToArray();

        return Ok(new
        {
            level,
            iterations,
            unorderedPairsObserved = pairStats.Count,
            bothOrientations,
            singleOrientation,
            topPairs
        });
    }
}
