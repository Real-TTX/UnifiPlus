namespace UnifiPlus.Web.Models;

public sealed class ActiveRulesPageViewModel
{
    public IReadOnlyList<ActiveRuleViewModel> Rules { get; init; } = [];

    public string? StatusMessage { get; init; }

    public bool StatusIsSuccess { get; init; }
}
