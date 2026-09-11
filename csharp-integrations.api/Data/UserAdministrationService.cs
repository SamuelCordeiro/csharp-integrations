using Microsoft.EntityFrameworkCore;

namespace csharp_integrations.api.Data;

/// <summary>
/// Queries users for identity administration.
/// </summary>
public sealed class UserAdministrationService(ApplicationDbContext database)
{
    /// <summary>
    /// Gets a paged collection of users.
    /// </summary>
    /// <param name="query">Pagination and filter options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A paged collection of users.</returns>
    public async Task<UserAdministrationPage> GetUsersAsync(
        UserAdministrationQuery query,
        CancellationToken cancellationToken = default)
    {
        ValidateQuery(query);

        var users = CreateFilteredQuery(query);
        var totalCount = await users.CountAsync(cancellationToken);
        var pageUsers = await users
            .OrderBy(user => user.UserName)
            .ThenBy(user => user.Id)
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);
        var rolesByUserId = await GetRolesByUserIdAsync(pageUsers.Select(user => user.Id), cancellationToken);

        return new UserAdministrationPage
        {
            PageNumber = query.PageNumber,
            PageSize = query.PageSize,
            TotalCount = totalCount,
            Users = pageUsers.Select(user => CreateUser(user, rolesByUserId)).ToList()
        };
    }

    /// <summary>
    /// Gets one user by identifier.
    /// </summary>
    /// <param name="userId">User identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The user when found; otherwise, <see langword="null"/>.</returns>
    public async Task<UserAdministrationUser?> GetUserAsync(int userId, CancellationToken cancellationToken = default)
    {
        var user = await database.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(user => user.Id == userId, cancellationToken);

        if (user is null)
        {
            return null;
        }

        var rolesByUserId = await GetRolesByUserIdAsync([userId], cancellationToken);
        return CreateUser(user, rolesByUserId);
    }

    private IQueryable<ApplicationUser> CreateFilteredQuery(UserAdministrationQuery query)
    {
        var users = database.Users.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Username))
        {
            var username = query.Username.Trim();
            users = users.Where(user => user.UserName != null && user.UserName.Contains(username));
        }

        if (!string.IsNullOrWhiteSpace(query.Role))
        {
            var normalizedRole = query.Role.Trim().ToUpperInvariant();
            users = users.Where(user => database.UserRoles.Any(assignment =>
                assignment.UserId == user.Id
                && database.Roles.Any(role =>
                    role.Id == assignment.RoleId
                    && role.NormalizedName == normalizedRole)));
        }

        return users;
    }

    private async Task<IReadOnlyDictionary<int, IReadOnlyList<string>>> GetRolesByUserIdAsync(
        IEnumerable<int> userIds,
        CancellationToken cancellationToken)
    {
        var ids = userIds.ToArray();
        var assignments = await database.UserRoles
            .Where(assignment => ids.Contains(assignment.UserId))
            .Join(
                database.Roles,
                assignment => assignment.RoleId,
                role => role.Id,
                (assignment, role) => new { assignment.UserId, role.Name })
            .OrderBy(assignment => assignment.Name)
            .ToListAsync(cancellationToken);

        return assignments
            .GroupBy(assignment => assignment.UserId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<string>)group
                    .Select(assignment => assignment.Name ?? string.Empty)
                    .ToList());
    }

    private static UserAdministrationUser CreateUser(
        ApplicationUser user,
        IReadOnlyDictionary<int, IReadOnlyList<string>> rolesByUserId)
    {
        return new UserAdministrationUser
        {
            Id = user.Id,
            Username = user.UserName ?? string.Empty,
            Roles = rolesByUserId.GetValueOrDefault(user.Id, []),
            Status = user.LockoutEnd > DateTimeOffset.UtcNow ? "Locked" : "Available",
            LockoutEndUtc = user.LockoutEnd?.UtcDateTime,
            AccessFailedCount = user.AccessFailedCount,
            CreatedAtUtc = user.CreatedAtUtc,
            UpdatedAtUtc = user.UpdatedAtUtc
        };
    }

    private static void ValidateQuery(UserAdministrationQuery query)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(query.PageNumber, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(query.PageSize, 100);
        ArgumentOutOfRangeException.ThrowIfLessThan(query.PageSize, 1);
    }
}

/// <summary>
/// Defines pagination and filters for administrative user queries.
/// </summary>
public sealed class UserAdministrationQuery
{
    /// <summary>
    /// Gets or sets the one-based page number.
    /// </summary>
    public int PageNumber { get; init; } = 1;

    /// <summary>
    /// Gets or sets the maximum number of users per page.
    /// </summary>
    public int PageSize { get; init; } = 20;

    /// <summary>
    /// Gets or sets the optional username filter.
    /// </summary>
    public string? Username { get; init; }

    /// <summary>
    /// Gets or sets the optional role filter.
    /// </summary>
    public string? Role { get; init; }
}

/// <summary>
/// Represents a paged administrative user query result.
/// </summary>
public sealed class UserAdministrationPage
{
    /// <summary>
    /// Gets the one-based page number.
    /// </summary>
    public required int PageNumber { get; init; }

    /// <summary>
    /// Gets the maximum number of users per page.
    /// </summary>
    public required int PageSize { get; init; }

    /// <summary>
    /// Gets the total number of matching users.
    /// </summary>
    public required int TotalCount { get; init; }

    /// <summary>
    /// Gets the users in the current page.
    /// </summary>
    public required IReadOnlyList<UserAdministrationUser> Users { get; init; }
}

/// <summary>
/// Represents a user returned for identity administration.
/// </summary>
public sealed class UserAdministrationUser
{
    /// <summary>
    /// Gets the user identifier.
    /// </summary>
    public required int Id { get; init; }

    /// <summary>
    /// Gets the username.
    /// </summary>
    public required string Username { get; init; }

    /// <summary>
    /// Gets the assigned roles.
    /// </summary>
    public required IReadOnlyList<string> Roles { get; init; }

    /// <summary>
    /// Gets the current account availability status.
    /// </summary>
    public required string Status { get; init; }

    /// <summary>
    /// Gets the UTC lockout expiration when the account is locked.
    /// </summary>
    public DateTime? LockoutEndUtc { get; init; }

    /// <summary>
    /// Gets the current number of failed authentication attempts.
    /// </summary>
    public required int AccessFailedCount { get; init; }

    /// <summary>
    /// Gets the UTC creation date.
    /// </summary>
    public required DateTime CreatedAtUtc { get; init; }

    /// <summary>
    /// Gets the UTC date of the latest user update.
    /// </summary>
    public required DateTime UpdatedAtUtc { get; init; }
}
