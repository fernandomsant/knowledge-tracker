import { useState } from 'react';
import { CalendarDays, ChevronDown, Target } from '../../icons';
import { dateLabel } from './dashboardData';

const timeLabel = goal => !goal.hasDeadline ? 'No deadline' : goal.daysLeft < 0 ? `${Math.abs(goal.daysLeft)} ${Math.abs(goal.daysLeft) === 1 ? 'day' : 'days'} overdue` : goal.daysLeft === 0 ? 'Due today' : `${goal.daysLeft} ${goal.daysLeft === 1 ? 'day' : 'days'} left`;
const detailLabel = goal => goal.kind === 1 ? `${goal.metricDefinition?.name ?? 'Metric'} · ${goal.currentValue} / ${goal.targetValue}` : goal.subGoals?.length ? `${goal.subGoals.filter(subGoal => subGoal.isCompleted).length} of ${goal.subGoals.length} sub-goals complete` : 'Completion goal';

function UrgencyBadge({ urgency }) {
  return <span className={`deadline-priority-badge ${urgency.toLowerCase().replace(' ', '-')}`}>{urgency}</span>;
}

export function DeadlinePriorities({ priorities, onInspectSubject }) {
  const [selectedGoalId, setSelectedGoalId] = useState(null);
  return <section className="deadline-priorities-card">
    <header className="deadline-priorities-header">
      <div><span className="deadline-priorities-kicker"><CalendarDays size={15}/> DEADLINE-DRIVEN PRIORITIES</span><h2>Goals that need your attention</h2></div>
      <small>{priorities.length} active {priorities.length === 1 ? 'priority' : 'priorities'}</small>
    </header>
    <div className="deadline-priorities-content">
      {priorities.length ? priorities.map(goal => {
        const expanded = selectedGoalId === goal.id;
        return <article className={`deadline-priority-row ${goal.urgency.toLowerCase().replace(' ', '-')}`} key={goal.id}>
          <button type="button" className="deadline-priority-summary" aria-expanded={expanded} onClick={() => setSelectedGoalId(expanded ? null : goal.id)}>
            <span className="deadline-priority-icon"><Target size={17}/></span>
            <span className="deadline-priority-main"><strong>{goal.title}</strong><small>{goal.subject?.name ?? 'Unknown subject'} · {goal.topic?.name ?? 'Unknown topic'} · {goal.hasDeadline ? dateLabel(goal.targetDate) : 'All time'}</small></span>
            <span className="deadline-priority-progress"><b>{Math.round(goal.progress)}%</b><i><span style={{ width: `${goal.progress}%` }}/></i></span>
            <span className="deadline-priority-deadline"><strong>{timeLabel(goal)}</strong><UrgencyBadge urgency={goal.urgency}/></span>
            <ChevronDown className={expanded ? 'is-expanded' : ''} size={16}/>
          </button>
          {expanded ? <div className="deadline-priority-detail"><div><CalendarDays size={15}/><span>{goal.hasDeadline ? <>Deadline <strong>{dateLabel(goal.targetDate)}</strong></> : 'No deadline set'}</span></div><div><Target size={15}/><span>{detailLabel(goal)}</span></div><button type="button" onClick={() => onInspectSubject(goal.subjectId)}>Open {goal.subject?.name ?? 'subject'}</button></div> : null}
        </article>;
      }) : <div className="deadline-priorities-empty"><CalendarDays size={19}/><div><strong>No active priorities</strong><span>Active all-time goals will appear here, with or without a deadline.</span></div></div>}
    </div>
  </section>;
}
