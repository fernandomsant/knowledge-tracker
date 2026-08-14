using System.Data.Common;
using KnowledgeTracker.Application.Authentication;
using KnowledgeTracker.Application.Knowledge;
using KnowledgeTracker.Data.Authentication.Repositories;
using KnowledgeTracker.Data.Knowledge.Repositories;
using KnowledgeTracker.Infrastructure.Authentication;
using KnowledgeTracker.Web.Authentication.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Data.SqlClient;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("KnowledgeTracker")
    ?? builder.Configuration.GetConnectionString("KnowledgeTracker_01")
    ?? throw new InvalidOperationException("A KnowledgeTracker connection string is required.");
var authenticationOptions = KnowledgeTracker.Application.Authentication.AuthenticationOptions.Default;
var accessTokenKey = ReadSecret(builder.Configuration, "Authentication:AccessTokenSigningKey");
var refreshTokenPepper = ReadSecret(builder.Configuration, "Authentication:RefreshTokenPepper");

builder.Services.AddControllers();
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
builder.Services.AddScoped<ISessionRepository, SqlServerSessionRepository>();
builder.Services.AddScoped<ISubjectRepository, SqlServerSubjectRepository>();
builder.Services.AddScoped<ISubjectLayoutRepository, SqlServerSubjectLayoutRepository>();
builder.Services.AddScoped<ITopicRepository, SqlServerTopicRepository>();
builder.Services.AddScoped<IStudyNoteRepository, SqlServerStudyNoteRepository>();
builder.Services.AddScoped<IStudyMetricDefinitionRepository, SqlServerStudyMetricDefinitionRepository>();
builder.Services.AddScoped<ISubjectConnectionRepository, SqlServerSubjectConnectionRepository>();
builder.Services.AddScoped<ISubjectGoalRepository, SqlServerSubjectGoalRepository>();
builder.Services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
builder.Services.AddSingleton<IAccessTokenService>(_ =>
    new HmacAccessTokenService(accessTokenKey, authenticationOptions)
);
builder.Services.AddSingleton<IRefreshTokenService>(_ =>
    new OpaqueRefreshTokenService(refreshTokenPepper)
);
builder.Services.AddScoped<KnowledgeTracker.Application.Authentication.IAuthenticationService, KnowledgeTracker.Application.Authentication.AuthenticationService>();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<ISubjectService, SubjectService>();
builder.Services.AddScoped<ISubjectLayoutService, SubjectLayoutService>();
builder.Services.AddScoped<ITopicService, TopicService>();
builder.Services.AddScoped<IStudyNoteService, StudyNoteService>();
builder.Services.AddScoped<IStudyMetricDefinitionService, StudyMetricDefinitionService>();
builder.Services.AddScoped<ISubjectConnectionService, SubjectConnectionService>();
builder.Services.AddScoped<ISubjectGoalService, SubjectGoalService>();
builder.Services
    .AddAuthentication(AccessTokenAuthenticationHandler.AuthenticationScheme)
    .AddScheme<AuthenticationSchemeOptions, AccessTokenAuthenticationHandler>(
        AccessTokenAuthenticationHandler.AuthenticationScheme,
        _ => { }
    );
builder.Services.AddAuthorization();

var app = builder.Build();
if (!app.Environment.IsDevelopment())
    app.UseHttpsRedirection();
app.UseCors("frontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();

static byte[] ReadSecret(IConfiguration configuration, string key)
{
    var value = configuration[key]
        ?? throw new InvalidOperationException($"Configuration value '{key}' is required.");
    var bytes = Convert.FromBase64String(value);
    if (bytes.Length < 32)
        throw new InvalidOperationException($"Configuration value '{key}' must decode to at least 32 bytes.");
    return bytes;
}
