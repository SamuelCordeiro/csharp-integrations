using csharp_integrations.api.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace csharp_integrations.tests.Integration;

/// <summary>
/// Covers dynamic password validation through ASP.NET Identity.
/// </summary>
public sealed class PasswordPolicyValidatorTests
{
    /// <summary>
    /// Verifies that the active policy rejects passwords without required characters.
    /// </summary>
    /// <param name="password">Password evaluated by the Identity pipeline.</param>
    /// <param name="errorCode">Expected policy validation error code.</param>
    [Theory]
    [InlineData("lowercase1!", "PasswordRequiresUppercase")]
    [InlineData("UPPERCASE1!", "PasswordRequiresLowercase")]
    [InlineData("ValidPassword!", "PasswordRequiresDigit")]
    [InlineData("ValidPassword1", "PasswordRequiresNonAlphanumeric")]
    public async Task CreateAsync_WithPasswordMissingRequiredCharacter_ReturnsPolicyError(
        string password,
        string errorCode)
    {
        using var factory = new ApiFactory();
        using var scope = factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var result = await userManager.CreateAsync(CreateUser(), password);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, error => error.Code == errorCode);
    }

    /// <summary>
    /// Verifies that a policy change affects the next password validation.
    /// </summary>
    [Fact]
    public async Task CreateAsync_AfterPolicyIsRelaxed_AcceptsMatchingPassword()
    {
        using var factory = new ApiFactory();
        using var scope = factory.Services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var policy = await database.PasswordPolicies.SingleAsync();
        policy.RequireUppercase = false;
        policy.UpdatedAtUtc = DateTime.UtcNow;
        await database.SaveChangesAsync();

        var result = await userManager.CreateAsync(CreateUser(), "lowercase1!");

        Assert.True(result.Succeeded);
    }

    /// <summary>
    /// Verifies that the active policy is applied when a password is changed.
    /// </summary>
    [Fact]
    public async Task ChangePasswordAsync_AfterMinimumLengthIsIncreased_ReturnsPolicyError()
    {
        using var factory = new ApiFactory();
        using var scope = factory.Services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = CreateUser();
        var createResult = await userManager.CreateAsync(user, "ValidPassword1!");
        var policy = await database.PasswordPolicies.SingleAsync();
        policy.MinimumLength = 16;
        policy.UpdatedAtUtc = DateTime.UtcNow;
        await database.SaveChangesAsync();

        var result = await userManager.ChangePasswordAsync(user, "ValidPassword1!", "NewPassword1!");

        Assert.True(createResult.Succeeded);
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, error => error.Code == "PasswordTooShort");
    }

    private static ApplicationUser CreateUser() => new()
    {
        UserName = $"password-policy-{Guid.NewGuid():N}"
    };
}
