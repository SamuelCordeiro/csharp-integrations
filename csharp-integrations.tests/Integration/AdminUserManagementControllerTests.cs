using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using csharp_integrations.api.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace csharp_integrations.tests.Integration;

/// <summary>
/// Covers administrative user management endpoints.
/// </summary>
public sealed class AdminUserManagementControllerTests
{
    /// <summary>
    /// Verifies that administrators can create users with supported roles.
    /// </summary>
    [Fact]
    public async Task CreateUser_WithValidRequest_CreatesUserWithRoles()
    {
        using var factory = new ApiFactory();
        var client = CreateHttpsClient(factory);
        var accessToken = await LoginAsync(client, "Admin");
        var request = CreateAuthorizedRequest(
            HttpMethod.Post,
            "/api/admin/users",
            accessToken,
            new { username = "Carla", password = "Carla#123", roles = new[] { "employee", "manager" } });

        var response = await client.SendAsync(request);
        var user = await response.Content.ReadFromJsonAsync<UserResult>();
        var persistedUser = await GetUserAsync(factory, "Carla");
        var roles = await GetRolesAsync(factory, persistedUser);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(user);
        Assert.Equal(persistedUser.Id, user.Id);
        Assert.Equal("Carla", user.Username);
        Assert.Contains(ApplicationRoles.Employee, user.Roles);
        Assert.Contains(ApplicationRoles.Manager, user.Roles);
        Assert.False(string.IsNullOrWhiteSpace(persistedUser.PasswordHash));
        Assert.Contains(ApplicationRoles.Employee, roles);
        Assert.Contains(ApplicationRoles.Manager, roles);
    }

    /// <summary>
    /// Verifies that password policy failures return validation ProblemDetails.
    /// </summary>
    [Fact]
    public async Task CreateUser_WithPasswordViolatingPolicy_ReturnsValidationProblemDetails()
    {
        using var factory = new ApiFactory();
        var client = CreateHttpsClient(factory);
        var accessToken = await LoginAsync(client, "Admin");
        var request = CreateAuthorizedRequest(
            HttpMethod.Post,
            "/api/admin/users",
            accessToken,
            new { username = "Carla", password = "weak", roles = new[] { "employee" } });

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    /// <summary>
    /// Verifies that unsupported roles return validation ProblemDetails.
    /// </summary>
    [Fact]
    public async Task CreateUser_WithUnsupportedRole_ReturnsValidationProblemDetails()
    {
        using var factory = new ApiFactory();
        var client = CreateHttpsClient(factory);
        var accessToken = await LoginAsync(client, "Admin");
        var request = CreateAuthorizedRequest(
            HttpMethod.Post,
            "/api/admin/users",
            accessToken,
            new { username = "Carla", password = "Carla#123", roles = new[] { "owner" } });

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    /// <summary>
    /// Verifies that administrators can replace user roles.
    /// </summary>
    [Fact]
    public async Task UpdateRoles_WithSupportedRoles_ReplacesUserRoles()
    {
        using var factory = new ApiFactory();
        var client = CreateHttpsClient(factory);
        var accessToken = await LoginAsync(client, "Admin");
        var user = await GetUserAsync(factory, "Josh");
        var request = CreateAuthorizedRequest(
            HttpMethod.Put,
            $"/api/admin/users/{user.Id}/roles",
            accessToken,
            new { roles = new[] { "employee" } });

        var response = await client.SendAsync(request);
        var roles = await GetRolesAsync(factory, user);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Contains(ApplicationRoles.Employee, roles);
        Assert.DoesNotContain(ApplicationRoles.Manager, roles);
    }

    /// <summary>
    /// Verifies that the last active administrator retains the administrator role.
    /// </summary>
    [Fact]
    public async Task UpdateRoles_WhenRemovingLastActiveAdministrator_ReturnsConflictProblemDetails()
    {
        using var factory = new ApiFactory();
        var client = CreateHttpsClient(factory);
        var accessToken = await LoginAsync(client, "Admin");
        var administrator = await GetUserAsync(factory, "Admin");
        var request = CreateAuthorizedRequest(
            HttpMethod.Put,
            $"/api/admin/users/{administrator.Id}/roles",
            accessToken,
            new { roles = new[] { "employee" } });

        var response = await client.SendAsync(request);
        var roles = await GetRolesAsync(factory, administrator);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Contains(ApplicationRoles.Administrator, roles);
    }

    /// <summary>
    /// Verifies that non-administrators cannot create users.
    /// </summary>
    [Fact]
    public async Task CreateUser_WithEmployeeAccessToken_ReturnsForbidden()
    {
        using var factory = new ApiFactory();
        var client = CreateHttpsClient(factory);
        var accessToken = await LoginAsync(client, "Alice");
        var request = CreateAuthorizedRequest(
            HttpMethod.Post,
            "/api/admin/users",
            accessToken,
            new { username = "Carla", password = "Carla#123", roles = new[] { "employee" } });

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private static HttpClient CreateHttpsClient(ApiFactory factory) => factory.CreateClient(
        new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

    private static HttpRequestMessage CreateAuthorizedRequest(
        HttpMethod method,
        string requestUri,
        string accessToken,
        object content)
    {
        var request = new HttpRequestMessage(method, requestUri)
        {
            Content = JsonContent.Create(content)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return request;
    }

    private static async Task<string> LoginAsync(HttpClient client, string username)
    {
        var response = await client.PostAsJsonAsync(
            "/Auth/Bearer/AuthBearer/Login",
            new { username, password = "Demo#123" });
        var login = await response.Content.ReadFromJsonAsync<LoginResult>();

        response.EnsureSuccessStatusCode();
        return login!.AccessToken;
    }

    private static async Task<ApplicationUser> GetUserAsync(ApiFactory factory, string username)
    {
        using var scope = factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        return (await userManager.FindByNameAsync(username))!;
    }

    private static async Task<IList<string>> GetRolesAsync(ApiFactory factory, ApplicationUser user)
    {
        using var scope = factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var currentUser = await userManager.FindByIdAsync(user.Id.ToString());
        return await userManager.GetRolesAsync(currentUser!);
    }

    private sealed class LoginResult
    {
        public required string AccessToken { get; init; }
    }

    private sealed class UserResult
    {
        public required int Id { get; init; }

        public required string Username { get; init; }

        public required IReadOnlyList<string> Roles { get; init; }
    }
}
