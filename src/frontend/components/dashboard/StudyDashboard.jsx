import { memo, useEffect, useMemo, useState } from 'react';
import { DeadlinePriorities } from './DeadlinePriorities';
import { StudyBehavior } from './StudyBehavior';
import { buildDeadlinePriorities, buildStudyBehaviorData, dashboardPropsEqual, dashboardRangeDates } from './dashboardData';

function StudyDashboard({ subjects, notes, goals, topics, goalActivity, onLoadGoalActivity, onInspectSubject, onPrioritizeGoal }) {
  const [range, setRange] = useState('month');
  const priorities = useMemo(() => buildDeadlinePriorities({ subjects, goals, topics }), [subjects, goals, topics]);
  const behavior = useMemo(() => buildStudyBehaviorData({ subjects, notes, goals, topics, goalActivity, range }), [subjects, notes, goals, topics, goalActivity, range]);
  useEffect(() => {
    const { from, to } = dashboardRangeDates(range);
    void onLoadGoalActivity(from, to);
  }, [goals, onLoadGoalActivity, range]);
  return <section className="deadline-dashboard" aria-label="Dashboard"><DeadlinePriorities priorities={priorities} onInspectSubject={onInspectSubject}/><StudyBehavior behavior={behavior} range={range} onRangeChange={setRange} onInspectSubject={onInspectSubject} onPrioritizeGoal={onPrioritizeGoal}/></section>;
}

export default memo(StudyDashboard, dashboardPropsEqual);
