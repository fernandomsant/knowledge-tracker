import { memo, useMemo, useState } from 'react';
import { DeadlinePriorities } from './DeadlinePriorities';
import { StudyBehavior } from './StudyBehavior';
import { buildDeadlinePriorities, buildStudyBehaviorData, dashboardPropsEqual } from './dashboardData';

function StudyDashboard({ subjects, notes, goals, topics, onInspectSubject, onPrioritizeGoal }) {
  const [range, setRange] = useState('month');
  const priorities = useMemo(() => buildDeadlinePriorities({ subjects, goals, topics }), [subjects, goals, topics]);
  const behavior = useMemo(() => buildStudyBehaviorData({ subjects, notes, goals, topics, range }), [subjects, notes, goals, topics, range]);
  return <section className="deadline-dashboard" aria-label="Dashboard"><DeadlinePriorities priorities={priorities} onInspectSubject={onInspectSubject}/><StudyBehavior behavior={behavior} range={range} onRangeChange={setRange} onInspectSubject={onInspectSubject} onPrioritizeGoal={onPrioritizeGoal}/></section>;
}

export default memo(StudyDashboard, dashboardPropsEqual);
