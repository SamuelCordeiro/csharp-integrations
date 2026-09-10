using System.ComponentModel.DataAnnotations;
using csharp_integrations.api.Data;
using csharp_integrations.core.Auth.Bearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace csharp_integrations.api.Controllers.Identity;

/// <summary>
/// Manages the active password policy for administrators.
/// </summary>
[ApiController]
[Route("api/admin/password-policy")]
[Authorize(Policy = ApplicationAuthorizationPolicies.ManagePasswordPolicy)]
public sealed class AdminPasswordPolicyController(PasswordPolicyService passwordPolicyService) : ControllerBase
{
    /// <summary>
    /// Gets the active password policy.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The active password policy.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(PasswordPolicyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PasswordPolicyResponse>> Get(CancellationToken cancellationToken)
    {
        var policy = await passwordPolicyService.GetAsync(cancellationToken);

        return Ok(PasswordPolicyResponse.From(policy));
    }

    /// <summary>
    /// Updates the active password policy.
    /// </summary>
    /// <param name="request">Password policy requirements.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated password policy.</returns>
    [HttpPut]
    [ProducesResponseType(typeof(PasswordPolicyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PasswordPolicyResponse>> Update(
        [FromBody] UpdatePasswordPolicyRequest request,
        CancellationToken cancellationToken)
    {
        var update = new PasswordPolicyUpdate
        {
            MinimumLength = request.MinimumLength,
            RequireUppercase = request.RequireUppercase,
            RequireLowercase = request.RequireLowercase,
            RequireDigit = request.RequireDigit,
            RequireNonAlphanumeric = request.RequireNonAlphanumeric,
            RequiredUniqueCharacters = request.RequiredUniqueCharacters,
            MaxFailedAccessAttempts = request.MaxFailedAccessAttempts,
            LockoutDurationMinutes = request.LockoutDurationMinutes
        };
        var policy = await passwordPolicyService.UpdateAsync(
            update,
            TokenService.GetUserId(User),
            cancellationToken);

        return Ok(PasswordPolicyResponse.From(policy));
    }
}

/// <summary>
/// Represents a password policy update request.
/// </summary>
public sealed class UpdatePasswordPolicyRequest
{
    /// <summary>
    /// Gets the minimum password length.
    /// </summary>
    [Range(8, 128)]
    public int MinimumLength { get; init; }

    /// <summary>
    /// Gets whether uppercase characters are required.
    /// </summary>
    public bool RequireUppercase { get; init; }

    /// <summary>
    /// Gets whether lowercase characters are required.
    /// </summary>
    public bool RequireLowercase { get; init; }

    /// <summary>
    /// Gets whether numeric characters are required.
    /// </summary>
    public bool RequireDigit { get; init; }

    /// <summary>
    /// Gets whether non-alphanumeric characters are required.
    /// </summary>
    public bool RequireNonAlphanumeric { get; init; }

    /// <summary>
    /// Gets the minimum number of unique password characters.
    /// </summary>
    [Range(1, 128)]
    public int RequiredUniqueCharacters { get; init; }

    /// <summary>
    /// Gets the maximum failed sign-in attempts before lockout.
    /// </summary>
    [Range(1, 20)]
    public int MaxFailedAccessAttempts { get; init; }

    /// <summary>
    /// Gets the lockout duration in minutes.
    /// </summary>
    [Range(1, 1440)]
    public int LockoutDurationMinutes { get; init; }
}

/// <summary>
/// Represents the active password policy returned by the API.
/// </summary>
public sealed class PasswordPolicyResponse
{
    /// <summary>
    /// Gets the minimum password length.
    /// </summary>
    public required int MinimumLength { get; init; }

    /// <summary>
    /// Gets whether uppercase characters are required.
    /// </summary>
    public required bool RequireUppercase { get; init; }

    /// <summary>
    /// Gets whether lowercase characters are required.
    /// </summary>
    public required bool RequireLowercase { get; init; }

    /// <summary>
    /// Gets whether numeric characters are required.
    /// </summary>
    public required bool RequireDigit { get; init; }

    /// <summary>
    /// Gets whether non-alphanumeric characters are required.
    /// </summary>
    public required bool RequireNonAlphanumeric { get; init; }

    /// <summary>
    /// Gets the minimum number of unique password characters.
    /// </summary>
    public required int RequiredUniqueCharacters { get; init; }

    /// <summary>
    /// Gets the maximum failed sign-in attempts before lockout.
    /// </summary>
    public required int MaxFailedAccessAttempts { get; init; }

    /// <summary>
    /// Gets the lockout duration in minutes.
    /// </summary>
    public required int LockoutDurationMinutes { get; init; }

    /// <summary>
    /// Gets the UTC date of the latest update.
    /// </summary>
    public required DateTime UpdatedAtUtc { get; init; }

    /// <summary>
    /// Gets the administrator who last updated the policy.
    /// </summary>
    public int? UpdatedByUserId { get; init; }

    /// <summary>
    /// Creates an API response from a persisted password policy.
    /// </summary>
    /// <param name="policy">Persisted password policy.</param>
    /// <returns>The API password policy response.</returns>
    public static PasswordPolicyResponse From(PasswordPolicy policy) => new()
    {
        MinimumLength = policy.MinimumLength,
        RequireUppercase = policy.RequireUppercase,
        RequireLowercase = policy.RequireLowercase,
        RequireDigit = policy.RequireDigit,
        RequireNonAlphanumeric = policy.RequireNonAlphanumeric,
        RequiredUniqueCharacters = policy.RequiredUniqueCharacters,
        MaxFailedAccessAttempts = policy.MaxFailedAccessAttempts,
        LockoutDurationMinutes = policy.LockoutDurationMinutes,
        UpdatedAtUtc = policy.UpdatedAtUtc,
        UpdatedByUserId = policy.UpdatedByUserId
    };
}
