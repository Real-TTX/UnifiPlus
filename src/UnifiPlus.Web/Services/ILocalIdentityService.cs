using UnifiPlus.Web.Data;

namespace UnifiPlus.Web.Services;

public interface ILocalIdentityService
{
    Task<LocalUser?> ValidateAsync(string userId, string password, CancellationToken cancellationToken);
}
