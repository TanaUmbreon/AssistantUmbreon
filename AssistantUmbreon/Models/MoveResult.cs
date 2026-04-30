namespace AssistantUmbreon.Models;

public record MoveResult(
    bool IsSuccess,
    int MovedCount,
    string? ErrorMessage
);
