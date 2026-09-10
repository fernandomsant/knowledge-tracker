using System.Data.Common;
using System.Diagnostics;
using KnowledgeTracker.Application.Authentication;
using KnowledgeTracker.Application.Knowledge;
using KnowledgeTracker.Data.Authentication.Repositories;
using KnowledgeTracker.Data.Database;
using KnowledgeTracker.Data.Knowledge.Repositories;
using KnowledgeTracker.Infrastructure.Authentication;
using KnowledgeTracker.Infrastructure.Authentication.Services;
using KnowledgeTracker.Infrastructure.Authentication.Services.AccessTokens;
using KnowledgeTracker.Web.Authentication.Services;
using KnowledgeTracker.Web.Knowledge.Filters;
using KnowledgeTracker.Web.Middleware;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

var environmentSettings = LoadEnvironmentSettings();
var builder = WebApplication.CreateBuilder(args);
if (environmentSettings is not null)
{
    builder.Configuration.AddInMemoryCollection(environmentSettings
        .Where(setting => !string.IsNullOrWhiteSpace(setting.Value) && string.IsNullOrWhiteSpace(builder.Configuration[setting.Key]))
        .ToDictionary(setting => setting.Key, setting => setting.Value));
}

var connectionString = builder.Configuration.GetConnectionString("KnowledgeTracker")
    ?? throw new InvalidOperationException("A KnowledgeTracker connection string is required.");
var authenticationOptions = KnowledgeTracker.Application.Authentication.AuthenticationOptions.Default;
var accessTokenKey = ReadSecret(builder.Configuration, "Authentication:AccessTokenSigningKey");
var refreshTokenPepper = ReadSecret(builder.Configuration, "Authentication:RefreshTokenPepper");

builder.Services.AddControllers();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddScoped<McpExceptionFilter>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddCors(options =>
    options.AddPolicy(
        "frontend",
        policy =>
            policy
                .WithOrigins(builder.Configuration["Frontend:Origin"] ?? "http://localhost:5173")
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials()
    )
);
builder.Services.AddSingleton(authenticationOptions);
builder.Services.AddSingleton<Func<DbConnection>>(_ => () => new SqlConnection(connectionString));
builder.Services.AddScoped<IUserRepository, SqlServerUserRepository>();
builder.Services.AddScoped<IWorkspaceRepository, SqlServerWorkspaceRepository>();
builder.Services.AddScoped<IMcpAccessTokenRepository, SqlServerMcpAccessTokenRepository>();
builder.Services.AddScoped<ISessionRepository, SqlServerSessionRepository>();
builder.Services.AddScoped<ISubjectRepository, SqlServerSubjectRepository>();
builder.Services.AddScoped<ISubjectLayoutRepository, SqlServerSubjectLayoutRepository>();
builder.Services.AddScoped<ITopicRepository, SqlServerTopicRepository>();
builder.Services.AddScoped<IStudyNoteRepository, SqlServerStudyNoteRepository>();
builder.Services.AddScoped<IStudyMetricDefinitionRepository, SqlServerStudyMetricDefinitionRepository>();
builder.Services.AddScoped<ISubjectConnectionRepository, SqlServerSubjectConnectionRepository>();
builder.Services.AddScoped<ISubjectGoalRepository, SqlServerSubjectGoalRepository>();
builder.Services.AddScoped<ISubjectGoalActivityRepository>(sp => (SqlServerSubjectGoalRepository)sp.GetRequiredService<ISubjectGoalRepository>());
builder.Services.AddScoped<ISubjectGoalCompletionRepository, SqlServerSubjectGoalCompletionRepository>();
builder.Services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
builder.Services.AddSingleton<IClock, KnowledgeTracker.Infrastructure.Authentication.SystemClock>();
builder.Services.AddSingleton<IMcpAccessTokenGenerator, OpaqueMcpAccessTokenGenerator>();
builder.Services.AddScoped<IMcpAccessTokenValidator, McpAccessTokenValidator>();
builder.Services.AddScoped<ICurrentUserContext, CurrentUserContext>();
builder.Services.AddScoped<ICurrentWorkspaceContext, CurrentWorkspaceContext>();
builder.Services.AddScoped<CurrentUserDataScope>();
builder.Services.AddScoped<CurrentWorkspaceDataScope>();
builder.Services.AddScoped<IActionAuthorizationService, McpAwareActionAuthorizationService>();
builder.Services.AddSingleton<IAccessTokenService>(_ =>
    new HmacAccessTokenService(accessTokenKey, authenticationOptions)
);
builder.Services.AddSingleton<IRefreshTokenService>(_ =>
    new OpaqueRefreshTokenService(refreshTokenPepper)
);
builder.Services.AddScoped<KnowledgeTracker.Application.Authentication.IAuthenticationService, KnowledgeTracker.Application.Authentication.AuthenticationService>();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<IWorkspaceService, WorkspaceService>();
builder.Services.AddScoped<IMcpAccessTokenService, McpAccessTokenService>();
builder.Services.AddScoped<ISubjectService, SubjectService>();
builder.Services.AddScoped<ISubjectLayoutService, SubjectLayoutService>();
builder.Services.AddScoped<ITopicService, TopicService>();
builder.Services.AddScoped<IStudyNoteService, StudyNoteService>();
builder.Services.AddScoped<IStudyMetricDefinitionService, StudyMetricDefinitionService>();
builder.Services.AddScoped<ISubjectConnectionService, SubjectConnectionService>();
builder.Services.AddScoped<ISubjectGoalService, SubjectGoalService>();
builder.Services.AddScoped<ISubjectGoalActivityService, SubjectGoalActivityService>();
builder.Services
    .AddAuthentication(AccessTokenAuthenticationHandler.AuthenticationScheme)
    .AddScheme<AuthenticationSchemeOptions, AccessTokenAuthenticationHandler>(
        AccessTokenAuthenticationHandler.AuthenticationScheme,
        _ => { }
    )
    .AddScheme<AuthenticationSchemeOptions, McpAccessTokenAuthenticationHandler>(
        McpAccessTokenAuthenticationHandler.AuthenticationScheme,
        _ => { }
    );
