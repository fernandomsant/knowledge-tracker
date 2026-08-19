import { useMemo, useState } from 'react';
import { ChevronDown } from '../../icons';

const COLORS = { teal: '#2e9483', blue: '#547fc8', amber: '#c58c35', purple: '#8b68b5' };

export function formatStudyMinutes(minutes) {
  const roundedMinutes = Math.round(minutes ?? 0);
  if (roundedMinutes < 60) return `${roundedMinutes}m`;
  const hours = Math.floor(roundedMinutes / 60);
  const remainder = roundedMinutes % 60;
  return remainder ? `${hours}h ${remainder}m` : `${hours}h`;
}

export function SubjectActivityChart({ data, totalStudyMinutes, onInspectSubject }) {
  const [expandedSubjects, setExpandedSubjects] = useState(() => new Set());
  const visibleSubjects = useMemo(() => {
    const visible = [];
    let collapsedDepth = null;

    data.forEach(subject => {
      if (collapsedDepth !== null && subject.depth > collapsedDepth) return;
      if (collapsedDepth !== null) collapsedDepth = null;
      visible.push(subject);
      if (subject.isAggregate && !expandedSubjects.has(subject.id)) collapsedDepth = subject.depth;
    });

    return visible;
  }, [data, expandedSubjects]);

  if (!data.length) return <div className="behavior-empty"><span>No subjects to show.</span></div>;

  const toggleSubject = subject => {
    setExpandedSubjects(current => {
      const next = new Set(current);
      if (!next.has(subject.id)) {
        next.add(subject.id);
        return next;
      }

      next.delete(subject.id);
      const subjectIndex = data.findIndex(candidate => candidate.id === subject.id);
      for (let index = subjectIndex + 1; index < data.length && data[index].depth > subject.depth; index += 1) {
        next.delete(data[index].id);
      }
      return next;
    });
  };

  return (
    <div className="subject-time-tree" role="list" aria-label="Study time by subject hierarchy">
      {!totalStudyMinutes ? <p>No study time recorded in this range.</p> : null}
      {visibleSubjects.map(subject => {
        const percentage = Math.round(subject.percentage);
        const expanded = expandedSubjects.has(subject.id);
        const noteLabel = `${subject.noteCount} ${subject.noteCount === 1 ? 'note' : 'notes'}`;
        const detail = subject.isAggregate
          ? `${noteLabel} across ${subject.descendantCount} descendant ${subject.descendantCount === 1 ? 'subject' : 'subjects'}`
          : noteLabel;

        return (
          <div className="subject-time-item" role="listitem" key={subject.id}>
            <div
              className={`subject-time-row ${subject.isAggregate ? 'aggregate' : 'leaf'} ${subject.depth ? 'nested' : ''} ${expanded ? 'expanded' : ''}`}
              style={{ '--subject-indent': `${subject.depth * 17}px`, '--subject-color': COLORS[subject.color] ?? COLORS.teal }}
            >
              {subject.isAggregate ? (
                <button
                  type="button"
                  className="subject-time-toggle"
                  onClick={() => toggleSubject(subject)}
                  aria-expanded={expanded}
                  aria-label={`${expanded ? 'Hide descendants of' : 'Show children of'} ${subject.name}`}
                >
                  <ChevronDown size={14}/>
                </button>
              ) : <span className="subject-time-toggle-spacer" aria-hidden="true"/>}
              <button
                type="button"
                className="subject-time-content"
                onClick={() => onInspectSubject(subject.id)}
                aria-label={`Open ${subject.name}. ${formatStudyMinutes(subject.studyMinutes)}, ${percentage}% of study time.`}
              >
                <span className="subject-time-identity">
                  <i aria-hidden="true"/>
                  <span>
                    <strong>{subject.name}</strong>
                    <small>{detail}</small>
                  </span>
                </span>
                <span className="subject-time-value">
                  <b>{formatStudyMinutes(subject.studyMinutes)}</b>
                  <small>{percentage}%</small>
                </span>
                <span className="subject-time-track" role="progressbar" aria-valuemin="0" aria-valuemax="100" aria-valuenow={percentage}>
                  <i style={{ width: `${subject.percentage}%` }}/>
                </span>
              </button>
            </div>
          </div>
        );
      })}
    </div>
  );
}
