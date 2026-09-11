using System.ComponentModel.DataAnnotations;
using csharp_integrations.api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace csharp_integrations.api.Controllers.Identity;

/// <summary>
/// Queries users for identity administrators.
/// </summary>
[ApiController]
[Route("api/admin/users")]
[Authorize(Policy = ApplicationAuthorizationPolicies.ManageUsers)]
public sealed class AdminUsersController(UserAdministrationService userAdministrationService) : ControllerBase
{
    /// <summary>
    /// Gets a paged collection of users.
    /// </summary>
    /// <param name="request">Pagination and filter options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A paged collection of users.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(UserPageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<UserPageResponse>> GetUsers(
        [FromQuery] GetUsersRequest request,
        CancellationToken cancellationToken)
    {
        var page = await userAdministrationService.GetUsersAsync(new UserAdministrationQuery
        {
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            Username = request.Username,
            Role = request.Role
        }, cancellationToken);

        return Ok(UserPageResponse.From(page));
    }

    /// <summary>
    /// Gets one user by identifier.
    /// </summary>
    /// <param name="userId">User identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The requested user.</returns>
    [HttpGet("{userId:int}")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<UserResponse>> GetUser(int userId, CancellationToken cancellationToken)
    {
        var user = await userAdministrationService.GetUserAsync(userId, cancellationToken);

        if (user is null)
        {
            return Problem(statusCode: StatusCodes.Status404NotFound, title: "User not found.");
        }

        return Ok(UserResponse.From(user));
    }
}

/// <summary>
/// Represents pagination and filters for administrative user queries.
/// </summary>
public sealed class GetUsersRequest
{
    /// <summary>
    /// Gets the one-based page number.
    /// </summary>
    [Range(1, int.MaxValue)]
    public int PageNumber { get; init; } = 1;

    /// <summary>
    /// Gets the maximum number of users per page.
    /// </summary>
    [Range(1, 100)]
    public int PageSize { get; init; } = 20;

    /// <summary>
    /// Gets the optional username filter.
    /// </summary>
    public string? Username { get; init; }

    /// <summary>
    /// Gets the optional role filter.
    /// </summary>
    public string? Role { get; init; }
}

/// <summary>
/// Represents a paged user response.
/// </summary>
public sealed class UserPageResponse
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
    public required IReadOnlyList<UserResponse> Users { get; init; }

    /// <summary>
    /// Creates an API response from an administrative user page.
    /// </summary>
    /// <param name="page">Administrative user page.</param>
    /// <returns>The API user page response.</returns>
    public static UserPageResponse From(UserAdministrationPage page) => new()
    {
        PageNumber = page.PageNumber,
        PageSize = page.PageSize,
        TotalCount = page.TotalCount,
        Users = page.Users.Select(UserResponse.From).ToList()
    };
}

/// <summary>
/// Represents a user returned by administrative endpoints.
/// </summary>
public sealed class UserResponse
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

    /// <summary>
    /// Creates an API response from an administrative user.
    /// </summary>
    /// <param name="user">Administrative user.</param>
    /// <returns>The API user response.</returns>
    public static UserResponse From(UserAdministrationUser user) => new()
    {
        Id = user.Id,
        Username = user.Username,
        Roles = user.Roles,
        Status = user.Status,
        LockoutEndUtc = user.LockoutEndUtc,
        AccessFailedCount = user.AccessFailedCount,
        CreatedAtUtc = user.CreatedAtUtc,
        UpdatedAtUtc = user.UpdatedAtUtc
    };
}
