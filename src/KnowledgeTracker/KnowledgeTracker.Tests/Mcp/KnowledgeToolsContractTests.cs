using System.ComponentModel;
using System.Reflection;
using KnowledgeTracker.Mcp;
using ModelContextProtocol.Server;
using Xunit;

namespace KnowledgeTracker.Tests.Mcp;

public sealed class KnowledgeToolsContractTests
{
    private static readonly string[] ExpectedMethodNames =
    [
        nameof(KnowledgeTools.ListSubjectsAsync),
        nameof(KnowledgeTools.GetSubjectAsync),
        nameof(KnowledgeTools.CreateSubjectAsync),
        nameof(KnowledgeTools.ListTopicsAsync),
        nameof(KnowledgeTools.CreateTopicAsync),
        nameof(KnowledgeTools.ListNotesAsync),
        nameof(KnowledgeTools.CreateNoteAsync),
        nameof(KnowledgeTools.ListGoalsAsync),
        nameof(KnowledgeTools.CreateGoalAsync),
        nameof(KnowledgeTools.CompleteGoalAsync),
        nameof(KnowledgeTools.ListGoalActivityAsync),
    ];

    private static readonly string[] ExpectedToolNames =
    [
        "list_subjects",
        "get_subject",
        "create_subject",
        "list_topics",
        "create_topic",
        "list_notes",
        "create_note",
        "list_goals",
        "create_goal",
        "complete_goal",
        "list_goal_activity",
    ];

    [Fact]
    public void ToolCatalog_MatchesApplicationOperationMap()
    {
        var tools = typeof(KnowledgeTools)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Where(method => method.GetCustomAttribute<McpServerToolAttribute>() is not null)
            .ToArray();

        Assert.Equal(ExpectedMethodNames, tools.Select(method => method.Name));
        Assert.Equal(ExpectedToolNames, tools
            .Select(method => method.GetCustomAttribute<McpServerToolAttribute>()!.Name));
        Assert.All(tools, method =>
        {
            var description = method.GetCustomAttribute<DescriptionAttribute>()?.Description;
            Assert.False(string.IsNullOrWhiteSpace(description));
            Assert.Contains("Requires ", description, StringComparison.Ordinal);
        });
    }

    [Fact]
    public void ToolParameters_PreserveRequiredAndOptionalApplicationInputs()
    {
        Assert.Equal(
            ["name", "description", "parentSubjectId", "cancellationToken"],
            ParametersOf(nameof(KnowledgeTools.CreateSubjectAsync)));
        Assert.Equal(
            ["subjectId", "request", "cancellationToken"],
            ParametersOf(nameof(KnowledgeTools.CreateNoteAsync)));
        Assert.Equal(
            ["from", "to", "cancellationToken"],
            ParametersOf(nameof(KnowledgeTools.ListGoalActivityAsync)));

        var createSubjectParameters = Method(nameof(KnowledgeTools.CreateSubjectAsync)).GetParameters();
        var nullability = new NullabilityInfoContext();
        Assert.False(createSubjectParameters[0].ParameterType.IsGenericType);
        Assert.Equal(typeof(string), createSubjectParameters[0].ParameterType);
        Assert.Equal(typeof(string), createSubjectParameters[1].ParameterType);
        Assert.Equal(NullabilityState.Nullable, nullability.Create(createSubjectParameters[1]).ReadState);
        Assert.Equal(typeof(Guid?), createSubjectParameters[2].ParameterType);
    }

    private static string[] ParametersOf(string methodName) =>
        Method(methodName).GetParameters().Select(parameter => parameter.Name!).ToArray();

    private static MethodInfo Method(string methodName) =>
        typeof(KnowledgeTools).GetMethod(methodName)!;
}