builder.Services.AddAuthorization();

var app = builder.Build();
app.UseExceptionHandler();
if (!app.Environment.IsDevelopment())
    app.UseHttpsRedirection();
if (app.Environment.IsDevelopment())
{
    app.Use(async (context, next) =>
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            await next(context);
        }
        finally
        {
            app.Logger.LogInformation(
                "HTTP {Method} {Path} responded {StatusCode} in {ElapsedMilliseconds} ms",
                context.Request.Method,
                context.Request.Path,
                context.Response.StatusCode,
                stopwatch.ElapsedMilliseconds
            );
        }
    });
}
app.UseCors("frontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();

static Dictionary<string, string?>? LoadEnvironmentSettings()
{
    for (var directory = new DirectoryInfo(Directory.GetCurrentDirectory()); directory is not null; directory = directory.Parent)
    {
        var solutionDirectory = File.Exists(Path.Combine(directory.FullName, "KnowledgeTracker.slnx"))
            ? directory
            : new DirectoryInfo(Path.Combine(directory.FullName, "src", "KnowledgeTracker"));
        var candidate = Path.Combine(solutionDirectory.FullName, ".env.json");
        if (File.Exists(Path.Combine(solutionDirectory.FullName, "KnowledgeTracker.slnx")) && File.Exists(candidate))
        {
            using var document = JsonDocument.Parse(File.ReadAllText(candidate));
            var settings = new Dictionary<string, string?>();
            AddConfigurationValues(document.RootElement, null, settings);
            return settings;
        }
    }

    return null;
}

static void AddConfigurationValues(JsonElement element, string? prefix, Dictionary<string, string?> settings)
{
    foreach (var property in element.EnumerateObject())
    {
        var key = string.IsNullOrEmpty(prefix) ? property.Name : $"{prefix}:{property.Name}";
        if (property.Value.ValueKind == JsonValueKind.Object)
            AddConfigurationValues(property.Value, key, settings);
        else if (property.Value.ValueKind != JsonValueKind.Null)
            settings[key] = property.Value.ToString();
    }
}

static byte[] ReadSecret(IConfiguration configuration, string key)
{
    var value = configuration[key]
        ?? throw new InvalidOperationException($"Configuration value '{key}' is required.");
    var bytes = Convert.FromBase64String(value);
    if (bytes.Length < 32)
        throw new InvalidOperationException($"Configuration value '{key}' must decode to at least 32 bytes.");
    return bytes;
}
