namespace UnifiPlus.Web.Models;

public sealed class RecoveredUserResult
{
    public string UserId { get; init; } = string.Empty;

    public string Role { get; init; } = string.Empty;

    public string TemporaryPassword { get; init; } = string.Empty;

    public bool AlreadyExisted { get; init; }
}
