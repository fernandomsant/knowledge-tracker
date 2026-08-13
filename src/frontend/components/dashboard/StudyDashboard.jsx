import { memo, useMemo } from 'react';
import { DeadlinePriorities } from './DeadlinePriorities';
import { buildDeadlinePriorities, dashboardPropsEqual } from './dashboardData';

function StudyDashboard({ subjects, goals, onInspectSubject }) {
  const priorities = useMemo(() => buildDeadlinePriorities({ subjects, goals }), [subjects, goals]);
  return <section className="deadline-dashboard" aria-label="Dashboard"><DeadlinePriorities priorities={priorities} onInspectSubject={onInspectSubject}/></section>;
}

export default memo(StudyDashboard, dashboardPropsEqual);
