const COLORS = { teal: '#2e9483', blue: '#547fc8', amber: '#c58c35', purple: '#8b68b5' };

export function formatStudyMinutes(minutes) {
  const roundedMinutes = Math.round(minutes ?? 0);
  if (roundedMinutes < 60) return `${roundedMinutes}m`;
  const hours = Math.floor(roundedMinutes / 60);
  const remainder = roundedMinutes % 60;
  return remainder ? `${hours}h ${remainder}m` : `${hours}h`;
}

export function SubjectActivityChart({ data, totalStudyMinutes, onInspectSubject }) {
  if (!data.length) return <div className="behavior-empty"><span>No subjects to show.</span></div>;

  return (
    <div className="subject-time-tree" role="list" aria-label="Study time by subject hierarchy">
      {!totalStudyMinutes ? <p>No study time recorded in this range.</p> : null}
      {data.map(subject => {
        const percentage = Math.round(subject.percentage);
        const noteLabel = `${subject.noteCount} ${subject.noteCount === 1 ? 'note' : 'notes'}`;
        const detail = subject.isAggregate
          ? `${noteLabel} across ${subject.descendantCount} descendant ${subject.descendantCount === 1 ? 'subject' : 'subjects'}`
          : noteLabel;

        return (
          <div className="subject-time-item" role="listitem" key={subject.id}>
            <button
              type="button"
              className={`subject-time-row ${subject.isAggregate ? 'aggregate' : 'leaf'} ${subject.depth ? 'nested' : ''}`}
              style={{ '--subject-depth': subject.depth, '--subject-color': COLORS[subject.color] ?? COLORS.teal }}
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
        );
      })}
    </div>
  );
}
