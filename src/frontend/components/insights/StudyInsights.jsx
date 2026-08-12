import { lazy, Suspense, useMemo, useState } from 'react';
import { CalendarDays, Check, Clock3, FileText, Target, Zap } from '../../icons';
import { formatDuration, getDashboardData } from './dashboardData';

const Charts = lazy(() => import('./DashboardCharts'));

const ranges = [{ value: 7, label: 'Week' }, { value: 30, label: 'Month' }, { value: 90, label: '90 days' }];
const shortDate = new Intl.DateTimeFormat(undefined, { month: 'short', day: 'numeric' });

function PaceBadge({ pace }) {
  const className = pace.toLowerCase().replace(' ', '-');
  return <span className={`pace-badge ${className}`}>{pace}</span>;
}

export function StudyInsights({ subjects, notes, goals, notesBySubject, onInspectSubject }) {
  const [range, setRange] = useState(30);
  const dashboard = useMemo(() => getDashboardData({ subjects, notes, goals, notesBySubject, range }), [subjects, notes, goals, notesBySubject, range]);
  return <section className="analytics-dashboard" aria-label="Study dashboard">
    <header className="analytics-heading">
      <div><span>WORKSPACE ANALYTICS</span><h2>Your study rhythm</h2><p>See what needs attention, where your time is going, and whether your goals are keeping pace.</p></div>
      <div className="analytics-range" role="tablist" aria-label="Dashboard time range">{ranges.map(option => <button key={option.value} type="button" role="tab" aria-selected={range === option.value} className={range === option.value ? 'active' : ''} onClick={() => setRange(option.value)}>{option.label}</button>)}</div>
    </header>
    <div className="analytics-kpis">
      <div><Clock3 size={18}/><span>Study time<strong>{formatDuration(dashboard.totalMinutes)}</strong></span></div>
      <div><FileText size={18}/><span>Notes captured<strong>{dashboard.notesInRange}</strong></span></div>
      <div><Target size={18}/><span>Active goals<strong>{dashboard.activeGoals.length}</strong></span></div>
    </div>
    <div className="analytics-grid">
      <article className="analytics-card activity-card"><header><div><span>STUDY ACTIVITY</span><h3>Daily study time</h3></div><b>{formatDuration(dashboard.totalMinutes)}</b></header><Suspense fallback={<p className="analytics-empty">Loading activity chart…</p>}><Charts kind="activity" data={dashboard.timeline}/></Suspense></article>
      <article className="analytics-card attention-card"><header><div><span>ATTENTION QUEUE</span><h3>Where to focus next</h3></div><Zap size={17}/></header><div className="attention-list">{dashboard.attention.slice(0, 5).map((subject, index) => <button type="button" key={subject.id} onClick={() => onInspectSubject(subject.id)}><em>{index + 1}</em><i className={`color-dot ${subject.color}`}/><span><strong>{subject.name}</strong><small>{subject.daysSinceStudy === null ? 'No study notes yet' : `${subject.daysSinceStudy} days since studying`} · {subject.goals} active goals</small></span><b style={{ '--attention': `${subject.score}%` }}/></button>)}</div></article>
      <article className="analytics-card"><header><div><span>TIME BY SUBJECT</span><h3>Study allocation</h3></div><Clock3 size={17}/></header><Suspense fallback={<p className="analytics-empty">Loading subject chart…</p>}><Charts kind="subject" data={dashboard.subjectTime} dataKey="minutes" name="Study time" onInspect={onInspectSubject}/></Suspense></article>
      <article className="analytics-card"><header><div><span>NOTES BY SUBJECT</span><h3>Knowledge captured</h3></div><FileText size={17}/></header><Suspense fallback={<p className="analytics-empty">Loading note chart…</p>}><Charts kind="subject" data={dashboard.noteCounts} dataKey="notes" name="Notes" onInspect={onInspectSubject}/></Suspense></article>
    </div>
    <section className="goal-control-room"><header><div><span>GOAL CONTROL ROOM</span><h3>Progress and pace</h3></div><small>Click a goal to inspect its subject</small></header><div className="goal-dashboard-list">{dashboard.activeGoals.length ? dashboard.activeGoals.map(goal => <button type="button" className="goal-dashboard-item" key={goal.id} onClick={() => onInspectSubject(goal.subjectId)}><div className="goal-dashboard-top"><span><strong>{goal.title}</strong><small>{goal.metricDefinition?.name ?? 'Manual completion goal'} · {goal.deadline ? `due ${shortDate.format(new Date(`${goal.deadline}T00:00:00`))}` : 'no deadline'}</small></span><PaceBadge pace={goal.pace}/></div><div className="goal-progress"><i style={{ width: `${goal.progress}%` }}/>{goal.expected !== null ? <b style={{ left: `${goal.expected}%` }} title={`Expected pace: ${Math.round(goal.expected)}%`}/> : null}</div><div className="goal-dashboard-meta"><span>{goal.kind === 1 ? `${Math.round(goal.progress)}% complete · ${goal.currentValue} / ${goal.targetValue}` : 'Mark done when complete'}</span><strong>{goal.daysLeft === null ? 'No deadline' : goal.daysLeft < 0 ? `${Math.abs(goal.daysLeft)} days overdue` : `${goal.daysLeft} days left`}</strong></div><Suspense fallback={<p className="analytics-empty">Loading pace chart…</p>}><Charts kind="goal" goal={goal} notes={notes}/></Suspense></button>) : <p className="analytics-empty">Create a goal to see progress and pace here.</p>}</div></section>
    <section className="deadline-overview"><header><div><span>DEADLINE OVERVIEW</span><h3>Upcoming commitments</h3></div><CalendarDays size={17}/></header><div>{dashboard.activeGoals.filter(goal => goal.deadline).slice(0, 6).map(goal => <button type="button" key={goal.id} className={goal.daysLeft < 0 ? 'overdue' : goal.daysLeft <= 7 ? 'urgent' : ''} onClick={() => onInspectSubject(goal.subjectId)}><i style={{ width: `${Math.max(12, goal.expected ?? 12)}%` }}/><span>{goal.title}<small>{goal.daysLeft < 0 ? `${Math.abs(goal.daysLeft)} days overdue` : `${goal.daysLeft} days remaining`}</small></span>{goal.daysLeft < 0 ? <Zap size={14}/> : <Check size={14}/>}</button>)}{dashboard.activeGoals.every(goal => !goal.deadline) ? <p className="analytics-empty">Goals with a due date appear here.</p> : null}</div></section>
  </section>;
}
