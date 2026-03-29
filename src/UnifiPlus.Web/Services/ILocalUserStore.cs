using UnifiPlus.Web.Data;

namespace UnifiPlus.Web.Services;

public interface ILocalUserStore
{
    string StoragePath { get; }

    Task<IReadOnlyList<LocalUser>> GetAllAsync(CancellationToken cancellationToken);

    Task<LocalUser?> FindByUserIdAsync(string userId, CancellationToken cancellationToken);

    Task<bool> HasAnyUsersAsync(CancellationToken cancellationToken);

    Task SaveAllAsync(IReadOnlyList<LocalUser> users, CancellationToken cancellationToken);
}
