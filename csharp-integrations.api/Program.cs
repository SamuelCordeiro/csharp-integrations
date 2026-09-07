using csharp_integrations.core.AI.Providers.Ollama.Extensions;
using csharp_integrations.core.Auth.Bearer;
using csharp_integrations.core.Auth.SAML;
using csharp_integrations.core.Swagger;
using csharp_integrations.core.AI.Providers.Ollama.Models;
using csharp_integrations.api.Infrastructure;
using ITfoxtec.Identity.Saml2.MvcCore.Configuration;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);
var isSamlAuthenticationEnabled = builder.Configuration.IsSamlAuthenticationEnabled();
var corsAllowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
var corsAllowCredentials = builder.Configuration.GetValue<bool>("Cors:AllowCredentials");

// Add services to the container.
#region Error Handling
builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
        context.ProblemDetails.Extensions.TryAdd("traceId", context.HttpContext.TraceIdentifier);
});
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
#endregion Error Handling

#region Bearer Auth
builder.Services.AddBearerAuthentication(builder.Configuration);
builder.Services.AddScoped<TokenService>();
builder.Services.AddSingleton<InMemoryRefreshTokenStore>();
builder.Services.AddScoped<RefreshTokenService>();
#endregion Bearer Auth

// Adding Saml authentication service
#region Saml2 Auth
if (isSamlAuthenticationEnabled)
{
    builder.Services.AddSamlAuthentication(builder.Configuration);
}
#endregion Saml2 Auth

#region  Ollama
builder.Services.AddOllama(builder.Configuration);
#endregion Ollama

#region Controllers
builder.Services.AddControllers(options =>
{
    if (!isSamlAuthenticationEnabled)
    {
        options.Conventions.Add(new ConditionalSamlControllerConvention());
    }
});
#endregion Controllers

#region Cors
if (corsAllowedOrigins.Length > 0)
{
    builder.Services.AddCors(options => options.AddPolicy("Frontend", policy =>
    {
        policy
            .WithOrigins(corsAllowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();

        if (corsAllowCredentials)
        {
            policy.AllowCredentials();
        }
    }));
}
#endregion Cors

#region Rate Limiting
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, cancellationToken) =>
    {
        var problemDetails = new Microsoft.AspNetCore.Mvc.ProblemDetails
        {
            Status = StatusCodes.Status429TooManyRequests,
            Title = "Too Many Requests",
            Type = "https://httpstatuses.com/429",
            Detail = "The request rate limit has been exceeded.",
            Instance = context.HttpContext.Request.Path
        };
        problemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;

        await context.HttpContext.RequestServices
            .GetRequiredService<IProblemDetailsService>()
            .TryWriteAsync(new ProblemDetailsContext
            {
                HttpContext = context.HttpContext,
                ProblemDetails = problemDetails
            });
    };
    options.AddPolicy("authentication", context => RateLimitPartition.GetFixedWindowLimiter(
        GetClientIdentifier(context),
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 5,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            AutoReplenishment = true
        }));
    options.AddPolicy("ollama", context => RateLimitPartition.GetFixedWindowLimiter(
        GetClientIdentifier(context),
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 30,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            AutoReplenishment = true
        }));
    options.AddPolicy("ollama-download", context => RateLimitPartition.GetFixedWindowLimiter(
        GetClientIdentifier(context),
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 3,
            Window = TimeSpan.FromHours(1),
            QueueLimit = 0,
            AutoReplenishment = true
        }));
});
#endregion Rate Limiting

// Adding Swagger service
#region Swagger
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
var xmlDocumentationFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
var xmlDocumentationPath = Path.Combine(AppContext.BaseDirectory, xmlDocumentationFile);
var coreXmlDocumentationFile = $"{typeof(ChatRequest).Assembly.GetName().Name}.xml";
var coreXmlDocumentationPath = Path.Combine(AppContext.BaseDirectory, coreXmlDocumentationFile);
builder.Services.AddSwaggerWithBearerSupport(
    "Csharp Integrations Api",
    "v1",
    "Collection of endpoints for the CSharp integration Api.",
    [xmlDocumentationPath, coreXmlDocumentationPath]);
#endregion Swagger

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
// builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
#region Error Handling
app.UseExceptionHandler();
#endregion Error Handling

#region Swagger
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    
    // app.MapOpenApi();
}
#endregion Swagger

#region Http
app.UseHttpsRedirection();
app.UseRouting();
#endregion Http

#region Cors
if (corsAllowedOrigins.Length > 0)
{
    app.UseCors("Frontend");
}
#endregion Cors

#region Rate Limiting
app.UseRateLimiter();
#endregion Rate Limiting

#region Bearer Auth
app.UseAuthentication();
app.UseAuthorization();
#endregion Bearer Auth

#region Saml2 Auth
if (isSamlAuthenticationEnabled)
{
    app.UseSaml2();
}
#endregion Saml2 Auth

#region Controllers
app.MapControllers();
#endregion Controllers

app.Run();

static string GetClientIdentifier(HttpContext context)
{
    return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
}

/// <summary>
/// Exposes the top-level application entry point to the integration-test assembly.
/// </summary>
public partial class Program
{
}
