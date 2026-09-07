using csharp_integrations.core.GlobalResources.Repositories;

namespace csharp_integrations.tests.Unit;

/// <summary>
/// Covers the behavior of the in-memory demonstration users.
/// </summary>
public sealed class UserRepositoryTests
{
    /// <summary>
    /// Verifies that known demonstration credentials return their matching user.
    /// </summary>
    [Fact]
    public void Get_WithKnownCredentials_ReturnsUser()
    {
        var user = UserRepository.Get("Josh", "123");

        Assert.NotNull(user);
        Assert.Equal(1, user.Id);
        Assert.Equal("manager", user.Role);
    }

    /// <summary>
    /// Verifies that unknown credentials do not throw or expose a user.
    /// </summary>
    [Fact]
    public void Get_WithInvalidCredentials_ReturnsNull()
    {
        var user = UserRepository.Get("Josh", "invalid");

        Assert.Null(user);
    }
}
