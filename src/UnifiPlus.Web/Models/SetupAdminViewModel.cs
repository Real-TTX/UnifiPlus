namespace UnifiPlus.Web.Models;

public sealed class SetupAdminViewModel
{
    public CreateAdminRequest Form { get; init; } = new();

    public string UserStorePath { get; init; } = string.Empty;

    public string ConnectionStorePath { get; init; } = string.Empty;
}
