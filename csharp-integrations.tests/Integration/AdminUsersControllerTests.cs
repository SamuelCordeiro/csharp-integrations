using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using csharp_integrations.api.Data;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace csharp_integrations.tests.Integration;

/// <summary>
/// Covers administrative user query endpoints.
/// </summary>
public sealed class AdminUsersControllerTests
{
    /// <summary>
    /// Verifies that administrators can query paged users without sensitive fields.
    /// </summary>
    [Fact]
    public async Task GetUsers_WithAdministratorAccessToken_ReturnsPagedUsersWithoutSensitiveFields()
    {
        using var factory = new ApiFactory();
        var client = CreateHttpsClient(factory);
        var accessToken = await LoginAsync(client, "Admin");
        var request = CreateAuthorizedRequest(HttpMethod.Get, "/api/admin/users?pageNumber=1&pageSize=2", accessToken);

        var response = await client.SendAsync(request);
        var content = await response.Content.ReadAsStringAsync();
        var page = await response.Content.ReadFromJsonAsync<UserPageResult>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(page);
        Assert.Equal(1, page.PageNumber);
        Assert.Equal(2, page.PageSize);
        Assert.Equal(3, page.TotalCount);
        Assert.Equal(2, page.Users.Count);
        Assert.All(page.Users, user =>
        {
            Assert.False(string.IsNullOrWhiteSpace(user.Username));
            Assert.False(string.IsNullOrWhiteSpace(user.Status));
            Assert.NotEqual(default, user.CreatedAtUtc);
            Assert.NotEqual(default, user.UpdatedAtUtc);
        });
        Assert.DoesNotContain("passwordHash", content, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("securityStamp", content, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Verifies that role filters return only matching users.
    /// </summary>
    [Fact]
    public async Task GetUsers_WithRoleFilter_ReturnsMatchingUsers()
    {
        using var factory = new ApiFactory();
        var client = CreateHttpsClient(factory);
        var accessToken = await LoginAsync(client, "Admin");
        var request = CreateAuthorizedRequest(HttpMethod.Get, "/api/admin/users?role=manager", accessToken);

        var response = await client.SendAsync(request);
        var page = await response.Content.ReadFromJsonAsync<UserPageResult>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(page);
        var user = Assert.Single(page.Users);
        Assert.Equal("Josh", user.Username);
        Assert.Contains(ApplicationRoles.Manager, user.Roles);
    }

    /// <summary>
    /// Verifies that username filters return matching users.
    /// </summary>
    [Fact]
    public async Task GetUsers_WithUsernameFilter_ReturnsMatchingUsers()
    {
        using var factory = new ApiFactory();
        var client = CreateHttpsClient(factory);
        var accessToken = await LoginAsync(client, "Admin");
        var request = CreateAuthorizedRequest(HttpMethod.Get, "/api/admin/users?username=lic", accessToken);

        var response = await client.SendAsync(request);
        var page = await response.Content.ReadFromJsonAsync<UserPageResult>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(page);
        var user = Assert.Single(page.Users);
        Assert.Equal("Alice", user.Username);
    }

    /// <summary>
    /// Verifies that administrators can query one user by identifier.
    /// </summary>
    [Fact]
    public async Task GetUser_WithAdministratorAccessToken_ReturnsUserDetails()
    {
        using var factory = new ApiFactory();
        var client = CreateHttpsClient(factory);
        var accessToken = await LoginAsync(client, "Admin");
        var userId = await GetUserIdAsync(factory, "Josh");
        var request = CreateAuthorizedRequest(HttpMethod.Get, $"/api/admin/users/{userId}", accessToken);

        var response = await client.SendAsync(request);
        var user = await response.Content.ReadFromJsonAsync<UserResult>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(user);
        Assert.Equal(userId, user.Id);
        Assert.Equal("Josh", user.Username);
        Assert.Contains(ApplicationRoles.Manager, user.Roles);
        Assert.Equal("Available", user.Status);
    }

    /// <summary>
    /// Verifies that non-administrators cannot query users.
    /// </summary>
    [Fact]
    public async Task GetUsers_WithEmployeeAccessToken_ReturnsForbidden()
    {
        using var factory = new ApiFactory();
        var client = CreateHttpsClient(factory);
        var accessToken = await LoginAsync(client, "Alice");
        var request = CreateAuthorizedRequest(HttpMethod.Get, "/api/admin/users", accessToken);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    /// <summary>
    /// Verifies that missing users return ProblemDetails.
    /// </summary>
    [Fact]
    public async Task GetUser_WithUnknownIdentifier_ReturnsNotFoundProblemDetails()
    {
        using var factory = new ApiFactory();
        var client = CreateHttpsClient(factory);
        var accessToken = await LoginAsync(client, "Admin");
        var request = CreateAuthorizedRequest(HttpMethod.Get, "/api/admin/users/99999", accessToken);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    private static HttpClient CreateHttpsClient(ApiFactory factory) => factory.CreateClient(
        new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

    private static HttpRequestMessage CreateAuthorizedRequest(HttpMethod method, string requestUri, string accessToken)
    {
        var request = new HttpRequestMessage(method, requestUri);
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

    private static async Task<int> GetUserIdAsync(ApiFactory factory, string username)
    {
        using var scope = factory.Services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        return await database.Users
            .Where(user => user.UserName == username)
            .Select(user => user.Id)
            .SingleAsync();
    }

    private sealed class LoginResult
    {
        public required string AccessToken { get; init; }
    }

    private sealed class UserPageResult
    {
        public required int PageNumber { get; init; }

        public required int PageSize { get; init; }

        public required int TotalCount { get; init; }

        public required IReadOnlyList<UserResult> Users { get; init; }
    }

    private sealed class UserResult
    {
        public required int Id { get; init; }

        public required string Username { get; init; }

        public required IReadOnlyList<string> Roles { get; init; }

        public required string Status { get; init; }

        public required DateTime CreatedAtUtc { get; init; }

        public required DateTime UpdatedAtUtc { get; init; }
    }
}
