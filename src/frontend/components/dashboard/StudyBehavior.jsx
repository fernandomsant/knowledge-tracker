import { useState } from 'react';
import { CalendarDays, Check, FileText, Target } from '../../icons';
import { SubjectActivityChart } from './SubjectActivityChart';
import './StudyBehavior.css';

const RANGE_OPTIONS = [['week', 'Week'], ['month', 'Month'], ['quarter', '3 months'], ['all', 'All time']];

function ConsistencyBadge({ trend }) { return <span className={`behavior-badge ${trend.toLowerCase().replace(' ', '-')}`}>{trend}</span>; }
function PriorityBadge({ priority }) { return <span className={`behavior-priority ${priority.toLowerCase().replace(' ', '-')}`}>{priority}</span>; }

export function StudyBehavior({ behavior, range, onRangeChange, onInspectSubject }) {
  const [selectedGoalId, setSelectedGoalId] = useState(null);
  return <section className="study-behavior-card">
    <header className="study-behavior-header"><div><span><CalendarDays size={15}/> SUBJECT STUDY BEHAVIOR</span><h2>Consistency and study-note activity</h2></div><div className="behavior-range" role="group" aria-label="Study behavior time range">{RANGE_OPTIONS.map(([value, label]) => <button type="button" key={value} className={range === value ? 'active' : ''} aria-pressed={range === value} onClick={() => onRangeChange(value)}>{label}</button>)}</div></header>
    <div className="study-behavior-grid">
      <section className="behavior-panel consistency-panel"><header><div><Target size={16}/><span>RECURRING GOAL CONSISTENCY</span></div><small>{behavior.periodicGoals.length} tracked</small></header>{behavior.periodicGoals.length ? <div className="consistency-list">{behavior.periodicGoals.map(goal => { const selected = selectedGoalId === goal.id; return <article key={goal.id}><button type="button" className="consistency-summary" onClick={() => setSelectedGoalId(selected ? null : goal.id)} aria-expanded={selected}><span><strong>{goal.title}</strong><small>{goal.periodLabel} · {goal.completed} / {goal.expected} met</small></span><span className="consistency-status"><b>{Math.round(goal.consistency)}%</b><PriorityBadge priority={goal.priority}/></span><i><span style={{ width: `${goal.consistency}%` }}/></i></button>{selected ? <div className="consistency-detail"><span>{goal.priorityReason} · {goal.missed} missed · streak {goal.streak} · <ConsistencyBadge trend={goal.trend}/></span><button type="button" onClick={() => onInspectSubject(goal.subjectId)}>Open subject</button></div> : null}</article>; })}</div> : <div className="behavior-empty"><Check size={18}/><span>No metric-based recurring goals in this range.</span></div>}{behavior.unsupportedPeriodicGoals.length ? <p className="behavior-note">{behavior.unsupportedPeriodicGoals.length} completion goal{behavior.unsupportedPeriodicGoals.length === 1 ? '' : 's'} lack recorded recurring completion history.</p> : null}</section>
      <section className="behavior-panel activity-panel"><header><div><FileText size={16}/><span>STUDY NOTES BY SUBJECT</span></div><small>{behavior.totalNotes} in range</small></header><SubjectActivityChart data={behavior.subjectActivity} totalNotes={behavior.totalNotes} onInspectSubject={onInspectSubject}/></section>
    </div>
  </section>;
}
