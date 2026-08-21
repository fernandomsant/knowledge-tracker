import { memo, useEffect, useMemo, useRef, useState } from 'react';
import { Sparkles, X } from '../../icons';
import { IconButton } from '../IconButton';

function currentLocalDateTime() {
  const now = new Date();
  const local = new Date(now.getTime() - now.getTimezoneOffset() * 60_000);
  return local.toISOString().slice(0, 16);
}

function UnclassifiedNoteModalComponent({ open, subjects, topics, preferredSubjectId, onClose, onCreate }) {
  const titleRef = useRef(null);
  const leafSubjects = useMemo(() => {
    const parentIds = new Set(subjects.map(subject => subject.parentSubjectId).filter(Boolean));
    return subjects.filter(subject => !parentIds.has(subject.id));
  }, [subjects]);
  const [subjectId, setSubjectId] = useState('');
  const [topicId, setTopicId] = useState('');
  const [title, setTitle] = useState('');
  const [content, setContent] = useState('');
  const [studyStartedAt, setStudyStartedAt] = useState(currentLocalDateTime);
  const [studyDuration, setStudyDuration] = useState('00:00');
  const [saving, setSaving] = useState(false);
  const availableTopics = useMemo(
    () => topics.filter(topic => topic.subjectId === subjectId),
    [subjectId, topics],
  );

  useEffect(() => {
    if (!open) return undefined;

    const nextSubjectId = leafSubjects.some(subject => subject.id === preferredSubjectId)
      ? preferredSubjectId
      : leafSubjects[0]?.id ?? '';
    setSubjectId(nextSubjectId);
    setTopicId(topics.find(topic => topic.subjectId === nextSubjectId)?.id ?? '');
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
  }, [leafSubjects, onClose, open, preferredSubjectId, topics]);

  if (!open) return null;

  const handleSubjectChange = event => {
    const nextSubjectId = event.target.value;
    setSubjectId(nextSubjectId);
    setTopicId(topics.find(topic => topic.subjectId === nextSubjectId)?.id ?? '');
  };

  const handleSubmit = async event => {
    event.preventDefault();
    const startedAt = new Date(studyStartedAt);
    if (!subjectId || !topicId || !title.trim() || !content.trim() || Number.isNaN(startedAt.valueOf())) return;

    setSaving(true);
    try {
      const note = await onCreate(
        subjectId,
        topicId,
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

  const canSubmit = subjectId && topicId && title.trim() && content.trim() && studyStartedAt && !saving;

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
        <p id="unclassified-note-description">Save immediately and let the classifier discover its semantic relationships in the background.</p>
        <p className="classification-note-status"><Sparkles size={15}/> The note will start with Pending classification.</p>

        <label>Note title<input ref={titleRef} value={title} onChange={event => setTitle(event.target.value)} placeholder="Name this idea" required/></label>
        <label>Content<textarea value={content} onChange={event => setContent(event.target.value)} placeholder="Capture the thought to classify..." rows="5" required/></label>

        <div className="classification-note-grid">
          <label>Owning subject<select value={subjectId} onChange={handleSubjectChange} required><option value="">Choose a leaf subject</option>{leafSubjects.map(subject => <option key={subject.id} value={subject.id}>{subject.name}</option>)}</select></label>
          <label>Topic<select value={topicId} onChange={event => setTopicId(event.target.value)} required><option value="">Choose a topic</option>{availableTopics.map(topic => <option key={topic.id} value={topic.id}>{topic.name}</option>)}</select></label>
          <label>Study date and time<input type="datetime-local" value={studyStartedAt} onChange={event => setStudyStartedAt(event.target.value)} required/></label>
          <label>Time spent studying<input type="time" value={studyDuration} onChange={event => setStudyDuration(event.target.value)} step="60" required/></label>
        </div>

        {leafSubjects.length === 0 ? <small className="classification-note-warning">Create a leaf subject before adding notes.</small> : null}
        {subjectId && availableTopics.length === 0 ? <small className="classification-note-warning">This subject needs a topic before it can own a note.</small> : null}

        <div className="modal-actions">
          <button type="button" className="ghost-button" onClick={onClose}>Cancel</button>
          <button className="primary-button" disabled={!canSubmit}>{saving ? 'Saving…' : 'Save for classification'}</button>
        </div>
      </form>
    </div>
  );
}

export const UnclassifiedNoteModal = memo(UnclassifiedNoteModalComponent);
