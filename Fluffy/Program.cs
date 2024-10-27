using Fluffy.Auth;
using Fluffy.Data;
using Fluffy.Features.Carts;
using Fluffy.Features.Users;
using Fluffy.Filters;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

var appSettings = new AppSettings();

if (Environment.GetEnvironmentVariable("LOCAL_DOCKER") is not null)
    builder.Configuration.AddJsonFile("appsettings.Development.Docker.json", false, true);
if (Environment.GetEnvironmentVariable("IS_HOSTED_ENVIRONMENT") is not null)
    builder.Configuration.AddJsonFile("appsettings.Production.json", false, true);

builder.Configuration.Bind(appSettings);
builder.Services.AddSingleton(appSettings);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddScoped<LoggingFilter>();

// User related registration (move to seperate extension later per slice)
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IUserService, UserService>();

builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing.AddSource("Fluffy"));
builder.Services.AddProblemDetails();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(corsBuilder =>
    {
        corsBuilder.SetIsOriginAllowed(origin => new Uri(origin).Host == "localhost");
    });
});

builder.Services.AddDbContext<FluffyDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("FluffyDbContext")));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    // Register OpenApi with Scalar as UI available at https://localhost:7272/v1
    app.MapOpenApi();
    app.MapScalarApiReference((options =>
    {
        options.AddServer(new ScalarServer("https://localhost:7147", "Default (Aspire) SSL Localhost Server"));
        options.AddServer(new ScalarServer("http://localhost:5050", "Default DockerCompose Http Localhost Server"));
        options.EndpointPathPrefix = "/{documentName}";
    }));
    
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<FluffyDbContext>();
    
    // Run migrations
    await dbContext.Database.MigrateAsync();
}

app.UseHttpsRedirection();

app.MapGroup($"api/v1/{CartEndpoints.BasePath}/")
    .MapCartV1Endpoints()
    .AddEndpointFilter<LoggingFilter>()
    .WithName("Cart Api V1");

app.MapGroup($"api/v1/{UserEndpoints.BasePath}/")
    .MapUserV1Endpoints()
    .AddEndpointFilter<LoggingFilter>()
    .WithName("User Api V1");

app.Run();