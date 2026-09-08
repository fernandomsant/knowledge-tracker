import { useEffect, useRef } from 'react';
import { X } from '../../icons';
import { IconButton } from '../IconButton';

export function WorkspaceCreateModal({
  open,
  name,
  error,
  isSubmitting,
  onNameChange,
  onClose,
  onCreate,
}) {
  const inputRef = useRef(null);

  useEffect(() => {
    if (!open) return undefined;
    const frame = requestAnimationFrame(() => inputRef.current?.focus());
    return () => cancelAnimationFrame(frame);
  }, [open]);

  if (!open) return null;

  return (
    <div className="modal-backdrop" onMouseDown={event => { if (event.target === event.currentTarget) onClose(); }}>
      <form className="modal" onSubmit={onCreate}>
        <div className="modal-head">
          <div><span>NEW WORKSPACE</span><h2>Create a workspace</h2></div>
          <IconButton type="button" label="Close modal" onClick={onClose}><X size={19}/></IconButton>
        </div>
        <p>Start a separate knowledge universe for a different focus, project, or season of learning.</p>
        <label>
          Workspace name
          <input
            ref={inputRef}
            value={name}
            onChange={event => onNameChange(event.target.value)}
            placeholder="e.g. Product design"
            maxLength={256}
            disabled={isSubmitting}
          />
        </label>
        {error ? <p className="modal-error" role="alert">{error}</p> : null}
        <div className="modal-actions">
          <button type="button" className="ghost-button" onClick={onClose} disabled={isSubmitting}>Cancel</button>
          <button type="submit" className="primary-button" disabled={!name.trim() || isSubmitting}>
            {isSubmitting ? 'Creating…' : 'Create workspace'}
          </button>
        </div>
      </form>
    </div>
  );
}
