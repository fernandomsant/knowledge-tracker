import { memo, useMemo } from 'react';
import { CalendarDays, Check, FileText, Target, Zap } from '../../icons';
import { NotesDistributionChart } from './NotesDistributionChart';
import { buildDashboardData, dashboardPropsEqual, dateLabel } from './dashboardData';

function Badge({ children, tone }) { return <span className={`dashboard-badge ${tone}`}>{children}</span>; }

function StudyDashboard({ subjects, notes, goals, onInspectSubject }) {
  const dashboard = useMemo(() => buildDashboardData({ subjects, notes, goals }), [subjects, notes, goals]);
  return <section className="study-dashboard" aria-label="Study dashboard">
    <header className="dashboard-title"><div><span>STUDY DASHBOARD</span><h2>Choose the next useful thing.</h2><p>Priorities are based on your notes, active goals, deadlines, and recorded review history.</p></div></header>
    <section className="dashboard-focus"><header><div><Zap size={17}/><span>FOCUS NOW</span></div><small>Ranked from your workspace data</small></header><div>{dashboard.focus.length ? dashboard.focus.map(item => <button type="button" key={item.id} onClick={() => onInspectSubject(item.subjectId)}><Badge tone={item.kind === 'Goal' ? 'warning' : 'attention'}>{item.kind}</Badge><span><strong>{item.title}</strong><small>{item.reason}</small></span><b>Open</b></button>) : <p className="dashboard-empty">No urgent goal or neglected subject needs attention right now.</p>}</div></section>
    <div className="dashboard-grid">
      <section className="dashboard-panel goals-panel"><header><div><Target size={17}/><span>ACTIVE GOALS</span><h3>Progress and deadlines</h3></div></header><div className="goal-overview-list">{dashboard.activeGoals.length ? dashboard.activeGoals.map(goal => <button type="button" key={goal.id} onClick={() => onInspectSubject(goal.subjectId)}><div><strong>{goal.title}</strong><small>{goal.metricDefinition?.name ?? 'Completion goal'} · {dateLabel(goal.deadline)}</small></div><Badge tone={goal.status.toLowerCase().replace(' ', '-')}>{goal.status}</Badge><div className="dashboard-progress"><i style={{ width: `${goal.progress}%` }}/>{goal.elapsed !== null ? <b style={{ left: `${goal.elapsed}%` }} title={`Expected progress: ${Math.round(goal.elapsed)}%`}/> : null}</div><footer><span>{goal.kind === 1 ? `${Math.round(goal.progress)}% · ${goal.currentValue} / ${goal.targetValue}` : 'Mark complete from the subject'}</span><strong>{goal.daysLeft === null ? 'No deadline' : goal.daysLeft < 0 ? `${Math.abs(goal.daysLeft)} days overdue` : `${goal.daysLeft} days left`}</strong></footer></button>) : <p className="dashboard-empty">No active goals. Add one from a subject to track it here.</p>}</div></section>
      <section className="dashboard-panel review-panel"><header><div><CalendarDays size={17}/><span>REVIEW PRIORITY</span><h3>Subjects to revisit</h3></div></header><div className="review-list">{dashboard.subjectsForReview.slice(0, 6).map(subject => <button type="button" key={subject.id} onClick={() => onInspectSubject(subject.id)}><i className={`color-dot ${subject.color}`}/><span><strong>{subject.name}</strong><small>Last reviewed: no history recorded · Last note: {subject.latestNote ? `${relativeDate(subject.daysSinceNote)} (${dateLabel(subject.latestNote.studyStartedAtUtc.slice(0, 10))})` : 'no notes recorded'}</small></span><Badge tone={subject.priority.toLowerCase().replace(' ', '-')}>{subject.priority}</Badge></button>)}</div></section>
      <section className="dashboard-panel distribution-panel"><header><div><FileText size={17}/><span>NOTES BY SUBJECT</span><h3>Knowledge distribution</h3></div><small>{dashboard.totalNotes} total notes</small></header><NotesDistributionChart data={dashboard.distribution} onInspectSubject={onInspectSubject}/></section>
    </div>
  </section>;
}

const relativeDate = days => days === 0 ? 'today' : days === 1 ? '1 day ago' : `${days} days ago`;
export default memo(StudyDashboard, dashboardPropsEqual);
