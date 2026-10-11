using AspNetOrderAPI.Configuration;
using AspNetOrderAPI.Data;
using AspNetOrderAPI.Services;
using Azure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Optional: Azure Key Vault integration for production
if (builder.Environment.IsProduction())
{
    var keyVaultUrl = builder.Configuration["KeyVault:Url"];
    if (!string.IsNullOrWhiteSpace(keyVaultUrl))
    {
        builder.Configuration.AddAzureKeyVault(
            new Uri(keyVaultUrl),
            new DefaultAzureCredential());
    }
}

var logPath = Path.Combine(AppContext.BaseDirectory, "logs", "log-.txt");

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "AspNetOrderIntacctAPI")
    .Enrich.WithProperty("Environment", builder.Environment.EnvironmentName)
    .WriteTo.Console(outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File(
        logPath,
        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{Level:u3}] {Message:lj}{NewLine}{Exception}",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30)
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("database")
    .AddCheck<IntacctHealthCheck>("intacct");

builder.Services.Configure<IntacctOptions>(builder.Configuration.GetSection("Intacct"));
builder.Services.Configure<IntacctOAuthOptions>(builder.Configuration.GetSection("IntacctOAuth"));
builder.Services.Configure<PostgresOptions>(builder.Configuration.GetSection("ConnectionStrings"));

builder.Services.AddDbContext<AppDbContext>((serviceProvider, options) =>
{
    var postgresOptions = serviceProvider.GetRequiredService<IOptions<PostgresOptions>>();
    options.UseNpgsql(postgresOptions.Value.Postgres, npgsqlOptions =>
    {
        npgsqlOptions.EnableRetryOnFailure(3, TimeSpan.FromSeconds(2), null);
    });
});

builder.Services.AddHttpClient<IIntacctAuthService, IntacctAuthService>()
    .ConfigureHttpClient(client => client.Timeout = TimeSpan.FromSeconds(30));

builder.Services.AddHttpClient<IIntacctConfigService, IntacctConfigService>()
    .ConfigureHttpClient(client => client.Timeout = TimeSpan.FromSeconds(30));

builder.Services.AddHttpClient<IIntacctMappingService, IntacctMappingService>()
    .ConfigureHttpClient(client => client.Timeout = TimeSpan.FromSeconds(30));

builder.Services.AddHttpClient<IIntacctOrderService, IntacctOrderService>()
    .ConfigureHttpClient(client => client.Timeout = TimeSpan.FromSeconds(30));

builder.Services.AddHttpClient<IIntacctService, IntacctService>()
    .ConfigureHttpClient(client => client.Timeout = TimeSpan.FromSeconds(30));

builder.Services.AddHttpClient<IOrderJsonFeedService, OrderJsonFeedService>()
    .ConfigureHttpClient(client => client.Timeout = TimeSpan.FromSeconds(30));

builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IOrderJsonFeedService, OrderJsonFeedService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

var autoApplyMigrations = bool.Parse(builder.Configuration["Database:ApplyMigrationsOnStartup"] ?? "true");
if (autoApplyMigrations)
{
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
        Log.Information("Database migrations applied successfully.");
    }
}

Log.Information("AspNetOrderIntacctAPI is starting in {Environment} mode", app.Environment.EnvironmentName);
await app.RunAsync();

public class DatabaseHealthCheck : Microsoft.Extensions.Diagnostics.HealthChecks.IHealthCheck
{
    private readonly AppDbContext _dbContext;

    public DatabaseHealthCheck(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult> CheckHealthAsync(
        Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _dbContext.Database.CanConnectAsync(cancellationToken);
            return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy("Database is reachable.");
        }
        catch (Exception ex)
        {
            return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Unhealthy($"Database unavailable: {ex.Message}");
        }
    }
}

public class IntacctHealthCheck : Microsoft.Extensions.Diagnostics.HealthChecks.IHealthCheck
{
    private readonly IIntacctAuthService _authService;

    public IntacctHealthCheck(IIntacctAuthService authService)
    {
        _authService = authService;
    }

    public async Task<Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult> CheckHealthAsync(
        Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var token = await _authService.GetAccessTokenAsync();
            return string.IsNullOrWhiteSpace(token)
                ? Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Unhealthy("Intacct token could not be obtained.")
                : Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy("Intacct OAuth token is valid.");
        }
        catch (Exception ex)
        {
            return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Unhealthy($"Intacct health check failed: {ex.Message}");
        }
    }
}
