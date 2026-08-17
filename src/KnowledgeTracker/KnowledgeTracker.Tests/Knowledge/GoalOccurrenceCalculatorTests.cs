using KnowledgeTracker.Application.Knowledge;
using KnowledgeTracker.Domain.Knowledge;
using Xunit;

namespace KnowledgeTracker.Tests.Knowledge;

public sealed class GoalOccurrenceCalculatorTests
{
    [Fact]
    public void Daily_occurrences_use_utc_calendar_boundaries()
    {
        var goal = CreateGoal(GoalPeriod.Daily, new DateTimeOffset(2026, 8, 10, 23, 30, 0, TimeSpan.Zero));
        var rows = GoalOccurrenceCalculator.GetOccurrences(goal, new(2026, 8, 10), new(2026, 8, 12), new(2026, 8, 12));

        Assert.Equal(3, rows.Count);
        Assert.Equal(new DateOnly(2026, 8, 10), rows.First().StartDate);
        Assert.All(rows, row => Assert.Equal(row.StartDate, row.EndDate));
    }

    [Fact]
    public void Weekly_occurrences_start_on_monday_and_monthly_on_first_day()
    {
        var weekly = CreateGoal(GoalPeriod.Weekly, new DateTimeOffset(2026, 8, 12, 1, 0, 0, TimeSpan.Zero));
        var monthly = CreateGoal(GoalPeriod.Monthly, new DateTimeOffset(2026, 8, 12, 1, 0, 0, TimeSpan.Zero));

        var week = GoalOccurrenceCalculator.GetOccurrences(weekly, new(2026, 8, 10), new(2026, 8, 16), new(2026, 8, 16)).Single();
        var month = GoalOccurrenceCalculator.GetOccurrences(monthly, new(2026, 8, 1), new(2026, 8, 31), new(2026, 8, 31)).Single();

        Assert.Equal(new DateOnly(2026, 8, 10), week.StartDate);
        Assert.Equal(new DateOnly(2026, 8, 16), week.EndDate);
        Assert.Equal(new DateOnly(2026, 8, 1), month.StartDate);
        Assert.Equal(new DateOnly(2026, 8, 31), month.EndDate);
    }

    [Fact]
    public void Deactivated_goal_is_returned_only_for_overlapping_occurrences()
    {
        var goal = new SubjectGoal(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Goal", GoalKind.TargetDate, null, null, null, GoalPeriod.Daily, null, null, 1, false, null, new DateTimeOffset(2026, 8, 10, 0, 0, 0, TimeSpan.Zero), false, new DateTimeOffset(2026, 8, 12, 10, 0, 0, TimeSpan.Zero));

        var rows = GoalOccurrenceCalculator.GetOccurrences(goal, new(2026, 8, 10), new(2026, 8, 20), new(2026, 8, 20));

        Assert.Equal(3, rows.Count);
        Assert.Equal(new DateOnly(2026, 8, 12), rows.Last().EndDate);
    }

    private static SubjectGoal CreateGoal(GoalPeriod period, DateTimeOffset createdAtUtc) =>
        new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Goal", GoalKind.TargetDate, null, null, null, period, null, null, 1, false, null, createdAtUtc);
}
