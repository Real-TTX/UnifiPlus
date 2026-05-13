namespace UnifiPlus.Web.Models.Api;

public sealed class ApiCurrentUserResponse
{
    public string UserId { get; init; } = string.Empty;

    public string Role { get; init; } = string.Empty;

    public bool IsAdmin { get; init; }
}
