import { useState } from 'react';
import { Plus, X } from '../../icons';

export function MetricDefinitionComposer({ onCreate, onCreated }) {
  const [open, setOpen] = useState(false);
  const [name, setName] = useState('');
  const [numberKind, setNumberKind] = useState(1);
  const [saving, setSaving] = useState(false);

  const close = () => {
    setOpen(false);
    setName('');
    setNumberKind(1);
  };

  const submit = async event => {
    event.preventDefault();
    if (!name.trim() || saving) return;
    setSaving(true);
    const definition = await onCreate(name.trim(), numberKind);
    setSaving(false);
    if (!definition) return;
    onCreated(definition);
    close();
  };

  if (!open) {
    return <button type="button" className="metric-create-trigger" onClick={() => setOpen(true)}><Plus size={14}/> Create reusable metric</button>;
  }

  return (
    <form className="metric-definition-composer" onSubmit={submit}>
      <div className="metric-definition-composer-head"><span>New reusable metric</span><button type="button" aria-label="Cancel metric creation" onClick={close}><X size={14}/></button></div>
      <input value={name} onChange={event => setName(event.target.value)} placeholder="e.g. Problems solved" maxLength="256" autoFocus/>
      <fieldset>
        <legend>Value type</legend>
        <label><input type="radio" name="number-kind" checked={numberKind === 1} onChange={() => setNumberKind(1)}/> Natural number <small>0, 1, 2…</small></label>
        <label><input type="radio" name="number-kind" checked={numberKind === 2} onChange={() => setNumberKind(2)}/> Rational number <small>0.5, 1.25…</small></label>
      </fieldset>
      <div><button type="button" className="ghost-button" onClick={close}>Cancel</button><button className="primary-button" disabled={!name.trim() || saving}>{saving ? 'Creating…' : 'Create metric'}</button></div>
    </form>
  );
}
