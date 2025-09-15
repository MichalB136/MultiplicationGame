namespace MultiplicationGame.Models;

public sealed record QuestionDto(int A, int B, int Level);

public sealed record AnswerResultDto(bool IsCorrect, int Correct);

public sealed record HistoryEntryDto(string Text, bool IsCorrect);