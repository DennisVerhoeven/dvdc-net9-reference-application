using Fluffy.Data;
using Fluffy.Features.Carts;
using Fluffy.Features.Users;
using Fluffy.Filters;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
ConfigureBuilder(builder);

var app = builder.Build();
ConfigureApp(app);

app.Run();

static void ConfigureBuilder(WebApplicationBuilder builder)
{
    builder.AddServiceDefaults();
    ConfigureConfiguration(builder);
    ConfigureServices(builder.Services, builder.Configuration);
}

static void ConfigureConfiguration(WebApplicationBuilder builder)
{
    if (Environment.GetEnvironmentVariable("LOCAL_DOCKER") is not null)
        builder.Configuration.AddJsonFile("appsettings.Development.Docker.json", false, true);
    if (Environment.GetEnvironmentVariable("HOSTED_ENVIRONMENT") is not null)
        builder.Configuration.AddJsonFile("appsettings.Production.json", false, true);

    var appSettings = new AppSettings();
    builder.Configuration.Bind(appSettings);
    builder.Services.AddSingleton(appSettings);
}

static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
{
    services.AddOpenApi(options => { options.AddDocumentTransformer<LocalhostOpenApiServerTransformer>(); });

    services.AddScoped<LoggingFilter>();
    services.AddProblemDetails();

    // Register services per feature slice
    services.ConfigureUserFeatureV1();

    services.AddDbContext<FluffyDbContext>(options =>
        options.UseNpgsql(configuration.GetConnectionString("FluffyDbContext")));

    services.AddOpenTelemetry()
        .WithTracing(tracing => tracing.AddSource("Fluffy"));

    services.AddCors(options =>
    {
        options.AddDefaultPolicy(corsBuilder =>
        {
            corsBuilder.SetIsOriginAllowed(origin => new Uri(origin).Host == "localhost");
        });
    });
}

static void ConfigureApp(WebApplication app)
{
    if (app.Environment.IsDevelopment()) ConfigureAppForDevelopment(app);

    app.UseHttpsRedirection();
    ConfigureFeatureEndpoints(app);
    return;

    static void ConfigureAppForDevelopment(WebApplication app)
    {
        app.MapOpenApi();
        app.MapScalarApiReference(options =>
        {
            options.AddServer(Environment.GetEnvironmentVariable("LOCAL_DOCKER") is not null
                ? new ScalarServer("http://localhost:5050", "Default DockerCompose Http Localhost Server")
                : new ScalarServer("https://localhost:7147", "Default (Aspire) SSL Localhost Server"));
            options.EndpointPathPrefix = "/{documentName}";
        });

        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<FluffyDbContext>();
        dbContext.Database.Migrate();
    }
}

static void ConfigureFeatureEndpoints(WebApplication app)
{
    app.MapGroup($"api/v1/{CartEndpoints.BasePath}/")
        .MapCartV1Endpoints()
        .AddEndpointFilter<LoggingFilter>()
        .WithName("Cart Api V1");

    app.MapGroup($"api/v1/{UserFeature.BasePath}/")
        .MapUserV1Endpoints()
        .AddEndpointFilter<LoggingFilter>()
        .WithName("User Api V1");
}

// Implementation for the OpenApiDocumentTransformer to change the server url to localhost for the docker compose server
internal class LocalhostOpenApiServerTransformer : IOpenApiDocumentTransformer
{
    public async Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        document.Servers =
        [
            Environment.GetEnvironmentVariable("LOCAL_DOCKER") is not null
                ? new OpenApiServer
                {
                    Url = "https://localhost:5050",
                    Description = "Default DockerCompose Http Localhost Server"
                }
                : new OpenApiServer
                {
                    Url = "https://localhost:7147",
                    Description = "Default (Aspire) SSL Localhost Server"
                }
        ];
    }
}