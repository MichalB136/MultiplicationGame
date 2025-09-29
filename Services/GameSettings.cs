namespace MultiplicationGame.Services;

public sealed class GameSettings
{
    // Allowed difficulty levels (e.g. 60, 80, 100, 1000)
    public int[] Levels { get; set; } = new[] { 100 };

    // Maximum multiplier to generate for non-1000 levels (default 10)
    public int DefaultMaxMultiplier { get; set; } = 10;

    // Factors that should appear less frequently
    public int[] LowProbabilityFactors { get; set; } = new[] { 1, 2, 3, 4, 10 };

    // Percentage chance (0-100) that a low-probability pair will be chosen when both groups exist
    public int LowFactorChancePercent { get; set; } = 10;
}
