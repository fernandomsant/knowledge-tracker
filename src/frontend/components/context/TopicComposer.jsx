import { useState } from 'react';
import { Plus, X } from '../../icons';

export function TopicComposer({ onCreate, onCreated }) {
  const [open, setOpen] = useState(false);
  const [name, setName] = useState('');
  const [saving, setSaving] = useState(false);

  const close = () => {
    setOpen(false);
    setName('');
  };

  const submit = async event => {
    event.preventDefault();
    if (!name.trim() || saving) return;
    setSaving(true);
    const topic = await onCreate(name.trim());
    setSaving(false);
    if (!topic) return;
    onCreated(topic);
    close();
  };

  if (!open) return <button type="button" className="metric-create-trigger" onClick={() => setOpen(true)}><Plus size={14}/> Create topic</button>;

  return <form className="metric-definition-composer" onSubmit={submit}>
    <div className="metric-definition-composer-head"><span>New topic</span><button type="button" aria-label="Cancel topic creation" onClick={close}><X size={14}/></button></div>
    <input value={name} onChange={event => setName(event.target.value)} placeholder="e.g. Linux networking" maxLength="256" autoFocus/>
    <div><button type="button" className="ghost-button" onClick={close}>Cancel</button><button className="primary-button" disabled={!name.trim() || saving}>{saving ? 'Creating…' : 'Create topic'}</button></div>
  </form>;
}
