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

function studyDurationMinutes(duration) {
  const parts = String(duration ?? '').split(':').map(Number);
  if (parts.length < 2 || parts.some(part => !Number.isFinite(part) || part < 0)) return 0;
  const [hours, minutes, seconds = 0] = parts;
  return hours * 60 + minutes + seconds / 60;
}

function buildSubjectTimeHierarchy(subjects, notes) {
  const subjectsById = new Map(subjects.map(subject => [subject.id, subject]));
  const directActivity = new Map();
  const childrenByParentId = new Map();
  subjects.forEach(subject => directActivity.set(subject.id, { minutes: 0, notes: 0 }));

  notes.forEach(note => {
    const activity = directActivity.get(note.subjectId);
    if (!activity) return;
    activity.minutes += studyDurationMinutes(note.studyDuration);
    activity.notes += 1;
  });

  subjects.forEach(subject => {
    if (!subject.parentSubjectId || !subjectsById.has(subject.parentSubjectId) || subject.parentSubjectId === subject.id) return;
    const children = childrenByParentId.get(subject.parentSubjectId) ?? [];
    children.push(subject);
    childrenByParentId.set(subject.parentSubjectId, children);
  });

  const visited = new Set();
  /** @param {any} subject @param {number} depth @returns {any} */
  const buildBranch = (subject, depth) => {
    if (visited.has(subject.id)) return null;
    visited.add(subject.id);
    const direct = directActivity.get(subject.id) ?? { minutes: 0, notes: 0 };
    const children = (childrenByParentId.get(subject.id) ?? [])
      .map(child => buildBranch(child, depth + 1))
      .filter(Boolean)
      .toSorted((left, right) => right.studyMinutes - left.studyMinutes || left.name.localeCompare(right.name));
    const studyMinutes = direct.minutes + children.reduce((sum, child) => sum + child.studyMinutes, 0);
    const noteCount = direct.notes + children.reduce((sum, child) => sum + child.noteCount, 0);
    const descendantCount = children.reduce((sum, child) => sum + child.descendantCount + 1, 0);
    return { ...subject, depth, studyMinutes, directStudyMinutes: direct.minutes, noteCount, directNoteCount: direct.notes, descendantCount, isAggregate: children.length > 0, children };
  };

  const roots = subjects.filter(subject => !subject.parentSubjectId || !subjectsById.has(subject.parentSubjectId) || subject.parentSubjectId === subject.id);
  const branches = roots.map(subject => buildBranch(subject, 0)).filter(Boolean);
  subjects.forEach(subject => {
    if (!visited.has(subject.id)) branches.push(buildBranch(subject, 0));
  });
  branches.sort((left, right) => right.studyMinutes - left.studyMinutes || left.name.localeCompare(right.name));

  const totalStudyMinutes = [...directActivity.values()].reduce((sum, activity) => sum + activity.minutes, 0);
  const subjectActivity = new Array();
  const flatten = branch => {
    subjectActivity.push({ ...branch, percentage: totalStudyMinutes ? branch.studyMinutes / totalStudyMinutes * 100 : 0 });
    branch.children.forEach(flatten);
  };
  branches.forEach(flatten);
  return { subjectActivity, totalStudyMinutes };
}

export function buildStudyBehaviorData({ subjects, notes, goals, topics, goalActivity = [], range, now = new Date() }) {
  const { from } = dashboardRangeDates(range, now);
  const fromDate = parseDate(from);
  const subjectsById = new Map(subjects.map(subject => [subject.id, subject]));
  const topicsById = new Map(topics.map(topic => [topic.id, topic]));
  const visibleNotes = notes.filter(note => new Date(note.studyStartedAtUtc) >= fromDate);
  const totalNotes = visibleNotes.length;
  const { subjectActivity, totalStudyMinutes } = buildSubjectTimeHierarchy(subjects, visibleNotes);
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
  return { subjectActivity, totalNotes, totalStudyMinutes, periodicGoals, goalActivitySeries: buildGoalActivitySeries(goalActivity, range, now) };
}
