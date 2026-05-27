namespace IconSwapperGui.Core.Models;

public sealed record IconOperationResult(
    bool Succeeded,
    string Title,
    string Message,
    string? Detail = null)
{
    public static IconOperationResult Success(string title, string message)
        => new(true, title, message);

    public static IconOperationResult Failure(string title, string message, string? detail = null)
        => new(false, title, message, detail);
}
