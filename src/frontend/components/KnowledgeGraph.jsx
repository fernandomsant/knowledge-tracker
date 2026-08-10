import { memo, useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { FileText, Folder, GitBranch, MoreHorizontal, Pencil, Plus, RotateCcw, Trash2, X } from '../icons';
import { IconButton } from './IconButton';

const MIN_ZOOM = 0.55;
const MAX_ZOOM = 1.8;

const edgeKey = (source, target) => [source, target].sort().join(':');

const SubjectNode = memo(function SubjectNode({
  subject,
  noteCount,
  isSelected,
  connectMode,
  zoom,
  onOpen,
  onSelect,
  onMove,
}) {
  const dragRef = useRef(null);
  const movedRef = useRef(false);

  const handlePointerDown = event => {
    if (event.button !== 0) return;
    event.stopPropagation();
    event.currentTarget.setPointerCapture(event.pointerId);
    dragRef.current = {
      pointerId: event.pointerId,
      clientX: event.clientX,
      clientY: event.clientY,
      x: subject.x,
      y: subject.y,
    };
    movedRef.current = false;
  };

  const handlePointerMove = event => {
    const drag = dragRef.current;
    if (!drag || drag.pointerId !== event.pointerId || connectMode) return;
    const deltaX = event.clientX - drag.clientX;
    const deltaY = event.clientY - drag.clientY;
    if (Math.abs(deltaX) + Math.abs(deltaY) > 4) movedRef.current = true;
    onMove(subject.id, drag.x + deltaX / zoom, drag.y + deltaY / zoom);
  };

  const handlePointerUp = event => {
    if (dragRef.current?.pointerId !== event.pointerId) return;
    event.stopPropagation();
    dragRef.current = null;
    if (connectMode) onSelect(subject.id);
    else if (!movedRef.current) onOpen(subject.id);
  };

  return (
    <button
      className={`subject-node ${subject.color} ${isSelected ? 'selected' : ''} ${connectMode ? 'connectable' : ''}`}
      style={{ transform: `translate3d(${subject.x}px, ${subject.y}px, 0)` }}
      onPointerDown={handlePointerDown}
      onPointerMove={handlePointerMove}
      onPointerUp={handlePointerUp}
      onPointerCancel={() => { dragRef.current = null; }}
    >
      <span className="node-top">
        <span className="folder-box"><Folder size={21}/></span>
        <span className="node-arrow">↗</span>
      </span>
      <strong>{subject.name}</strong>
      <span className="note-count">{noteCount} {noteCount === 1 ? 'note' : 'notes'}</span>
      <small>{noteCount ? 'Open to browse notes' : 'Open to add the first note'}</small>
    </button>
  );
});

function SubjectDrawer({ subject, notes, onClose, onAddNote, onUpdateNote, onUpdateSubject, onRemoveSubject }) {
  const [editingId, setEditingId] = useState(null);
  const [title, setTitle] = useState('');
  const [excerpt, setExcerpt] = useState('');
  const [editingSubject, setEditingSubject] = useState(false);
  const [subjectName, setSubjectName] = useState(subject.name);
  const [subjectDescription, setSubjectDescription] = useState(subject.description ?? '');
  const titleRef = useRef(null);
  const subjectNameRef = useRef(null);

  useEffect(() => {
    if (editingId !== null) titleRef.current?.focus();
  }, [editingId]);

  useEffect(() => {
    setSubjectName(subject.name);
    setSubjectDescription(subject.description ?? '');
  }, [subject.description, subject.id, subject.name]);

  useEffect(() => {
    if (editingSubject) subjectNameRef.current?.focus();
  }, [editingSubject]);

  const beginEditing = note => {
    setEditingId(note?.id ?? 'new');
    setTitle(note?.title ?? '');
    setExcerpt(note?.excerpt ?? '');
  };

  const handleSave = async event => {
    event.preventDefault();
    const nextTitle = title.trim();
    if (!nextTitle) return;
    const note = editingId === 'new'
      ? await onAddNote(subject.id, nextTitle, excerpt.trim())
      : await onUpdateNote(editingId, nextTitle, excerpt.trim());
    if (note) setEditingId(null);
  };

  const saveSubject = async event => {
    event.preventDefault();
    const name = subjectName.trim();
    if (!name) return;
    if (await onUpdateSubject(subject.id, name, subjectDescription.trim() || null)) setEditingSubject(false);
  };

  return (
    <aside
      className="subject-drawer"
      onPointerDown={event => event.stopPropagation()}
      onWheel={event => event.stopPropagation()}
    >
      <div className="drawer-head">
        <div><span>SUBJECT DETAILS</span><h3>{subject.name}</h3></div>
        <div className="drawer-actions"><button className="text-button" onClick={() => setEditingSubject(true)}><Pencil size={15}/> Edit</button><button className="remove-node-button" onClick={() => { if (window.confirm(`Remove ${subject.name} and its notes?`)) onRemoveSubject(); }}><Trash2 size={15}/> Remove node</button><IconButton label="Close subject details" onClick={onClose}><X size={19}/></IconButton></div>
      </div>
      <div className={`subject-banner ${subject.color}`}>
        <span><Folder size={24}/></span>
        <div><strong>{subject.name}</strong><small>{notes.length} {notes.length === 1 ? 'note' : 'notes'} in this subject</small></div>
      </div>
      {editingSubject ? (
        <form className="note-editor" onSubmit={saveSubject}>
          <label>Subject name<input ref={subjectNameRef} value={subjectName} onChange={event => setSubjectName(event.target.value)} maxLength="256"/></label>
          <label>Description<textarea value={subjectDescription} onChange={event => setSubjectDescription(event.target.value)} placeholder="What are you studying?" rows="3"/></label>
          <div><button type="button" className="ghost-button" onClick={() => setEditingSubject(false)}>Cancel</button><button className="primary-button">Save subject</button></div>
        </form>
      ) : null}
      <div className="drawer-section-title">
        <div><span>Ideas in this subject</span><small>Capture thoughts while the context is fresh.</small></div>
        <button className="text-button" onClick={() => beginEditing(null)}><Plus size={15}/> Add note</button>
      </div>
      {editingId !== null ? (
        <form className="note-editor" onSubmit={handleSave}>
          <label>Note title<input ref={titleRef} value={title} onChange={event => setTitle(event.target.value)} placeholder="Name this idea"/></label>
          <label>Excerpt<textarea value={excerpt} onChange={event => setExcerpt(event.target.value)} placeholder="Add the key thought..." rows="4"/></label>
          <div><button type="button" className="ghost-button" onClick={() => setEditingId(null)}>Cancel</button><button className="primary-button">Save note</button></div>
        </form>
      ) : null}
      <div className="drawer-notes">
        {notes.map(note => (
          <article className="drawer-note" key={note.id}>
            <span className={`file-box ${subject.color}`}><FileText size={17}/></span>
            <div><strong>{note.title}</strong><p>{note.excerpt || 'No excerpt yet.'}</p><small>{note.date}</small></div>
            <IconButton label={`Edit ${note.title}`} onClick={() => beginEditing(note)}><MoreHorizontal size={18}/></IconButton>
          </article>
        ))}
        {notes.length === 0 && editingId === null ? (
          <div className="empty-state"><FileText size={26}/><strong>No notes here yet</strong><p>Add the first idea to start shaping this subject.</p></div>
        ) : null}
      </div>
    </aside>
  );
}

export function KnowledgeGraph({
  subjects,
  subjectsById,
  notesBySubject,
  connections,
  onMoveSubject,
  onConnect,
  onAddNote,
  onUpdateNote,
  onUpdateSubject,
  onCreateSubject,
  onRemoveSubject,
  onRemoveConnection,
}) {
  const [pan, setPan] = useState({ x: 0, y: 0 });
  const [zoom, setZoom] = useState(1);
  const [connectMode, setConnectMode] = useState(false);
  const [connectionStart, setConnectionStart] = useState(null);
  const [openSubjectId, setOpenSubjectId] = useState(null);
  const [connectionsOpen, setConnectionsOpen] = useState(false);
  const canvasRef = useRef(null);
  const panDragRef = useRef(null);

  const connectionKeys = useMemo(
    () => new Set(connections.map(edge => edgeKey(edge.source, edge.target))),
    [connections]
  );

  const beginPan = useCallback(event => {
    if (event.button !== 0 || event.target.closest('button, .subject-drawer, .canvas-controls')) return;
    event.currentTarget.setPointerCapture(event.pointerId);
    panDragRef.current = {
      pointerId: event.pointerId,
      clientX: event.clientX,
      clientY: event.clientY,
      panX: pan.x,
      panY: pan.y,
    };
  }, [pan]);

  const movePan = useCallback(event => {
    const drag = panDragRef.current;
    if (!drag || drag.pointerId !== event.pointerId) return;
    setPan({ x: drag.panX + event.clientX - drag.clientX, y: drag.panY + event.clientY - drag.clientY });
  }, []);

  const endPan = useCallback(event => {
    if (panDragRef.current?.pointerId === event.pointerId) panDragRef.current = null;
  }, []);

  const handleWheel = useCallback(event => {
    if (event.target.closest('.subject-drawer')) return;
    event.preventDefault();
    event.stopPropagation();
    const rect = canvasRef.current.getBoundingClientRect();
    const nextZoom = Math.min(MAX_ZOOM, Math.max(MIN_ZOOM, zoom * Math.exp(-event.deltaY * 0.0012)));
    if (nextZoom === zoom) return;
    const pointerX = event.clientX - rect.left;
    const pointerY = event.clientY - rect.top;
    const worldX = (pointerX - pan.x) / zoom;
    const worldY = (pointerY - pan.y) / zoom;
    setPan({ x: pointerX - worldX * nextZoom, y: pointerY - worldY * nextZoom });
    setZoom(nextZoom);
  }, [pan, zoom]);

  const selectForConnection = useCallback(id => {
    if (connectionStart === null) {
      setConnectionStart(id);
      return;
    }
    if (connectionStart !== id && !connectionKeys.has(edgeKey(connectionStart, id))) {
      onConnect(connectionStart, id);
    }
    setConnectionStart(null);
  }, [connectionKeys, connectionStart, onConnect]);

  const toggleConnectMode = useCallback(() => {
    setConnectMode(current => !current);
    setConnectionStart(null);
    setOpenSubjectId(null);
  }, []);

  const resetCanvas = useCallback(() => {
    setPan({ x: 0, y: 0 });
    setZoom(1);
  }, []);

  const removeOpenSubject = useCallback(() => {
    if (openSubjectId === null) return;
    onRemoveSubject(openSubjectId);
    setOpenSubjectId(null);
    setConnectionStart(current => current === openSubjectId ? null : current);
  }, [onRemoveSubject, openSubjectId]);

  const openSubject = openSubjectId ? subjectsById.get(openSubjectId) : null;

  return (
    <div className="graph-shell">
      <div
        ref={canvasRef}
        className="graph-canvas"
        onPointerDown={beginPan}
        onPointerMove={movePan}
        onPointerUp={endPan}
        onPointerCancel={endPan}
        onWheelCapture={handleWheel}
      >
        {connectMode ? (
          <div className="connect-banner"><GitBranch size={16}/>{connectionStart ? 'Now choose the second subject.' : 'Click two subjects to create a connection.'}</div>
        ) : null}
        <div className="graph-world" style={{ transform: `translate3d(${pan.x}px, ${pan.y}px, 0) scale(${zoom})` }}>
          <svg className="connections" width="1200" height="720" aria-hidden="true">
            {connections.map(edge => {
              const source = subjectsById.get(edge.source);
              const target = subjectsById.get(edge.target);
              if (!source || !target) return null;
              const x1 = source.x + 105;
              const y1 = source.y + 73;
              const x2 = target.x + 105;
              const y2 = target.y + 73;
              return <g key={edge.id}><line x1={x1} y1={y1} x2={x2} y2={y2}/><circle cx={x1} cy={y1} r="4"/><circle cx={x2} cy={y2} r="4"/></g>;
            })}
          </svg>
          {subjects.map(subject => (
            <SubjectNode
              key={subject.id}
              subject={subject}
              noteCount={notesBySubject.get(subject.id)?.length ?? 0}
              isSelected={connectionStart === subject.id}
              connectMode={connectMode}
              zoom={zoom}
              onOpen={setOpenSubjectId}
              onSelect={selectForConnection}
              onMove={onMoveSubject}
            />
          ))}
        </div>
        <div className="graph-legend"><span><i/>{connections.length} connections</span><span>{connectMode ? 'Choose two subjects' : 'Drag subjects to arrange them'}</span></div>
        <div className="canvas-controls">
          <button onClick={onCreateSubject}><Plus size={16}/> Add node</button>
          <button className={connectionsOpen ? 'active' : ''} onClick={() => setConnectionsOpen(current => !current)}><GitBranch size={16}/> Links ({connections.length})</button>
          <button className={connectMode ? 'active' : ''} onClick={toggleConnectMode}><GitBranch size={16}/>{connectMode ? 'Done' : 'Connect'}</button>
          <span>{Math.round(zoom * 100)}%</span>
          <IconButton label="Reset canvas" onClick={resetCanvas}><RotateCcw size={16}/></IconButton>
        </div>
        {connectionsOpen ? (
          <aside className="connection-panel" onPointerDown={event => event.stopPropagation()}>
            <div className="connection-panel-head"><div><span>CONNECTIONS</span><strong>{connections.length} links</strong></div><IconButton label="Close connections" onClick={() => setConnectionsOpen(false)}><X size={18}/></IconButton></div>
            {connections.length ? connections.map(connection => {
              const source = subjectsById.get(connection.source);
              const target = subjectsById.get(connection.target);
              return <div className="connection-row" key={connection.id}><span>{source?.name ?? 'Unknown'}<small>connected to</small>{target?.name ?? 'Unknown'}</span><IconButton label={`Remove connection between ${source?.name ?? 'subject'} and ${target?.name ?? 'subject'}`} onClick={() => onRemoveConnection(connection.id)}><Trash2 size={16}/></IconButton></div>;
            }) : <p className="connection-empty">Use Connect, then choose two nodes.</p>}
          </aside>
        ) : null}
        {openSubject ? (
          <SubjectDrawer
            subject={openSubject}
            notes={notesBySubject.get(openSubject.id) ?? []}
            onClose={() => setOpenSubjectId(null)}
            onAddNote={onAddNote}
            onUpdateNote={onUpdateNote}
            onUpdateSubject={onUpdateSubject}
            onRemoveSubject={removeOpenSubject}
          />
        ) : null}
      </div>
    </div>
  );
}
