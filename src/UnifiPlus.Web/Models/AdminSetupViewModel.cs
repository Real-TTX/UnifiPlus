namespace UnifiPlus.Web.Models;

public sealed class AdminSetupViewModel
{
    public AdminUniFiSetupRequest Form { get; init; } = new();

    public string StoragePath { get; init; } = string.Empty;

    public bool HasSavedConfiguration { get; init; }

    public DateTimeOffset? LastUpdatedUtc { get; init; }

    public UniFiConnectionTestResult? LastTest { get; init; }

    public bool HasStoredApiKey { get; init; }

    public bool HasStoredPassword { get; init; }

    public string? StatusMessage { get; init; }

    public bool StatusIsSuccess { get; init; }

    public UniFiRecoveryResult? RecoveryResult { get; init; }
}
