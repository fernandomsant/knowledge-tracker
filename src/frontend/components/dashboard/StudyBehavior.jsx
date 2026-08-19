import { useState } from 'react';
import { CalendarDays, Check, FileText, Target } from '../../icons';
import { formatStudyMinutes, SubjectActivityChart } from './SubjectActivityChart';
import { GoalsMetChart } from './GoalsMetChart';
import './StudyBehavior.css';

const RANGE_OPTIONS = [['week', 'Week'], ['month', 'Month'], ['quarter', '3 months'], ['all', 'All time']];

function ConsistencyBadge({ trend }) { return <span className={`behavior-badge ${trend.toLowerCase().replace(' ', '-')}`}>{trend}</span>; }
function PriorityBadge({ priority }) { return <span className={`behavior-priority ${priority.toLowerCase().replace(' ', '-')}`}>{priority}</span>; }

export function StudyBehavior({ behavior, range, onRangeChange, onInspectSubject, onPrioritizeGoal }) {
  const [selectedGoalId, setSelectedGoalId] = useState(null);

  return (
    <section className="study-behavior-card">
      <header className="study-behavior-header">
        <div>
          <span><CalendarDays size={15}/> SUBJECT STUDY BEHAVIOR</span>
          <h2>Consistency and study time</h2>
        </div>
        <div className="behavior-range" role="group" aria-label="Study behavior time range">
          {RANGE_OPTIONS.map(([value, label]) => (
            <button type="button" key={value} className={range === value ? 'active' : ''} aria-pressed={range === value} onClick={() => onRangeChange(value)}>
              {label}
            </button>
          ))}
        </div>
      </header>

      <div className="study-behavior-grid">
        <section className="behavior-panel consistency-panel">
          <header>
            <div><Target size={16}/><span>RECURRING GOAL CONSISTENCY</span></div>
            <small>{behavior.periodicGoals.length} tracked</small>
          </header>
          {behavior.periodicGoals.length ? (
            <div className="consistency-list">
              {behavior.periodicGoals.map((goal, index) => {
                const selected = selectedGoalId === goal.id;
                const priorityClass = goal.priority.toLowerCase().replace(' ', '-');
                const consistency = Math.round(goal.consistency);

                return (
                  <article className={`consistency-goal ${priorityClass}`} key={goal.id}>
                    <button
                      type="button"
                      className="consistency-summary"
                      onClick={() => setSelectedGoalId(selected ? null : goal.id)}
                      aria-expanded={selected}
                    >
                      <span className="consistency-rank" aria-hidden="true">{String(index + 1).padStart(2, '0')}</span>
                      <span className="consistency-copy">
                        <span className="consistency-heading">
                          <strong>{goal.title}</strong>
                          <PriorityBadge priority={goal.priority}/>
                        </span>
                        <span className="consistency-facts">
                          <span>{goal.topic?.name ?? 'Unknown topic'}</span>
                          <span>{goal.periodLabel}</span>
                          <span><b>{goal.completed}</b> / {goal.expected} met</span>
                        </span>
                      </span>
                      <span className="consistency-score"><b>{consistency}%</b><small>consistent</small></span>
                      <span
                        className="consistency-track"
                        role="progressbar"
                        aria-label={`${goal.title} consistency`}
                        aria-valuemin="0"
                        aria-valuemax="100"
                        aria-valuenow={consistency}
                      >
                        <i style={{ width: `${goal.consistency}%` }}/>
                      </span>
                    </button>
                    {selected ? (
                      <div className="consistency-detail">
                        <span className="consistency-detail-metrics">
                          {goal.hasOccurrenceHistory ? (
                            <>
                              <span><b>{goal.missed}</b> missed</span>
                              <span><b>{goal.streak}</b> streak</span>
                              <span>{goal.priorityReason}</span>
                              <ConsistencyBadge trend={goal.trend}/>
                            </>
                          ) : <span>{goal.priorityReason}</span>}
                        </span>
                        <div className="consistency-actions">
                          <button type="button" disabled={!index} onClick={() => onPrioritizeGoal(goal.id, behavior.periodicGoals[index - 1].id)} aria-label={`Move ${goal.title} up`}>↑</button>
                          <button type="button" disabled={index === behavior.periodicGoals.length - 1} onClick={() => onPrioritizeGoal(goal.id, behavior.periodicGoals[index + 1].id)} aria-label={`Move ${goal.title} down`}>↓</button>
                          <button type="button" onClick={() => onInspectSubject(goal.subjectId)}>Open subject</button>
                        </div>
                      </div>
                    ) : null}
                  </article>
                );
              })}
            </div>
          ) : (
            <div className="behavior-empty"><Check size={18}/><span>No active recurring goals in this range.</span></div>
          )}
        </section>

        <section className="behavior-panel activity-panel">
          <header><div><FileText size={16}/><span>STUDY TIME BY SUBJECT</span></div><small>{formatStudyMinutes(behavior.totalStudyMinutes)} in range</small></header>
          <SubjectActivityChart data={behavior.subjectActivity} totalStudyMinutes={behavior.totalStudyMinutes} onInspectSubject={onInspectSubject}/>
        </section>

        <section className="behavior-panel goals-met-panel">
          <header><div><Target size={16}/><span>GOALS MET OVER TIME</span></div><small>Occurrence start date</small></header>
          <GoalsMetChart series={behavior.goalActivitySeries}/>
        </section>
      </div>
    </section>
  );
}
