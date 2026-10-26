namespace MultiplicationGame.Models;

public sealed record QuestionDto(int A, int B, int Level);

public sealed record AnswerResultDto(bool IsCorrect, int Correct);

public sealed record HistoryEntryDto(string Text, bool IsCorrect);

public sealed record TrainingQuestionDto(int A, int B);

public sealed record TrainingAnswerDto(
    TrainingQuestionDto Question,
    int UserAnswer,
    bool IsCorrect,
    bool UsedHint);

public sealed record TrainingFactProgress(
    int Attempts,
    int Correct,
    int AssistedAnswers);

public sealed record TrainingSessionState(
    List<TrainingQuestionDto> Questions,
    List<TrainingAnswerDto> Answers,
    List<TrainingQuestionDto> Mistakes,
    long StartedAtUnixMilliseconds,
    long? CompletedAtUnixMilliseconds = null,
    Guid RoundId = default,
    bool IsAdaptive = false,
    bool HintsEnabled = false);

// Full history entry used across pages and services (includes user answer and correct value)
public sealed record HistoryEntry(string Text, double Correct, string User, bool IsCorrect);