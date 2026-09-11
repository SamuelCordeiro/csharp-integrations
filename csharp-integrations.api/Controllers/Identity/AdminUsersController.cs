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
public sealed class AdminUsersController(
    UserAdministrationService userAdministrationService,
    UserManagementService userManagementService,
    UserAccessManagementService userAccessManagementService) : ControllerBase
{
    /// <summary>
    /// Creates a user with application roles.
    /// </summary>
    /// <param name="request">User creation data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created user.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<UserResponse>> CreateUser(
        [FromBody] CreateUserRequest request,
        CancellationToken cancellationToken)
    {
        var result = await userManagementService.CreateAsync(request.Username, request.Password, request.Roles);
        var failure = CreateFailureResult(result);
        if (failure is not null)
        {
            return failure;
        }

        var user = await userAdministrationService.GetUserAsync(result.UserId!.Value, cancellationToken);
        return CreatedAtAction(nameof(GetUser), new { userId = user!.Id }, UserResponse.From(user));
    }

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

    /// <summary>
    /// Replaces a user's application roles.
    /// </summary>
    /// <param name="userId">User identifier.</param>
    /// <param name="request">Roles that must remain assigned.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content when the roles are updated.</returns>
    [HttpPut("{userId:int}/roles")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateRoles(
        int userId,
        [FromBody] UpdateUserRolesRequest request,
        CancellationToken cancellationToken)
    {
        var result = await userManagementService.UpdateRolesAsync(userId, request.Roles);
        var failure = CreateFailureResult(result);

        return failure is null ? NoContent() : failure;
    }

    /// <summary>
    /// Temporarily locks a user.
    /// </summary>
    /// <param name="userId">User identifier.</param>
    /// <param name="request">Lockout duration.</param>
    /// <returns>No content when the user is locked.</returns>
    [HttpPost("{userId:int}/lock")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> LockUser(int userId, [FromBody] LockUserRequest? request)
    {
        var result = await userAccessManagementService.LockAsync(
            userId,
            TimeSpan.FromMinutes(request?.DurationMinutes ?? LockUserRequest.DefaultDurationMinutes));

        return CreateAccessResult(result);
    }

    /// <summary>
    /// Clears a user's temporary lockout.
    /// </summary>
    /// <param name="userId">User identifier.</param>
    /// <returns>No content when the user is unlocked.</returns>
    [HttpPost("{userId:int}/unlock")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UnlockUser(int userId)
    {
        return CreateAccessResult(await userAccessManagementService.UnlockAsync(userId));
    }

    /// <summary>
    /// Disables a user until an administrator enables the account.
    /// </summary>
    /// <param name="userId">User identifier.</param>
    /// <returns>No content when the user is disabled.</returns>
    [HttpPost("{userId:int}/disable")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DisableUser(int userId)
    {
        return CreateAccessResult(await userAccessManagementService.DisableAsync(userId));
    }

    /// <summary>
    /// Enables a previously disabled user.
    /// </summary>
    /// <param name="userId">User identifier.</param>
    /// <returns>No content when the user is enabled.</returns>
    [HttpPost("{userId:int}/enable")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> EnableUser(int userId)
    {
        return CreateAccessResult(await userAccessManagementService.EnableAsync(userId));
    }

    private ActionResult? CreateFailureResult(UserManagementResult result)
    {
        return result.Status switch
        {
            UserManagementStatus.Succeeded => null,
            UserManagementStatus.NotFound => Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "User not found."),
            UserManagementStatus.InvalidRoles => ValidationProblem(new ValidationProblemDetails(
                new Dictionary<string, string[]>
                {
                    ["roles"] = ["One or more roles are not supported."]
                })),
            UserManagementStatus.LastActiveAdministrator => Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "The last active administrator must retain the administrator role."),
            UserManagementStatus.ValidationFailed => ValidationProblem(new ValidationProblemDetails(
                new Dictionary<string, string[]>
                {
                    ["identity"] = result.Errors.Select(error => error.Description).ToArray()
                })),
            _ => throw new InvalidOperationException("Unknown user management status.")
        };
    }

    private IActionResult CreateAccessResult(UserAccessManagementResult result)
    {
        return result.Status switch
        {
            UserAccessManagementStatus.Succeeded => NoContent(),
            UserAccessManagementStatus.NotFound => Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "User not found."),
            UserAccessManagementStatus.LastActiveAdministrator => Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "The last active administrator cannot be locked or disabled."),
            UserAccessManagementStatus.ValidationFailed => ValidationProblem(new ValidationProblemDetails(
                new Dictionary<string, string[]>
                {
                    ["identity"] = result.Errors.Select(error => error.Description).ToArray()
                })),
            _ => throw new InvalidOperationException("Unknown user access management status.")
        };
    }
}

/// <summary>
/// Represents an administrative lockout request.
/// </summary>
public sealed class LockUserRequest
{
    /// <summary>
    /// Defines the default temporary lockout duration.
    /// </summary>
    public const int DefaultDurationMinutes = 30;

    /// <summary>
    /// Gets the temporary lockout duration in minutes.
    /// </summary>
    [Range(1, 1440)]
    public int DurationMinutes { get; init; } = DefaultDurationMinutes;
}

/// <summary>
/// Represents user creation data.
/// </summary>
public sealed class CreateUserRequest
{
    /// <summary>
    /// Gets the username.
    /// </summary>
    [Required]
    [StringLength(256, MinimumLength = 1)]
    public required string Username { get; init; }

    /// <summary>
    /// Gets the password validated by the active policy.
    /// </summary>
    [Required]
    public required string Password { get; init; }

    /// <summary>
    /// Gets the roles assigned to the new user.
    /// </summary>
    [Required]
    [MinLength(1)]
    public required IReadOnlyList<string> Roles { get; init; }
}

/// <summary>
/// Represents a user role update.
/// </summary>
public sealed class UpdateUserRolesRequest
{
    /// <summary>
    /// Gets the roles that must remain assigned.
    /// </summary>
    [Required]
    [MinLength(1)]
    public required IReadOnlyList<string> Roles { get; init; }
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
    /// Gets whether the user can authenticate.
    /// </summary>
    public required bool IsActive { get; init; }

    /// <summary>
    /// Gets the UTC date when the user was disabled.
    /// </summary>
    public DateTime? DisabledAtUtc { get; init; }

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
        IsActive = user.IsActive,
        DisabledAtUtc = user.DisabledAtUtc,
        LockoutEndUtc = user.LockoutEndUtc,
        AccessFailedCount = user.AccessFailedCount,
        CreatedAtUtc = user.CreatedAtUtc,
        UpdatedAtUtc = user.UpdatedAtUtc
    };
}
