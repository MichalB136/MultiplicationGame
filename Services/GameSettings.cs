namespace MultiplicationGame.Services;

public sealed class GameSettings
{
    // Allowed difficulty levels (e.g. 60, 80, 100, 1000)
    // Do not set default arrays here so configuration can fully replace them.
    public int[]? Levels { get; set; }

    // Maximum multiplier to generate for non-1000 levels (default 10)
    public int DefaultMaxMultiplier { get; set; } = 10;

    // Factors that should appear less frequently
    public int[]? LowProbabilityFactors { get; set; }

    // Percentage chance (0-100) that a low-probability pair will be chosen when both groups exist
    public int LowFactorChancePercent { get; set; } = 10;

    // Number of correct answers in a row required to win the game
    public int RequiredCorrectAnswers { get; set; } = 10;
}
