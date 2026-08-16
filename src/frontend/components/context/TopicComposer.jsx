import { useState } from 'react';
import { Plus, Trash2, X } from '../../icons';

export function TopicComposer({ subjectId, topics = [], onCreate, onCreated, onRemove, onRemoved = () => {} }) {
  const [open, setOpen] = useState(false);
  const [name, setName] = useState('');
  const [saving, setSaving] = useState(false);

  const close = () => {
    setOpen(false);
    setName('');
  };

  const save = async () => {
    if (!name.trim() || saving) return;
    setSaving(true);
    const topic = await onCreate(subjectId, name.trim());
    setSaving(false);
    if (!topic) return;
    onCreated(topic);
    close();
  };

  const remove = async topic => {
    if (!window.confirm(`Delete ${topic.name}?`)) return;
    if (await onRemove(topic.id)) onRemoved(topic);
  };

  if (!open) return <button type="button" className="metric-create-trigger" onClick={() => setOpen(true)}><Plus size={14}/> Manage topics</button>;

  return <div className="topic-manager">
    <div className="metric-definition-composer">
      <div className="metric-definition-composer-head"><span>New topic</span><button type="button" aria-label="Close topic manager" onClick={close}><X size={14}/></button></div>
      <input value={name} onChange={event => setName(event.target.value)} onKeyDown={event => { if (event.key === 'Enter') { event.preventDefault(); void save(); } }} placeholder="e.g. Linux networking" maxLength="256" autoFocus/>
      <div><button type="button" className="primary-button" onClick={() => void save()} disabled={!name.trim() || saving}>{saving ? 'Creating…' : 'Create topic'}</button></div>
    </div>
    {topics.length ? <div className="topic-manager-list"><span>Topics in this subject</span>{topics.map(topic => <div key={topic.id}><small>{topic.name}</small><button type="button" aria-label={`Delete ${topic.name}`} onClick={() => void remove(topic)}><Trash2 size={14}/></button></div>)}</div> : null}
  </div>;
}
