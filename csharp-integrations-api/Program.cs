using csharp_integrations_core.Auth.Bearer;
using csharp_integrations_core.Auth.SAML;
using csharp_integrations_core.Swagger;
using ITfoxtec.Identity.Saml2.MvcCore.Configuration;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// Adding API authentication service
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
builder.Services.AddSwaggerWithBearerSupport("Csharp Integrations Api", "v1", "Collection of endpoints for the CSharp integration Api.");
#endregion Swagger

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    
    app.MapOpenApi();
}

app.UseSaml2();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();