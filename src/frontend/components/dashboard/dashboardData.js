const DAY = 86_400_000;
const RANGE_DAYS = { week: 7, month: 30, quarter: 90, all: null };
const PERIOD_LABELS = { 0: 'All time', 1: 'Daily', 2: 'Weekly', 3: 'Monthly', 4: 'Custom' };

export const clamp = value => Math.max(0, Math.min(100, value));
export const dateLabel = value => new Intl.DateTimeFormat(undefined, { month: 'short', day: 'numeric', year: 'numeric', timeZone: 'UTC' }).format(new Date(`${value}T00:00:00Z`));

export function dashboardPropsEqual(previous, next) {
  return previous.goals === next.goals && previous.subjects === next.subjects && previous.topics === next.topics && previous.notes === next.notes && previous.goalActivity === next.goalActivity;
}

const dateKey = value => `${value.getUTCFullYear()}-${String(value.getUTCMonth() + 1).padStart(2, '0')}-${String(value.getUTCDate()).padStart(2, '0')}`;
const parseDate = value => new Date(`${value}T00:00:00Z`);
const addDays = (value, days) => new Date(value.getTime() + days * DAY);

export function dashboardRangeDates(range, now = new Date()) {
  const to = new Date(Date.UTC(now.getUTCFullYear(), now.getUTCMonth(), now.getUTCDate()));
  const days = RANGE_DAYS[range];
  const from = days ? addDays(to, -days + 1) : new Date(Date.UTC(to.getUTCFullYear() - 10, 0, 1));
  return { from: dateKey(from), to: dateKey(to) };
}

function deadlineStatus(daysLeft, pace) {
  if (daysLeft < 0) return 'Overdue';
  if (daysLeft === 0) return 'Due today';
  if (daysLeft <= 7) return 'Due soon';
  return pace === 'At risk' ? 'At risk' : 'On track';
}

function goalProgress(goal) {
  if (goal.kind === 1 && goal.targetValue > 0) return clamp((goal.currentValue ?? 0) / goal.targetValue * 100);
  const subGoals = goal.subGoals ?? [];
  return subGoals.length ? clamp(subGoals.filter(subGoal => subGoal.isCompleted).length / subGoals.length * 100) : 0;
}

export function buildDeadlinePriorities({ goals, subjects, topics }) {
  const subjectsById = new Map(subjects.map(subject => [subject.id, subject]));
  const topicsById = new Map(topics.map(topic => [topic.id, topic]));
  return goals
    .filter(goal => !goal.isCompleted && goal.period === 0)
    .map(goal => {
      const hasDeadline = Boolean(goal.targetDate);
      const deadlineTime = hasDeadline ? parseDate(goal.targetDate).getTime() : null;
      const daysLeft = deadlineTime === null ? null : Math.ceil((deadlineTime - Date.now()) / DAY);
      const progress = goalProgress(goal);
      const createdAt = goal.createdAtUtc ? new Date(goal.createdAtUtc).getTime() : null;
      const elapsed = createdAt && deadlineTime !== null ? clamp((Date.now() - createdAt) / Math.max(1, deadlineTime - createdAt) * 100) : null;
      const pace = elapsed === null ? 'On track' : progress >= elapsed - 8 ? 'On track' : 'At risk';
      const urgency = daysLeft === null ? 'No deadline' : deadlineStatus(daysLeft, pace);
      const urgencyRank = daysLeft === null ? 4 : daysLeft < 0 ? 0 : daysLeft === 0 ? 1 : daysLeft <= 7 ? 2 : 3;
      return { ...goal, subject: subjectsById.get(goal.subjectId), topic: topicsById.get(goal.topicId), progress, elapsed, daysLeft, hasDeadline, urgency, urgencyRank, pace };
    })
    .toSorted((left, right) => left.urgencyRank - right.urgencyRank || (left.daysLeft ?? Number.MAX_SAFE_INTEGER) - (right.daysLeft ?? Number.MAX_SAFE_INTEGER) || left.progress - right.progress);
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

export function buildGoalActivitySeries(goalActivity, range, now = new Date()) {
  const { from, to } = dashboardRangeDates(range, now);
  const series = [];
  for (let cursor = parseDate(from); cursor <= parseDate(to); cursor = addDays(cursor, 1)) {
    const key = dateKey(cursor);
    series.push({ date: key, label: dateLabel(key), met: 0, expected: 0 });
  }
  const buckets = new Map(series.map(item => [item.date, item]));
  goalActivity.forEach(row => {
    const bucket = buckets.get(row.occurrenceStartDate) ?? buckets.get(from);
    if (!bucket) return;
    bucket.expected += 1;
    if (row.completedAtUtc) bucket.met += 1;
  });
  return series;
}

export function buildStudyBehaviorData({ subjects, notes, goals, topics, goalActivity = [], range, now = new Date() }) {
  const { from } = dashboardRangeDates(range, now);
  const fromDate = parseDate(from);
  const subjectsById = new Map(subjects.map(subject => [subject.id, subject]));
  const topicsById = new Map(topics.map(topic => [topic.id, topic]));
  const visibleNotes = notes.filter(note => new Date(note.studyStartedAtUtc) >= fromDate);
  const totalNotes = visibleNotes.length;
  const subjectActivity = subjects.map(subject => {
    const count = visibleNotes.filter(note => note.subjectId === subject.id).length;
    return { ...subject, count, percentage: totalNotes ? count / totalNotes * 100 : 0 };
  }).toSorted((left, right) => right.count - left.count || left.name.localeCompare(right.name));
  const activityByGoal = new Map();
  goalActivity.forEach(row => { const rows = activityByGoal.get(row.goalId) ?? []; rows.push(row); activityByGoal.set(row.goalId, rows); });
  const periodicGoals = goals.filter(goal => !goal.isCompleted && goal.period >= 1 && goal.period <= 3).map(goal => {
    const occurrences = (activityByGoal.get(goal.id) ?? []).map(row => ({ ...row, completed: Boolean(row.completedAtUtc) })).toSorted((left, right) => left.occurrenceStartDate.localeCompare(right.occurrenceStartDate));
    const completed = occurrences.filter(occurrence => occurrence.completed).length;
    const expected = occurrences.length;
    const missed = expected - completed;
    const consistency = expected ? completed / expected * 100 : 0;
    const trend = trendFor(occurrences);
    return { ...goal, subject: subjectsById.get(goal.subjectId), topic: topicsById.get(goal.topicId), periodLabel: PERIOD_LABELS[goal.period], completed, expected, missed, consistency, streak: currentStreak(occurrences), trend, hasOccurrenceHistory: true, ...recurringGoalPriority({ consistency, missed, expected, trend }) };
  }).toSorted((left, right) => (left.priorityOrder ?? Number.MAX_SAFE_INTEGER) - (right.priorityOrder ?? Number.MAX_SAFE_INTEGER) || left.priorityRank - right.priorityRank || left.consistency - right.consistency || right.expected - left.expected);
  return { subjectActivity, totalNotes, periodicGoals, goalActivitySeries: buildGoalActivitySeries(goalActivity, range, now) };
}
