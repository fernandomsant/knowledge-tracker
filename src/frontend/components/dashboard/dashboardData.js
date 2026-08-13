const DAY = 86_400_000;

export const clamp = value => Math.max(0, Math.min(100, value));
export const deadlineFor = goal => goal.targetDate ?? goal.periodEndDate;
export const dateLabel = value => value ? new Intl.DateTimeFormat(undefined, { month: 'short', day: 'numeric' }).format(new Date(`${value}T00:00:00`)) : 'No deadline';
export const relativeDays = days => days === 0 ? 'today' : days === 1 ? '1 day ago' : `${days} days ago`;

function subjectSnapshotMatches(left, right) {
  return left.length === right.length && left.every((subject, index) => subject.id === right[index].id && subject.name === right[index].name && subject.color === right[index].color);
}

export function dashboardPropsEqual(previous, next) {
  return previous.notes === next.notes && previous.goals === next.goals && subjectSnapshotMatches(previous.subjects, next.subjects);
}

export function buildDashboardData({ subjects, notes, goals }) {
  const totalNotes = notes.length;
  const notesBySubject = new Map(subjects.map(subject => [subject.id, []]));
  notes.forEach(note => notesBySubject.get(note.subjectId)?.push(note));
  const activeGoals = goals.filter(goal => !goal.isCompleted).map(goal => {
    const deadline = deadlineFor(goal);
    const progress = goal.kind === 1 && goal.targetValue > 0 ? clamp(goal.currentValue / goal.targetValue * 100) : 0;
    const start = goal.periodStartDate ?? goal.createdAtUtc?.slice(0, 10);
    const end = deadline ? new Date(`${deadline}T00:00:00`).getTime() : null;
    const startTime = start ? new Date(`${start}T00:00:00`).getTime() : null;
    const elapsed = end && startTime ? clamp((Date.now() - startTime) / Math.max(1, end - startTime) * 100) : null;
    const daysLeft = end ? Math.ceil((end - Date.now()) / DAY) : null;
    const status = daysLeft !== null && daysLeft < 0 ? 'Overdue' : elapsed === null ? 'Open' : progress >= elapsed + 8 ? 'Ahead' : progress >= elapsed - 8 ? 'On track' : 'At risk';
    return { ...goal, deadline, progress, elapsed, daysLeft, status };
  }).toSorted((left, right) => (left.daysLeft ?? Infinity) - (right.daysLeft ?? Infinity));
  const subjectsForReview = subjects.map(subject => {
    const subjectNotes = (notesBySubject.get(subject.id) ?? []).toSorted((left, right) => new Date(right.studyStartedAtUtc) - new Date(left.studyStartedAtUtc));
    const latestNote = subjectNotes[0] ?? null;
    const daysSinceNote = latestNote ? Math.max(0, Math.floor((Date.now() - new Date(latestNote.studyStartedAtUtc)) / DAY)) : null;
    const relevantGoals = activeGoals.filter(goal => goal.subjectId === subject.id);
    const goalRisk = relevantGoals.some(goal => goal.status === 'Overdue' || goal.status === 'At risk');
    const priority = goalRisk || daysSinceNote === null || daysSinceNote >= 14 ? 'Review now' : daysSinceNote >= 7 ? 'Review soon' : 'Recently active';
    const score = (daysSinceNote ?? 45) + (goalRisk ? 35 : 0);
    return { ...subject, latestNote, daysSinceNote, priority, score, goalRisk, activeGoalCount: relevantGoals.length };
  }).toSorted((left, right) => right.score - left.score);
  const focus = [
    ...activeGoals.filter(goal => goal.status === 'Overdue' || goal.status === 'At risk').map(goal => ({ id: `goal-${goal.id}`, subjectId: goal.subjectId, kind: 'Goal', title: goal.title, reason: goal.status === 'Overdue' ? `Deadline passed ${Math.abs(goal.daysLeft)} days ago.` : `${Math.round(goal.progress)}% complete with ${goal.daysLeft} days remaining.` })),
    ...subjectsForReview.filter(subject => subject.priority === 'Review now').map(subject => ({ id: `review-${subject.id}`, subjectId: subject.id, kind: 'Review', title: subject.name, reason: subject.latestNote ? `Last note added ${relativeDays(subject.daysSinceNote)}; no review history is recorded.` : 'No notes or review history are recorded.' })),
  ].slice(0, 5);
  const distribution = subjects.map(subject => {
    const count = notesBySubject.get(subject.id)?.length ?? 0;
    return { id: subject.id, name: subject.name, count, percentage: totalNotes ? count / totalNotes * 100 : 0, color: subject.color };
  }).toSorted((left, right) => right.count - left.count);
  return { activeGoals, subjectsForReview, focus, distribution, totalNotes };
}
