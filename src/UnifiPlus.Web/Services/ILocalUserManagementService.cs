using UnifiPlus.Web.Data;
using UnifiPlus.Web.Models;

namespace UnifiPlus.Web.Services;

public interface ILocalUserManagementService
{
    Task<IReadOnlyList<LocalUser>> GetUsersAsync(CancellationToken cancellationToken);

    Task<LocalUser> CreateUserAsync(string userId, string password, string role, CancellationToken cancellationToken);

    Task ChangeRoleAsync(string userId, string role, string actingUserId, CancellationToken cancellationToken);

    Task ResetPasswordAsync(string userId, string newPassword, CancellationToken cancellationToken);

    Task DeleteUserAsync(string userId, string actingUserId, CancellationToken cancellationToken);

    Task ChangeOwnPasswordAsync(string userId, string currentPassword, string newPassword, CancellationToken cancellationToken);

    Task<IReadOnlyDictionary<string, string>> GetClientAliasesAsync(string userId, CancellationToken cancellationToken);

    Task SetClientAliasAsync(string userId, string clientId, string alias, CancellationToken cancellationToken);

    Task<IReadOnlyList<RecoveredUserResult>> RecoverUsersFromUniFiAsync(IEnumerable<string> userIds, CancellationToken cancellationToken);
}
