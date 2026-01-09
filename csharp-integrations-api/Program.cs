using csharp_integrations_core.Auth.Bearer;
using csharp_integrations_core.Swagger;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// Adding API authentication service
#region Bearer Auth
builder.Services.AddBearerAuthentication(builder.Configuration);
builder.Services.AddTransient<TokenService>();
#endregion Bearer Auth

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

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();