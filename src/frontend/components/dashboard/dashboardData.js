const DAY = 86_400_000;
const RANGE_DAYS = { week: 7, month: 30, quarter: 90, all: null };
const PERIOD_LABELS = { 1: 'Daily', 2: 'Weekly', 3: 'Monthly' };

export const clamp = value => Math.max(0, Math.min(100, value));
export const dateLabel = value => new Intl.DateTimeFormat(undefined, { month: 'short', day: 'numeric', year: 'numeric' }).format(new Date(`${value}T00:00:00`));

export function dashboardPropsEqual(previous, next) {
  return previous.goals === next.goals && previous.subjects === next.subjects && previous.notes === next.notes;
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

const atStartOfDay = value => new Date(value.getFullYear(), value.getMonth(), value.getDate());
const dateKey = value => `${value.getFullYear()}-${String(value.getMonth() + 1).padStart(2, '0')}-${String(value.getDate()).padStart(2, '0')}`;
const weekStart = value => { const start = atStartOfDay(value); start.setDate(start.getDate() - (start.getDay() + 6) % 7); return start; };
const monthStart = value => new Date(value.getFullYear(), value.getMonth(), 1);
const occurrenceStart = (date, period) => period === 1 ? atStartOfDay(date) : period === 2 ? weekStart(date) : monthStart(date);
const occurrenceKey = (date, period) => period === 3 ? `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}` : dateKey(date);
const advanceOccurrence = (date, period) => { const next = new Date(date); if (period === 1) next.setDate(next.getDate() + 1); else if (period === 2) next.setDate(next.getDate() + 7); else next.setMonth(next.getMonth() + 1); return next; };

function rangeStart(range, now) {
  const days = RANGE_DAYS[range];
  if (!days) return null;
  const start = atStartOfDay(now);
  start.setDate(start.getDate() - days + 1);
  return start;
}

function goalOccurrences(goal, notes, from, now) {
  const metricId = goal.metricDefinition?.id;
  if (!metricId || !goal.targetValue) return null;
  const goalStart = atStartOfDay(new Date(goal.createdAtUtc));
  const firstDate = from && from > goalStart ? from : goalStart;
  const firstOccurrence = occurrenceStart(firstDate, goal.period);
  const values = new Map();
  notes.filter(note => note.subjectId === goal.subjectId && new Date(note.studyStartedAtUtc) >= goalStart).forEach(note => {
    const value = note.metrics.find(metric => metric.definition.id === metricId)?.value;
    if (value === undefined) return;
    const key = occurrenceKey(occurrenceStart(new Date(note.studyStartedAtUtc), goal.period), goal.period);
    values.set(key, (values.get(key) ?? 0) + Number(value));
  });
  const occurrences = [];
  for (let date = firstOccurrence; date <= now; date = advanceOccurrence(date, goal.period)) {
    const key = occurrenceKey(date, goal.period);
    occurrences.push({ key, completed: (values.get(key) ?? 0) >= Number(goal.targetValue) });
  }
  return occurrences;
}

function currentStreak(occurrences) {
  let streak = 0;
  for (const occurrence of occurrences.toReversed()) { if (!occurrence.completed) break; streak += 1; }
  return streak;
}

function trendFor(occurrences) {
  if (occurrences.length < 4) return 'Limited history';
  const midpoint = Math.floor(occurrences.length / 2);
  const before = occurrences.slice(0, midpoint);
  const recent = occurrences.slice(midpoint);
  const beforeRate = before.filter(occurrence => occurrence.completed).length / before.length;
  const recentRate = recent.filter(occurrence => occurrence.completed).length / recent.length;
  return recentRate > beforeRate ? 'Improving' : recentRate < beforeRate ? 'Declining' : 'Steady';
}

function recurringGoalPriority({ consistency, missed, expected, trend }) {
  if (!expected) return { priority: 'No activity', priorityRank: 3, priorityReason: 'No expected occurrences in this range' };
  if (consistency < 50) return { priority: 'Needs attention', priorityRank: 0, priorityReason: `${missed} missed occurrence${missed === 1 ? '' : 's'}` };
  if (missed > 0 || trend === 'Declining') return { priority: 'Review', priorityRank: 1, priorityReason: trend === 'Declining' ? 'Completion trend is declining' : `${missed} missed occurrence${missed === 1 ? '' : 's'}` };
  return { priority: 'On track', priorityRank: 2, priorityReason: 'All expected occurrences completed' };
}

export function buildStudyBehaviorData({ subjects, notes, goals, range, now = new Date() }) {
  const from = rangeStart(range, now);
  const visibleNotes = notes.filter(note => !from || new Date(note.studyStartedAtUtc) >= from);
  const totalNotes = visibleNotes.length;
  const subjectActivity = subjects.map(subject => {
    const count = visibleNotes.filter(note => note.subjectId === subject.id).length;
    return { ...subject, count, percentage: totalNotes ? count / totalNotes * 100 : 0 };
  }).toSorted((left, right) => right.count - left.count || left.name.localeCompare(right.name));
  const periodicGoals = goals.filter(goal => !goal.isCompleted && goal.kind === 1 && goal.period >= 1 && goal.period <= 3).map(goal => {
    const occurrences = goalOccurrences(goal, notes, from, now) ?? [];
    const completed = occurrences.filter(occurrence => occurrence.completed).length;
    const expected = occurrences.length;
    const missed = expected - completed;
    const consistency = expected ? completed / expected * 100 : 0;
    const trend = trendFor(occurrences);
    return { ...goal, periodLabel: PERIOD_LABELS[goal.period], completed, expected, missed, consistency, streak: currentStreak(occurrences), trend, ...recurringGoalPriority({ consistency, missed, expected, trend }) };
  }).toSorted((left, right) => (left.priorityOrder ?? Number.MAX_SAFE_INTEGER) - (right.priorityOrder ?? Number.MAX_SAFE_INTEGER) || left.priorityRank - right.priorityRank || left.consistency - right.consistency || right.expected - left.expected);
  const unsupportedPeriodicGoals = goals.filter(goal => !goal.isCompleted && goal.kind !== 1 && goal.period >= 1 && goal.period <= 3);
  return { subjectActivity, totalNotes, periodicGoals, unsupportedPeriodicGoals };
}
