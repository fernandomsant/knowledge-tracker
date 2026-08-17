using KnowledgeTracker.Domain.Knowledge;

namespace KnowledgeTracker.Application.Knowledge;

public sealed record GoalOccurrence(DateOnly StartDate, DateOnly EndDate);

public static class GoalOccurrenceCalculator
{
    public static IReadOnlyCollection<GoalOccurrence> GetOccurrences(SubjectGoal goal, DateOnly from, DateOnly to, DateOnly today)
    {
        if (from > to) return [];
        var created = DateOnly.FromDateTime(goal.CreatedAtUtc.UtcDateTime);
        var deactivated = goal.DeactivatedAtUtc is null
            ? today
            : DateOnly.FromDateTime(goal.DeactivatedAtUtc.Value.UtcDateTime);
        if (goal.Period == GoalPeriod.AllTime && goal.TargetDate is not null && goal.TargetDate.Value < deactivated)
            deactivated = goal.TargetDate.Value;
        if (deactivated < from || created > to) return [];

        return goal.Period switch
        {
            GoalPeriod.Daily => EnumerateRecurring(created, deactivated, from, to, static date => new(date, date)),
            GoalPeriod.Weekly => EnumerateRecurring(created, deactivated, from, to, static date => new(StartOfWeek(date), StartOfWeek(date).AddDays(6))),
            GoalPeriod.Monthly => EnumerateMonthly(created, deactivated, from, to),
            GoalPeriod.Custom => Overlap(goal.CustomPeriodStartDate!.Value, goal.CustomPeriodEndDate!.Value, created, deactivated, from, to)
                ? [new(goal.CustomPeriodStartDate.Value, goal.CustomPeriodEndDate.Value)] : [],
            _ => [new(created, deactivated)]
        };
    }

    private static IReadOnlyCollection<GoalOccurrence> EnumerateRecurring(
        DateOnly created,
        DateOnly deactivated,
        DateOnly from,
        DateOnly to,
        Func<DateOnly, GoalOccurrence> factory)
    {
        var cursor = factory(from).StartDate;
        while (cursor.AddDays(-1) >= from) cursor = cursor.AddDays(-1);
        var results = new List<GoalOccurrence>();
        for (; cursor <= to; cursor = cursor.AddDays(factory(cursor).EndDate.DayNumber - factory(cursor).StartDate.DayNumber + 1))
        {
            var occurrence = factory(cursor);
            if (occurrence.EndDate < created || occurrence.StartDate > deactivated) continue;
            results.Add(occurrence);
        }
        return results;
    }

    private static IReadOnlyCollection<GoalOccurrence> EnumerateMonthly(DateOnly created, DateOnly deactivated, DateOnly from, DateOnly to)
    {
        var cursor = new DateOnly(from.Year, from.Month, 1);
        var results = new List<GoalOccurrence>();
        for (; cursor <= to; cursor = cursor.AddMonths(1))
        {
            var occurrence = new GoalOccurrence(cursor, cursor.AddMonths(1).AddDays(-1));
            if (occurrence.EndDate >= created && occurrence.StartDate <= deactivated) results.Add(occurrence);
        }
        return results;
    }

    private static bool Overlap(DateOnly start, DateOnly end, DateOnly created, DateOnly deactivated, DateOnly from, DateOnly to) =>
        start <= to && end >= from && end >= created && start <= deactivated;

    private static DateOnly StartOfWeek(DateOnly date) => date.AddDays(-((int)date.DayOfWeek + 6) % 7);
}
