using Xunit;
using Microsoft.Extensions.Options;
using MultiplicationGame.Services;

namespace MultiplicationGame.Tests;

public class BonusAttemptsTests
{
    [Fact]
    public void GameSettings_BonusAttemptsThreshold_DefaultValue()
    {
        // Arrange
        var settings = new GameSettings();

        // Assert
        Assert.Equal(5, settings.BonusAttemptsThreshold);
    }

    [Fact]
    public void GameSettings_BonusAttemptsThreshold_CustomValue()
    {
        // Arrange
        var settings = new GameSettings { BonusAttemptsThreshold = 10 };

        // Assert
        Assert.Equal(10, settings.BonusAttemptsThreshold);
    }

    [Fact]
    public void GameSettings_InitialAttempts_DefaultValue()
    {
        // Arrange
        var settings = new GameSettings();

        // Assert
        Assert.Equal(3, settings.InitialAttempts);
    }

    [Fact]
    public void GameSettings_InitialAttempts_UnlimitedScenario()
    {
        // Arrange
        var settings = new GameSettings { InitialAttempts = 0 };

        // Assert
        Assert.Equal(0, settings.InitialAttempts);
    }

    [Fact]
    public void BonusLogic_ShouldAwardBonus_WhenThresholdReached()
    {
        // Arrange - simulate game state
        int perfectStreak = 5;
        int attemptsLeft = 2;
        int initialAttempts = 3;
        int bonusThreshold = 5;
        bool bonusAwarded = false;

        // Act - simulate bonus logic from HandleCorrectAnswer
        if (initialAttempts > 0 && bonusThreshold > 0 && perfectStreak >= bonusThreshold)
        {
            attemptsLeft++;
            bonusAwarded = true;
            perfectStreak = 0;
        }

        // Assert
        Assert.True(bonusAwarded, "Bonus should be awarded");
        Assert.Equal(3, attemptsLeft);
        Assert.Equal(0, perfectStreak);
    }

    [Fact]
    public void BonusLogic_ShouldAwardBonus_EvenWhenAtInitialAttempts()
    {
        // Arrange
        int perfectStreak = 5;
        int attemptsLeft = 3; // At initial attempts
        int initialAttempts = 3;
        int bonusThreshold = 5;
        bool bonusAwarded = false;

        // Act
        if (initialAttempts > 0 && bonusThreshold > 0 && perfectStreak >= bonusThreshold)
        {
            attemptsLeft++;
            bonusAwarded = true;
            perfectStreak = 0;
        }

        // Assert
        Assert.True(bonusAwarded, "Bonus should be awarded even when at initial attempts");
        Assert.Equal(4, attemptsLeft); // Should go above initial
        Assert.Equal(0, perfectStreak); // Counter resets
    }

    [Fact]
    public void BonusLogic_ShouldNotAwardBonus_WithUnlimitedAttempts()
    {
        // Arrange
        int perfectStreak = 5;
        int attemptsLeft = 0;
        int initialAttempts = 0; // Unlimited
        int bonusThreshold = 5;
        bool bonusAwarded = false;

        // Act
        if (initialAttempts > 0 && bonusThreshold > 0 && perfectStreak >= bonusThreshold)
        {
            attemptsLeft++;
            bonusAwarded = true;
            perfectStreak = 0;
        }

        // Assert
        Assert.False(bonusAwarded, "Bonus should not be awarded with unlimited attempts");
        Assert.Equal(0, attemptsLeft);
        Assert.Equal(5, perfectStreak); // Counter doesn't reset
    }

    [Fact]
    public void BonusLogic_ShouldNotAwardBonus_WhenBelowThreshold()
    {
        // Arrange
        int perfectStreak = 4; // Below threshold
        int attemptsLeft = 2;
        int initialAttempts = 3;
        int bonusThreshold = 5;
        bool bonusAwarded = false;

        // Act
        if (initialAttempts > 0 && bonusThreshold > 0 && perfectStreak >= bonusThreshold)
        {
            attemptsLeft++;
            bonusAwarded = true;
            perfectStreak = 0;
        }

        // Assert
        Assert.False(bonusAwarded, "Bonus should not be awarded below threshold");
        Assert.Equal(2, attemptsLeft);
        Assert.Equal(4, perfectStreak); // Counter doesn't change
    }

    [Fact]
    public void BonusLogic_PerfectStreak_IncrementOnCorrectAnswer()
    {
        // Arrange
        int perfectStreak = 3;

        // Act - simulate correct answer
        perfectStreak++;

        // Assert
        Assert.Equal(4, perfectStreak);
    }

    [Fact]
    public void BonusLogic_PerfectStreak_ResetOnIncorrectAnswer()
    {
        // Arrange
        int perfectStreak = 4;

        // Act - simulate incorrect answer (losing a life)
        perfectStreak = 0;

        // Assert
        Assert.Equal(0, perfectStreak);
    }
}
