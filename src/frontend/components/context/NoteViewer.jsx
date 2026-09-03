import { Clock3, FileText, Pencil, Trash2, X } from '../../icons';
import { IconButton } from '../IconButton';

const studyDateFormatter = new Intl.DateTimeFormat(undefined, { dateStyle: 'medium', timeStyle: 'short' });

export function NoteViewer({ note, onClose, onEdit, onDelete }) {
  const studiedAt = note.studyStartedAtUtc ? studyDateFormatter.format(new Date(note.studyStartedAtUtc)) : 'Not recorded';

  return (
    <section className="note-viewer" aria-labelledby={`note-viewer-${note.id}`}>
      <div className="note-viewer-head">
        <div><span>OPEN NOTE</span><h4 id={`note-viewer-${note.id}`}>{note.title}</h4></div>
        <div className="note-viewer-actions">
          <button type="button" className="text-button" onClick={onEdit}><Pencil size={15}/> Edit note</button>
          <button type="button" className="remove-node-button" onClick={() => { if (window.confirm(`Delete ${note.title}?`)) void onDelete(); }}><Trash2 size={15}/> Delete note</button>
          <IconButton label="Close note" onClick={onClose}><X size={17}/></IconButton>
        </div>
      </div>
      <p className="note-viewer-content">{note.excerpt || 'No content has been added to this note yet.'}</p>
      <dl className="note-viewer-details">
        <div><dt><FileText size={14}/> Studied</dt><dd>{studiedAt}</dd></div>
        <div><dt><Clock3 size={14}/> Time spent</dt><dd>{note.studyDuration || 'Not recorded'}</dd></div>
      </dl>
      {note.metrics?.length ? (
        <div className="note-viewer-metrics" aria-label="Study metrics">
          <span>Study metrics</span>
          <div>{note.metrics.map(metric => <small key={metric.definition.id}>{metric.definition.name}: <strong>{metric.value}</strong></small>)}</div>
        </div>
      ) : null}
    </section>
  );
}
