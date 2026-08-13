const DAY = 86_400_000;

export const clamp = value => Math.max(0, Math.min(100, value));
export const dateLabel = value => new Intl.DateTimeFormat(undefined, { month: 'short', day: 'numeric', year: 'numeric' }).format(new Date(`${value}T00:00:00`));

export function dashboardPropsEqual(previous, next) {
  return previous.goals === next.goals && previous.subjects === next.subjects;
}

function deadlineStatus(daysLeft, pace) {
  if (daysLeft < 0) return 'Overdue';
  if (daysLeft === 0) return 'Due today';
  if (daysLeft <= 7) return 'Due soon';
  return pace === 'At risk' ? 'At risk' : 'On track';
}

function goalProgress(goal) {
  if (goal.kind === 1 && goal.targetValue > 0) return clamp(goal.currentValue / goal.targetValue * 100);
  const subGoals = goal.subGoals ?? [];
  return subGoals.length ? clamp(subGoals.filter(subGoal => subGoal.isCompleted).length / subGoals.length * 100) : 0;
}

export function buildDeadlinePriorities({ goals, subjects }) {
  const subjectsById = new Map(subjects.map(subject => [subject.id, subject]));
  return goals
    .filter(goal => !goal.isCompleted && goal.period === 0 && goal.targetDate)
    .map(goal => {
      const deadlineTime = new Date(`${goal.targetDate}T00:00:00`).getTime();
      const daysLeft = Math.ceil((deadlineTime - Date.now()) / DAY);
      const progress = goalProgress(goal);
      const createdAt = goal.createdAtUtc ? new Date(goal.createdAtUtc).getTime() : null;
      const elapsed = createdAt ? clamp((Date.now() - createdAt) / Math.max(1, deadlineTime - createdAt) * 100) : null;
      const pace = elapsed === null ? 'On track' : progress >= elapsed - 8 ? 'On track' : 'At risk';
      const urgency = deadlineStatus(daysLeft, pace);
      const urgencyRank = daysLeft < 0 ? 0 : daysLeft === 0 ? 1 : daysLeft <= 7 ? 2 : 3;
      return { ...goal, subject: subjectsById.get(goal.subjectId), progress, elapsed, daysLeft, urgency, urgencyRank, pace };
    })
    .toSorted((left, right) => left.urgencyRank - right.urgencyRank || left.daysLeft - right.daysLeft || left.progress - right.progress);
}
