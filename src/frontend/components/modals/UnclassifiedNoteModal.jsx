import { memo, useEffect, useRef, useState } from 'react';
import { Sparkles, X } from '../../icons';
import { IconButton } from '../IconButton';

function currentLocalDateTime() {
  const now = new Date();
  const local = new Date(now.getTime() - now.getTimezoneOffset() * 60_000);
  return local.toISOString().slice(0, 16);
}

function UnclassifiedNoteModalComponent({ open, onClose, onCreate }) {
  const titleRef = useRef(null);
  const [title, setTitle] = useState('');
  const [content, setContent] = useState('');
  const [studyStartedAt, setStudyStartedAt] = useState(currentLocalDateTime);
  const [studyDuration, setStudyDuration] = useState('00:00');
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    if (!open) return undefined;

    setTitle('');
    setContent('');
    setStudyStartedAt(currentLocalDateTime());
    setStudyDuration('00:00');
    setSaving(false);

    const focusFrame = requestAnimationFrame(() => titleRef.current?.focus());
    const closeOnEscape = event => {
      if (event.key === 'Escape') onClose();
    };
    window.addEventListener('keydown', closeOnEscape);
    return () => {
      cancelAnimationFrame(focusFrame);
      window.removeEventListener('keydown', closeOnEscape);
    };
  }, [onClose, open]);

  if (!open) return null;

  const handleSubmit = async event => {
    event.preventDefault();
    const startedAt = new Date(studyStartedAt);
    if (!title.trim() || !content.trim() || Number.isNaN(startedAt.valueOf())) return;

    setSaving(true);
    try {
      const note = await onCreate(
        title.trim(),
        content.trim(),
        `${studyDuration}:00`,
        startedAt.toISOString(),
        [],
      );
      if (note) onClose();
    } finally {
      setSaving(false);
    }
  };

  const canSubmit = title.trim() && content.trim() && studyStartedAt && !saving;

  return (
    <div className="modal-backdrop" onMouseDown={event => { if (event.target === event.currentTarget) onClose(); }}>
      <form
        className="modal classification-note-modal"
        role="dialog"
        aria-modal="true"
        aria-labelledby="unclassified-note-title"
        aria-describedby="unclassified-note-description"
        onSubmit={handleSubmit}
      >
        <div className="modal-head">
          <div><span>NEW NOTE</span><h2 id="unclassified-note-title">Add an unclassified note</h2></div>
          <IconButton type="button" label="Close note composer" onClick={onClose}><X size={19}/></IconButton>
        </div>
        <p id="unclassified-note-description">Save immediately. The classification service will choose its owning subject and topic in the background.</p>
        <p className="classification-note-status"><Sparkles size={15}/> Subject and topic will be assigned automatically.</p>

        <label>Note title<input ref={titleRef} value={title} onChange={event => setTitle(event.target.value)} placeholder="Name this idea" required/></label>
        <label>Content<textarea value={content} onChange={event => setContent(event.target.value)} placeholder="Capture the thought to classify..." rows="5" required/></label>

        <div className="classification-note-grid">
          <label>Study date and time<input type="datetime-local" value={studyStartedAt} onChange={event => setStudyStartedAt(event.target.value)} required/></label>
          <label>Time spent studying<input type="time" value={studyDuration} onChange={event => setStudyDuration(event.target.value)} step="60" required/></label>
        </div>

        <div className="modal-actions">
          <button type="button" className="ghost-button" onClick={onClose}>Cancel</button>
          <button className="primary-button" disabled={!canSubmit}>{saving ? 'Saving…' : 'Save for classification'}</button>
        </div>
      </form>
    </div>
  );
}

export const UnclassifiedNoteModal = memo(UnclassifiedNoteModalComponent);
