using System.Data.Common;
using KnowledgeTracker.Application.Authentication;
using KnowledgeTracker.Application.Knowledge;
using KnowledgeTracker.Data.Authentication.Repositories;
using KnowledgeTracker.Infrastructure.Authentication;
using KnowledgeTracker.Infrastructure.Authentication.Services;
using KnowledgeTracker.Infrastructure.Authentication.Services.AccessTokens;
using KnowledgeTracker.Data.Knowledge.Repositories;
using KnowledgeTracker.Mcp;
using KnowledgeTracker.Mcp.Authentication.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Data.SqlClient;
using ModelContextProtocol.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpContextAccessor();

var connectionString = builder.Configuration.GetConnectionString("KnowledgeTracker")
    ?? builder.Configuration.GetConnectionString("KnowledgeTracker_01")
    ?? throw new InvalidOperationException("A KnowledgeTracker connection string is required.");

builder.Services.AddSingleton<Func<DbConnection>>(_ => () => new SqlConnection(connectionString));
builder.Services.AddScoped<IUserRepository, SqlServerUserRepository>();
builder.Services.AddScoped<IMcpAccessTokenRepository, SqlServerMcpAccessTokenRepository>();
builder.Services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
builder.Services.AddSingleton<IClock, KnowledgeTracker.Infrastructure.Authentication.SystemClock>();
builder.Services.AddSingleton<IMcpAccessTokenGenerator, OpaqueMcpAccessTokenGenerator>();
builder.Services.AddScoped<IUserPermissionService, DefaultUserPermissionService>();
builder.Services.AddScoped<IMcpAccessTokenValidator, McpAccessTokenValidator>();
builder.Services.AddScoped<ICurrentUserContext, CurrentUserContext>();
builder.Services.AddScoped<IActionAuthorizationService, McpAwareActionAuthorizationService>();
builder.Services.AddScoped<IMcpAccessTokenService, McpAccessTokenService>();
builder.Services.AddScoped<ISubjectRepository, SqlServerSubjectRepository>();
builder.Services.AddScoped<ISubjectLayoutRepository, SqlServerSubjectLayoutRepository>();
builder.Services.AddScoped<ITopicRepository, SqlServerTopicRepository>();
builder.Services.AddScoped<IStudyNoteRepository, SqlServerStudyNoteRepository>();
builder.Services.AddScoped<IStudyMetricDefinitionRepository, SqlServerStudyMetricDefinitionRepository>();
builder.Services.AddScoped<ISubjectConnectionRepository, SqlServerSubjectConnectionRepository>();
builder.Services.AddScoped<ISubjectGoalRepository, SqlServerSubjectGoalRepository>();
builder.Services.AddScoped<ISubjectGoalActivityRepository>(sp =>
    (SqlServerSubjectGoalRepository)sp.GetRequiredService<ISubjectGoalRepository>());
builder.Services.AddScoped<ISubjectGoalCompletionRepository, SqlServerSubjectGoalCompletionRepository>();

builder.Services.AddScoped<ISubjectService, SubjectService>();
builder.Services.AddScoped<ITopicService, TopicService>();
builder.Services.AddScoped<IStudyNoteService, StudyNoteService>();
builder.Services.AddScoped<IStudyMetricDefinitionService, StudyMetricDefinitionService>();
builder.Services.AddScoped<ISubjectConnectionService, SubjectConnectionService>();
builder.Services.AddScoped<ISubjectGoalService, SubjectGoalService>();
builder.Services.AddScoped<ISubjectGoalActivityService, SubjectGoalActivityService>();

builder.Services
    .AddAuthentication(McpAccessTokenAuthenticationHandler.AuthenticationScheme)
    .AddScheme<AuthenticationSchemeOptions, McpAccessTokenAuthenticationHandler>(
        McpAccessTokenAuthenticationHandler.AuthenticationScheme,
        _ => { }
    );
builder.Services.AddAuthorization();
builder.Services
    .AddMcpServer()
    .WithHttpTransport(options => options.SessionMode = HttpServerSessionMode.Stateless)
    .WithTools<KnowledgeTools>();

var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();
app.MapMcp("/mcp").RequireAuthorization();
app.Run();
