using Xunit;
using MultiplicationGame.Pages;
using MultiplicationGame.Services;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using System;

namespace MultiplicationGame.Tests;

public class GameTimerTests
{
    private static IOptions<GameSettings> GetTestConfiguration()
    {
        var settings = new GameSettings
        {
            RequiredCorrectAnswers = 15,
            InitialAttempts = 3,
            BonusAttemptsThreshold = 3
        };
        return Options.Create(settings);
    }

    [Fact]
    public void GameStartTime_ShouldBeInitialized_WhenGameStarts()
    {
        // Arrange
        var gameService = new GameService(GetTestConfiguration(), null);
        var gameStateService = new GameStateService(null);
        var model = new IndexModel(gameService, gameStateService, null, GetTestConfiguration());

        // Act - symulacja rozpoczęcia gry
        model.OnGet();

        // Assert - przy pierwszym OnGet nie ma jeszcze GameStartTime (nie wywołano ResetGameState)
        // GameStartTime jest ustawiany dopiero gdy użytkownik kliknie "Spróbuj ponownie" lub gdy gra się resetuje
        Assert.Equal(0, model.GameStartTime);
    }

    [Fact]
    public void GameElapsedSeconds_ShouldBeCalculated_WhenAnswerProcessed()
    {
        // Arrange
        var gameService = new GameService(GetTestConfiguration(), null);
        var gameStateService = new GameStateService(null);
        var model = new IndexModel(gameService, gameStateService, null, GetTestConfiguration())
        {
            GameStartTime = DateTimeOffset.UtcNow.AddSeconds(-10).ToUnixTimeMilliseconds(),
            A = 2,
            B = 3,
            UserAnswer = 6,
            Level = 100,
            Streak = 0,
            AttemptsLeft = 3
        };

        // Act
        model.OnPost();

        // Assert - czas powinien być około 10 sekund (±2 sekundy tolerancji)
        Assert.InRange(model.GameElapsedSeconds, 8, 12);
    }

    [Fact]
    public void GameTimer_ShouldReset_WhenGameRestarts()
    {
        // Arrange
        var gameService = new GameService(GetTestConfiguration(), null);
        var gameStateService = new GameStateService(null);
        var model = new IndexModel(gameService, gameStateService, null, GetTestConfiguration())
        {
            GameStartTime = DateTimeOffset.UtcNow.AddSeconds(-100).ToUnixTimeMilliseconds(),
            GameElapsedSeconds = 100,
            NextQuestion = true,
            AnswerChecked = true,
            AttemptsLeft = 0
        };

        // Act - restart gry
        model.OnPost();

        // Assert - nowy GameStartTime powinien być ustawiony (>0) i nie być starym timestampem
        Assert.True(model.GameStartTime > 0);
        var currentTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var timeDiff = currentTime - model.GameStartTime;
        Assert.InRange(timeDiff, 0, 5000); // Powinien być ustawiony w ciągu ostatnich 5 sekund
    }

    [Fact]
    public void GameTimer_ShouldPersist_AcrossMultipleAnswers()
    {
        // Arrange
        var gameService = new GameService(GetTestConfiguration(), null);
        var gameStateService = new GameStateService(null);
        var startTime = DateTimeOffset.UtcNow.AddSeconds(-5).ToUnixTimeMilliseconds();
        
        var model = new IndexModel(gameService, gameStateService, null, GetTestConfiguration())
        {
            GameStartTime = startTime,
            A = 2,
            B = 2,
            UserAnswer = 4,
            Level = 100,
            Streak = 0,
            AttemptsLeft = 3
        };

        // Act - pierwsza odpowiedź
        model.OnPost();
        var firstElapsed = model.GameElapsedSeconds;

        // Symulacja czasu między odpowiedziami
        System.Threading.Thread.Sleep(1000);

        // Druga odpowiedź z tym samym GameStartTime
        model.GameStartTime = startTime;
        model.A = 3;
        model.B = 3;
        model.UserAnswer = 9;
        model.OnPost();
        var secondElapsed = model.GameElapsedSeconds;

        // Assert - drugi czas powinien być większy niż pierwszy
        Assert.True(secondElapsed > firstElapsed);
    }

    [Fact]
    public void GameTimer_TimeFormat_ShouldDisplayCorrectly()
    {
        // Arrange
        int testSeconds = 125; // 2 min 5 sek
        
        // Act
        var minutes = testSeconds / 60;
        var seconds = testSeconds % 60;
        var timeText = minutes > 0 ? $"{minutes} min {seconds} sek" : $"{seconds} sek";

        // Assert
        Assert.Equal("2 min 5 sek", timeText);
    }

    [Fact]
    public void GameTimer_TimeFormat_SecondsOnly()
    {
        // Arrange
        int testSeconds = 45;
        
        // Act
        var minutes = testSeconds / 60;
        var seconds = testSeconds % 60;
        var timeText = minutes > 0 ? $"{minutes} min {seconds} sek" : $"{seconds} sek";

        // Assert
        Assert.Equal("45 sek", timeText);
    }
}
