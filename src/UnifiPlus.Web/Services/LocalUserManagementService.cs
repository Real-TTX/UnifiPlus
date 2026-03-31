using Microsoft.AspNetCore.Identity;
using UnifiPlus.Web.Authorization;
using UnifiPlus.Web.Data;
using UnifiPlus.Web.Models;

namespace UnifiPlus.Web.Services;

public sealed class LocalUserManagementService : ILocalUserManagementService
{
    private readonly ILocalUserStore _localUserStore;
    private readonly PasswordHasher<LocalUser> _passwordHasher = new();

    public LocalUserManagementService(ILocalUserStore localUserStore)
    {
        _localUserStore = localUserStore;
    }

    public Task<IReadOnlyList<LocalUser>> GetUsersAsync(CancellationToken cancellationToken)
    {
        return _localUserStore.GetAllAsync(cancellationToken);
    }

    public async Task<LocalUser> CreateUserAsync(string userId, string password, string role, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new InvalidOperationException("A user id is required.");
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException("A password is required.");
        }

        var users = (await _localUserStore.GetAllAsync(cancellationToken)).ToList();
        if (users.Any(user => string.Equals(user.UserId, userId, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException("That user id already exists.");
        }

        var normalizedRole = NormalizeRole(role);
        var user = new LocalUser
        {
            UserId = userId.Trim(),
            Role = normalizedRole,
            CreatedUtc = DateTimeOffset.UtcNow
        };

        user.PasswordHash = _passwordHasher.HashPassword(user, password);
        users.Add(user);
        await _localUserStore.SaveAllAsync(users, cancellationToken);
        return user;
    }

    public async Task ChangeRoleAsync(string userId, string role, string actingUserId, CancellationToken cancellationToken)
    {
        var users = (await _localUserStore.GetAllAsync(cancellationToken)).ToList();
        var user = users.FirstOrDefault(item => string.Equals(item.UserId, userId, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException("The selected user could not be found.");

        var normalizedRole = NormalizeRole(role);
        if (string.Equals(user.UserId, actingUserId, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(normalizedRole, AppRoles.Admin, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("You cannot remove your own admin role.");
        }

        if (string.Equals(user.Role, AppRoles.Admin, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(normalizedRole, AppRoles.Admin, StringComparison.OrdinalIgnoreCase) &&
            users.Count(item => string.Equals(item.Role, AppRoles.Admin, StringComparison.OrdinalIgnoreCase)) <= 1)
        {
            throw new InvalidOperationException("At least one administrator must remain.");
        }

        user.Role = normalizedRole;
        await _localUserStore.SaveAllAsync(users, cancellationToken);
    }

    public async Task ResetPasswordAsync(string userId, string newPassword, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(newPassword))
        {
            throw new InvalidOperationException("A new password is required.");
        }

        var users = (await _localUserStore.GetAllAsync(cancellationToken)).ToList();
        var user = users.FirstOrDefault(item => string.Equals(item.UserId, userId, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException("The selected user could not be found.");

        user.PasswordHash = _passwordHasher.HashPassword(user, newPassword);
        await _localUserStore.SaveAllAsync(users, cancellationToken);
    }

    public async Task DeleteUserAsync(string userId, string actingUserId, CancellationToken cancellationToken)
    {
        var users = (await _localUserStore.GetAllAsync(cancellationToken)).ToList();
        var user = users.FirstOrDefault(item => string.Equals(item.UserId, userId, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException("The selected user could not be found.");

        if (string.Equals(user.UserId, actingUserId, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("You cannot delete your own account.");
        }

        if (string.Equals(user.Role, AppRoles.Admin, StringComparison.OrdinalIgnoreCase) &&
            users.Count(item => string.Equals(item.Role, AppRoles.Admin, StringComparison.OrdinalIgnoreCase)) <= 1)
        {
            throw new InvalidOperationException("At least one administrator must remain.");
        }

        users.Remove(user);
        await _localUserStore.SaveAllAsync(users, cancellationToken);
    }

    public async Task ChangeOwnPasswordAsync(string userId, string currentPassword, string newPassword, CancellationToken cancellationToken)
    {
        var users = (await _localUserStore.GetAllAsync(cancellationToken)).ToList();
        var user = users.FirstOrDefault(item => string.Equals(item.UserId, userId, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException("Your user account could not be found.");

        var verification = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, currentPassword);
        if (verification == PasswordVerificationResult.Failed)
        {
            throw new InvalidOperationException("The current password is incorrect.");
        }

        user.PasswordHash = _passwordHasher.HashPassword(user, newPassword);
        await _localUserStore.SaveAllAsync(users, cancellationToken);
    }

    public async Task<IReadOnlyDictionary<string, string>> GetClientAliasesAsync(string userId, CancellationToken cancellationToken)
    {
        var user = await _localUserStore.FindByUserIdAsync(userId, cancellationToken);
        if (user is null || user.ClientAliases.Count == 0)
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        return new Dictionary<string, string>(user.ClientAliases, StringComparer.OrdinalIgnoreCase);
    }

    public async Task SetClientAliasAsync(string userId, string clientId, string alias, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new InvalidOperationException("No user is signed in.");
        }

        if (string.IsNullOrWhiteSpace(clientId))
        {
            throw new InvalidOperationException("No client id was provided.");
        }

        var users = (await _localUserStore.GetAllAsync(cancellationToken)).ToList();
        var user = users.FirstOrDefault(item => string.Equals(item.UserId, userId, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException("Your user account could not be found.");

        var normalizedAlias = alias.Trim();
        if (string.IsNullOrWhiteSpace(normalizedAlias))
        {
            user.ClientAliases.Remove(clientId);
        }
        else
        {
            user.ClientAliases[clientId] = normalizedAlias;
        }

        await _localUserStore.SaveAllAsync(users, cancellationToken);
    }

    public async Task<IReadOnlyList<RecoveredUserResult>> RecoverUsersFromUniFiAsync(IEnumerable<string> userIds, CancellationToken cancellationToken)
    {
        var normalizedUserIds = userIds
            .Select(userId => userId.Trim())
            .Where(userId => !string.IsNullOrWhiteSpace(userId))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(userId => userId, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var users = (await _localUserStore.GetAllAsync(cancellationToken)).ToList();
        var results = new List<RecoveredUserResult>();

        foreach (var userId in normalizedUserIds)
        {
            var existingUser = users.FirstOrDefault(item => string.Equals(item.UserId, userId, StringComparison.OrdinalIgnoreCase));
            if (existingUser is not null)
            {
                results.Add(new RecoveredUserResult
                {
                    UserId = existingUser.UserId,
                    Role = existingUser.Role,
                    TemporaryPassword = string.Empty,
                    AlreadyExisted = true
                });
                continue;
            }

            var recoveredUser = new LocalUser
            {
                UserId = userId,
                Role = AppRoles.User,
                CreatedUtc = DateTimeOffset.UtcNow
            };

            var temporaryPassword = $"UP-{Guid.NewGuid():N}"[..14] + "!";
            recoveredUser.PasswordHash = _passwordHasher.HashPassword(recoveredUser, temporaryPassword);
            users.Add(recoveredUser);

            results.Add(new RecoveredUserResult
            {
                UserId = recoveredUser.UserId,
                Role = recoveredUser.Role,
                TemporaryPassword = temporaryPassword,
                AlreadyExisted = false
            });
        }

        await _localUserStore.SaveAllAsync(users, cancellationToken);
        return results;
    }

    private static string NormalizeRole(string role)
    {
        return string.Equals(role, AppRoles.Admin, StringComparison.OrdinalIgnoreCase)
            ? AppRoles.Admin
            : AppRoles.User;
    }
}
