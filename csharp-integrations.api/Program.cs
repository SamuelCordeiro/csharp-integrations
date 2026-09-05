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

// Add services to the container.
#region Bearer Auth
builder.Services.AddBearerAuthentication(builder.Configuration);
builder.Services.AddTransient<TokenService>();
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

builder.Services.AddControllers(options =>
{
    if (!isSamlAuthenticationEnabled)
    {
        options.Conventions.Add(new ConditionalSamlControllerConvention());
    }
});

if (corsAllowedOrigins.Length > 0)
{
    builder.Services.AddCors(options => options.AddPolicy("Frontend", policy => policy
        .WithOrigins(corsAllowedOrigins)
        .AllowAnyHeader()
        .AllowAnyMethod()));
}

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
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
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    
    // app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseRouting();

if (corsAllowedOrigins.Length > 0)
{
    app.UseCors("Frontend");
}

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

if (isSamlAuthenticationEnabled)
{
    app.UseSaml2();
}

app.MapControllers();

app.Run();

static string GetClientIdentifier(HttpContext context)
{
    return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
}
