export const DAY = 86_400_000;

export const dayKey = value => new Date(value).toISOString().slice(0, 10);
export const clamp = value => Math.max(0, Math.min(100, value));
export const minutesFromDuration = value => {
  const parts = String(value ?? '').split(':').map(Number);
  return parts.length === 3 && parts.every(Number.isFinite) ? parts[0] * 60 + parts[1] + parts[2] / 60 : 0;
};
export const formatDuration = value => value >= 60 ? `${Math.floor(value / 60)}h ${Math.round(value % 60)}m` : `${Math.round(value)}m`;
export const goalDeadline = goal => goal.kind === 2 ? goal.targetDate : goal.periodEndDate;
export const goalProgress = goal => goal.kind === 1 && goal.targetValue > 0 ? clamp(goal.currentValue / goal.targetValue * 100) : 0;

export function getDashboardData({ subjects, notes, goals, notesBySubject, range }) {
  const since = new Date();
  since.setHours(0, 0, 0, 0);
  since.setDate(since.getDate() - range + 1);
  const timeline = Array.from({ length: range }, (_, index) => {
    const date = new Date(since);
    date.setDate(date.getDate() + index);
    return { date: dayKey(date), minutes: 0 };
  });
  const byDate = new Map(timeline.map(point => [point.date, point]));
  const notesInRange = notes.filter(note => new Date(note.studyStartedAtUtc) >= since);
  notesInRange.forEach(note => {
    const point = byDate.get(dayKey(note.studyStartedAtUtc));
    if (point) point.minutes += minutesFromDuration(note.studyDuration);
  });
  const subjectTime = subjects.map(subject => ({
    id: subject.id, name: subject.name, color: subject.color, minutes: (notesBySubject.get(subject.id) ?? []).filter(note => new Date(note.studyStartedAtUtc) >= since).reduce((total, note) => total + minutesFromDuration(note.studyDuration), 0),
  })).toSorted((left, right) => right.minutes - left.minutes);
  const noteCounts = subjects.map(subject => ({ id: subject.id, name: subject.name, color: subject.color, notes: (notesBySubject.get(subject.id) ?? []).filter(note => new Date(note.studyStartedAtUtc) >= since).length })).toSorted((left, right) => right.notes - left.notes);
  const activeGoals = goals.filter(goal => !goal.isCompleted).map(goal => {
    const deadline = goalDeadline(goal);
    const start = goal.periodStartDate ?? dayKey(goal.createdAtUtc);
    const startTime = new Date(`${start}T00:00:00`).getTime();
    const endTime = deadline ? new Date(`${deadline}T00:00:00`).getTime() : null;
    const progress = goalProgress(goal);
    const expected = endTime ? clamp((Date.now() - startTime) / Math.max(1, endTime - startTime) * 100) : null;
    const daysLeft = endTime ? Math.ceil((endTime - Date.now()) / DAY) : null;
    const pace = expected === null ? 'No deadline' : progress >= expected + 8 ? 'Ahead' : progress >= expected - 8 ? 'On track' : 'Behind';
    return { ...goal, deadline, progress, expected, daysLeft, pace };
  }).toSorted((left, right) => (left.daysLeft ?? Infinity) - (right.daysLeft ?? Infinity));
  const attention = subjects.map(subject => {
    const latest = (notesBySubject.get(subject.id) ?? []).toSorted((a, b) => new Date(b.studyStartedAtUtc) - new Date(a.studyStartedAtUtc))[0];
    const daysSinceStudy = latest ? Math.max(0, Math.floor((Date.now() - new Date(latest.studyStartedAtUtc)) / DAY)) : null;
    const subjectGoals = activeGoals.filter(goal => goal.subjectId === subject.id);
    const deadlineRisk = subjectGoals.reduce((score, goal) => score + (goal.daysLeft < 0 ? 80 : goal.daysLeft <= 7 ? 50 : goal.daysLeft <= 21 ? 20 : 0) + (goal.pace === 'Behind' ? 25 : 0), 0);
    return { id: subject.id, name: subject.name, color: subject.color, daysSinceStudy, score: Math.min(100, (daysSinceStudy ?? 45) * 1.4 + deadlineRisk), goals: subjectGoals.length };
  }).toSorted((left, right) => right.score - left.score);
  return { since, timeline, subjectTime, noteCounts, attention, activeGoals, totalMinutes: timeline.reduce((total, point) => total + point.minutes, 0), notesInRange: notesInRange.length };
}

export function getGoalHistory(goal, notes) {
  if (goal.kind !== 1 || !goal.deadline || !goal.targetValue) return [];
  const start = goal.periodStartDate ?? dayKey(goal.createdAtUtc);
  const entries = notes.filter(note => {
    const date = dayKey(note.studyStartedAtUtc);
    return date >= start && date <= goal.deadline && note.metrics.some(metric => metric.definition.id === goal.metricDefinition?.id);
  }).toSorted((a, b) => new Date(a.studyStartedAtUtc) - new Date(b.studyStartedAtUtc));
  const startTime = new Date(`${start}T00:00:00`).getTime();
  const endTime = new Date(`${goal.deadline}T00:00:00`).getTime();
  return entries.reduce((series, note) => {
    const previous = series.at(-1)?.actualValue ?? 0;
    const actualValue = previous + (note.metrics.find(metric => metric.definition.id === goal.metricDefinition?.id)?.value ?? 0);
    const date = dayKey(note.studyStartedAtUtc);
    return [...series, { date, actual: clamp(actualValue / goal.targetValue * 100), expected: clamp((new Date(`${date}T00:00:00`).getTime() - startTime) / Math.max(1, endTime - startTime) * 100), actualValue }];
  }, []);
}
