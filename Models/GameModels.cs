namespace MultiplicationGame.Models;

public sealed record QuestionDto(int A, int B, int Level);

public sealed record AnswerResultDto(bool IsCorrect, int Correct);

public sealed record HistoryEntryDto(string Text, bool IsCorrect);

// Full history entry used across pages and services (includes user answer and correct value)
public sealed record HistoryEntry(string Text, double Correct, string User, bool IsCorrect);