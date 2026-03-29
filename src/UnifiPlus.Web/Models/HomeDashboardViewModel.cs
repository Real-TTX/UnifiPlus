namespace UnifiPlus.Web.Models;

public sealed class HomeDashboardViewModel
{
    public string UserId { get; init; } = string.Empty;

    public bool IsAdmin { get; init; }

    public int UplinkCount { get; init; }

    public int AssignedClientCount { get; init; }

    public int TotalClientCount { get; init; }

    public int ActiveRuleCount { get; init; }
}
