using csharp_integrations.core.Auth;
using csharp_integrations.core.Auth.SAML;
using csharp_integrations.core.Swagger;
using ITfoxtec.Identity.Saml2.MvcCore.Configuration;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
#region Bearer Auth
builder.Services.AddBearerAuthentication(builder.Configuration);
builder.Services.AddTransient<TokenService>();
#endregion Bearer Auth

// Adding Saml authentication service
#region Saml2 Auth
builder.Services.AddSamlAuthentication(builder.Configuration);
#endregion Saml2 Auth

builder.Services.AddControllers();

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

app.UseSaml2();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();