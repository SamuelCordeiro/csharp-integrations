using Microsoft.AspNetCore.Identity;

namespace csharp_integrations.api.Data;

/// <summary>
/// Validates passwords against the active persisted policy.
/// </summary>
public sealed class PasswordPolicyValidator(PasswordPolicyService passwordPolicyService)
    : IPasswordValidator<ApplicationUser>
{
    /// <inheritdoc />
    public async Task<IdentityResult> ValidateAsync(
        UserManager<ApplicationUser> manager,
        ApplicationUser user,
        string? password)
    {
        if (string.IsNullOrEmpty(password))
        {
            return IdentityResult.Failed(new IdentityError
            {
                Code = "PasswordRequired",
                Description = "Password is required."
            });
        }

        var policy = await passwordPolicyService.GetAsync();
        var errors = new List<IdentityError>();

        if (password.Length < policy.MinimumLength)
        {
            errors.Add(CreateError("PasswordTooShort", $"Password must be at least {policy.MinimumLength} characters long."));
        }

        if (policy.RequireUppercase && !password.Any(char.IsUpper))
        {
            errors.Add(CreateError("PasswordRequiresUppercase", "Password must contain an uppercase character."));
        }

        if (policy.RequireLowercase && !password.Any(char.IsLower))
        {
            errors.Add(CreateError("PasswordRequiresLowercase", "Password must contain a lowercase character."));
        }

        if (policy.RequireDigit && !password.Any(char.IsDigit))
        {
            errors.Add(CreateError("PasswordRequiresDigit", "Password must contain a numeric character."));
        }

        if (policy.RequireNonAlphanumeric && !password.Any(character => !char.IsLetterOrDigit(character)))
        {
            errors.Add(CreateError("PasswordRequiresNonAlphanumeric", "Password must contain a non-alphanumeric character."));
        }

        if (password.Distinct().Count() < policy.RequiredUniqueCharacters)
        {
            errors.Add(CreateError(
                "PasswordRequiresUniqueCharacters",
                $"Password must contain at least {policy.RequiredUniqueCharacters} unique characters."));
        }

        return errors.Count == 0 ? IdentityResult.Success : IdentityResult.Failed([.. errors]);
    }

    private static IdentityError CreateError(string code, string description) => new()
    {
        Code = code,
        Description = description
    };
}
