import { memo, useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { FileText, Folder, GitBranch, MoreHorizontal, Pencil, Plus, RotateCcw, Trash2, X } from '../icons';
import { IconButton } from './IconButton';
import { MetricDefinitionComposer } from './context/MetricDefinitionComposer';
import { NoteViewer } from './context/NoteViewer';
import { SubjectGoals } from './context/SubjectGoals';
import { TopicComposer } from './context/TopicComposer';
import { getSubjectHierarchyEdges, getSubjectParentOptions } from '../knowledge/utils/subjectHierarchy';
import { CANVAS_WORLD_HEIGHT, CANVAS_WORLD_WIDTH, NODE_HEIGHT, NODE_WIDTH, layoutSubjects } from '../knowledge/utils/subjectLayout';

const MIN_ZOOM = 0.18;
const MAX_ZOOM = 2.2;

const edgeKey = (source, target) => [source, target].sort().join(':');

const toDateTimeLocalValue = value => {
  const date = value ? new Date(value) : new Date();
  return new Date(date.getTime() - date.getTimezoneOffset() * 60_000).toISOString().slice(0, 16);
};

const SubjectNode = memo(function SubjectNode({
  subject,
  noteCount,
  isSelected,
  connectMode,
  zoom,
  onOpen,
  onSelect,
  onMove,
  onMoveEnd,
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
    dragRef.current = null;
    if (movedRef.current) onMoveEnd(subject.id);
  };

  const handleClick = () => {
    if (movedRef.current) {
      movedRef.current = false;
      return;
    }
    if (connectMode) onSelect(subject.id);
    else onOpen(subject.id);
  };

  return (
    <button
      className={`subject-node ${subject.color} ${isSelected ? 'selected' : ''} ${connectMode ? 'connectable' : ''}`}
      style={{ transform: `translate3d(${subject.x}px, ${subject.y}px, 0)` }}
      onPointerDown={handlePointerDown}
      onPointerMove={handlePointerMove}
      onPointerUp={handlePointerUp}
      onPointerCancel={() => { dragRef.current = null; }}
      onClick={handleClick}
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

function SubjectDrawer({ subject, subjects, topics, notes, goals, metricDefinitions, drawer, onDrawerChange, onClose, onAddNote, onUpdateNote, onRemoveNote, onCreateMetricDefinition, onCreateTopic, onUpdateSubject, onRemoveSubject, onCreateGoal, onRemoveGoal, onCompleteGoal, onSetSubGoalCompletion }) {
  const { editingId, title, excerpt, topicId, studyStartedAt, studyDuration, metrics, editingSubject, subjectName, subjectDescription, parentSubjectId } = drawer;
  const subjectTopics = topics.filter(topic => topic.subjectId === subject.id);
  const titleRef = useRef(null);
  const subjectNameRef = useRef(null);
  const [viewingNoteId, setViewingNoteId] = useState(null);
  const parentOptions = useMemo(() => getSubjectParentOptions(subjects, subject.id), [subject.id, subjects]);
  const directChildCount = useMemo(() => subjects.filter(candidate => candidate.parentSubjectId === subject.id).length, [subject.id, subjects]);
  const viewingNote = useMemo(() => notes.find(note => note.id === viewingNoteId) ?? null, [notes, viewingNoteId]);

  useEffect(() => {
    if (editingId !== null) titleRef.current?.focus();
  }, [editingId]);

  useEffect(() => {
    if (editingSubject) subjectNameRef.current?.focus();
  }, [editingSubject]);

  const beginEditing = note => {
    setViewingNoteId(null);
    onDrawerChange({
      editingId: note?.id ?? 'new',
      title: note?.title ?? '',
      excerpt: note?.excerpt ?? '',
      topicId: note?.topicId ?? subjectTopics[0]?.id ?? '',
      studyStartedAt: toDateTimeLocalValue(note?.studyStartedAtUtc),
      studyDuration: note?.studyDuration?.slice(0, 5) ?? '00:00',
      metrics: note?.metrics?.map(metric => ({ definitionId: metric.definition.id, value: String(metric.value) })) ?? [],
    });
  };

  const openNote = note => {
    setViewingNoteId(note.id);
    onDrawerChange({ editingId: null });
  };

  const handleSave = async event => {
    event.preventDefault();
    const nextTitle = title.trim();
    if (!nextTitle) return;
    const nextStudyStartedAt = new Date(studyStartedAt);
    if (Number.isNaN(nextStudyStartedAt.valueOf())) return;
    const nextStudyStartedAtUtc = nextStudyStartedAt.toISOString();
    const nextStudyDuration = `${studyDuration}:00`;
    const nextMetrics = metrics
      .filter(metric => metric.definitionId || metric.value.trim())
      .map(metric => ({ definitionId: metric.definitionId, value: Number(metric.value) }));
    if (nextMetrics.some(metric => !metric.definitionId || !Number.isFinite(metric.value) || metric.value < 0)) return;
    const note = editingId === 'new'
      ? await onAddNote(subject.id, topicId, nextTitle, excerpt.trim(), nextStudyDuration, nextStudyStartedAtUtc, nextMetrics)
      : await onUpdateNote(editingId, topicId, nextTitle, excerpt.trim(), nextStudyDuration, nextStudyStartedAtUtc, nextMetrics);
    if (note) onDrawerChange({ editingId: null });
  };

  const saveSubject = async event => {
    event.preventDefault();
    const name = subjectName.trim();
    if (!name) return;
    if (await onUpdateSubject(subject.id, name, subjectDescription.trim() || null, parentSubjectId || null)) onDrawerChange({ editingSubject: false });
  };

  const startSubjectEditing = event => {
    event.preventDefault();
    event.stopPropagation();
    setViewingNoteId(null);
    onDrawerChange({
      editingSubject: true,
      subjectName: subject.name,
      subjectDescription: subject.description ?? '',
      parentSubjectId: subject.parentSubjectId ?? '',
    });
  };

  return (
    <aside
      className="subject-drawer"
      onPointerDown={event => event.stopPropagation()}
      onPointerUp={event => event.stopPropagation()}
      onWheel={event => event.stopPropagation()}
    >
      <div className="drawer-head">
        <div><span>SUBJECT DETAILS</span><h3>{subject.name}</h3></div>
        <div className="drawer-actions">
          {editingSubject ? <button type="submit" form="subject-editor" className="primary-button">Save changes</button> : <button type="button" className="text-button" onClick={startSubjectEditing}><Pencil size={15}/> Edit</button>}
          <button className="remove-node-button" onClick={() => { const childNotice = directChildCount ? ` ${directChildCount} child ${directChildCount === 1 ? 'subject will' : 'subjects will'} become top-level.` : ''; if (window.confirm(`Remove ${subject.name} and its notes?${childNotice}`)) onRemoveSubject(); }}><Trash2 size={15}/> Remove node</button>
          <IconButton label="Close subject details" onClick={onClose}><X size={19}/></IconButton>
        </div>
      </div>
      <div className={`subject-banner ${subject.color}`}>
        <span><Folder size={24}/></span>
        <div><strong>{subject.name}</strong><small>{notes.length} {notes.length === 1 ? 'note' : 'notes'} in this subject</small></div>
      </div>
      {editingSubject ? (
        <form id="subject-editor" className="note-editor" onSubmit={saveSubject}>
          <label>Subject name<input ref={subjectNameRef} value={subjectName} onChange={event => onDrawerChange({ subjectName: event.target.value })} maxLength="256"/></label>
          <label>Description<textarea value={subjectDescription} onChange={event => onDrawerChange({ subjectDescription: event.target.value })} placeholder="What are you studying?" rows="3"/></label>
          <label>Parent subject<select value={parentSubjectId} onChange={event => onDrawerChange({ parentSubjectId: event.target.value })}><option value="">Top-level subject</option>{parentOptions.map(candidate => <option key={candidate.id} value={candidate.id}>{candidate.label}</option>)}</select><small className="hierarchy-hint">Descendants and fifth-level parents are unavailable.</small></label>
          <div><button type="button" className="ghost-button" onClick={() => onDrawerChange({ editingSubject: false })}>Cancel</button></div>
        </form>
      ) : null}
      <SubjectGoals subjectId={subject.id} goals={goals} notes={notes} topics={subjectTopics} metricDefinitions={metricDefinitions} onCreateTopic={onCreateTopic} onCreate={goal => onCreateGoal(subject.id, goal)} onRemove={onRemoveGoal} onComplete={onCompleteGoal} onSetSubGoalCompletion={onSetSubGoalCompletion}/>
      <div className="drawer-section-title">
        <div><span>Ideas in this subject</span><small>Capture thoughts while the context is fresh.</small></div>
        <button className="text-button" onClick={() => beginEditing(null)}><Plus size={15}/> Add note</button>
      </div>
      {editingId !== null ? (
        <form className="note-editor" onSubmit={handleSave}>
          <label>Note title<input ref={titleRef} value={title} onChange={event => onDrawerChange({ title: event.target.value })} placeholder="Name this idea"/></label>
          <label>Excerpt<textarea value={excerpt} onChange={event => onDrawerChange({ excerpt: event.target.value })} placeholder="Add the key thought..." rows="4"/></label>
          <label>Topic<select value={topicId} onChange={event => onDrawerChange({ topicId: event.target.value })} required><option value="">Choose a topic</option>{subjectTopics.map(topic => <option key={topic.id} value={topic.id}>{topic.name}</option>)}</select></label>
          <TopicComposer subjectId={subject.id} onCreate={onCreateTopic} onCreated={topic => onDrawerChange({ topicId: topic.id })}/>
          <label>Study date and time<input type="datetime-local" value={studyStartedAt} onChange={event => onDrawerChange({ studyStartedAt: event.target.value })} required/></label>
          <label>Time spent studying<input type="time" value={studyDuration} onChange={event => onDrawerChange({ studyDuration: event.target.value })} step="60" required/></label>
          <section className="note-metrics" aria-label="Study metrics">
            <div className="note-metrics-head"><span>Study metrics</span><button type="button" className="text-button" onClick={() => onDrawerChange({ metrics: [...metrics, { definitionId: '', value: '' }] })}><Plus size={14}/> Add metric</button></div>
            {metrics.map((metric, index) => (
              <div className="metric-row" key={`${editingId}-metric-${index}`}>
                <select aria-label="Metric" value={metric.definitionId} onChange={event => onDrawerChange({ metrics: metrics.map((item, itemIndex) => itemIndex === index ? { ...item, definitionId: event.target.value } : item) })}><option value="">Choose a metric</option>{metricDefinitions.map(definition => <option key={definition.id} value={definition.id}>{definition.name} ({definition.numberKind === 1 ? 'natural' : 'rational'})</option>)}</select>
                <input aria-label="Metric value" type="number" min="0" step="0.01" value={metric.value} onChange={event => onDrawerChange({ metrics: metrics.map((item, itemIndex) => itemIndex === index ? { ...item, value: event.target.value } : item) })} placeholder="0"/>
                <IconButton label="Remove metric" onClick={() => onDrawerChange({ metrics: metrics.filter((_, itemIndex) => itemIndex !== index) })}><X size={15}/></IconButton>
              </div>
            ))}
            {metrics.length === 0 ? <small>Study date and duration are always recorded. Pages read and exercises done are ready to use.</small> : null}
            <MetricDefinitionComposer
              onCreate={onCreateMetricDefinition}
              onCreated={definition => onDrawerChange({ metrics: [...metrics, { definitionId: definition.id, value: '' }] })}
            />
          </section>
          <div><button type="button" className="ghost-button" onClick={() => onDrawerChange({ editingId: null })}>Cancel</button><button className="primary-button">Save note</button></div>
        </form>
      ) : null}
      <div className="drawer-notes">
        {notes.map(note => (
          <div className="drawer-note-item" key={note.id}>
            <article className="drawer-note">
              <span className={`file-box ${subject.color}`}><FileText size={17}/></span>
              <button type="button" className="drawer-note-preview" onClick={() => openNote(note)}><strong>{note.title}</strong><p>{note.excerpt || 'No excerpt yet.'}</p><small>{note.date}</small></button>
              <IconButton label={`Edit ${note.title}`} onClick={() => beginEditing(note)}><MoreHorizontal size={18}/></IconButton>
            </article>
            {viewingNote?.id === note.id ? <NoteViewer note={note} onClose={() => setViewingNoteId(null)} onEdit={() => beginEditing(note)} onDelete={async () => { if (await onRemoveNote(note.id)) setViewingNoteId(null); }}/> : null}
          </div>
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
  topics,
  subjectsById,
  notesBySubject,
  connections,
  canvasContext,
  onCanvasContextChange,
  onConnect,
  onAddNote,
  metricDefinitions,
  goalsBySubject,
  onCreateMetricDefinition,
  onCreateTopic,
  onUpdateNote,
  onRemoveNote,
  onUpdateSubject,
  onCreateSubject,
  onRemoveSubject,
  onRemoveConnection,
  onCreateGoal,
  onRemoveGoal,
  onCompleteGoal,
  onSetSubGoalCompletion,
  onSaveLayout,
}) {
  const { pan, zoom, connectMode, connectionStart, openSubjectId, connectionsOpen, drawer, hasCenteredSubjectLayout } = canvasContext;
  const canvasRef = useRef(null);
  const panDragRef = useRef(null);

  const updateCanvasContext = useCallback(update => {
    onCanvasContextChange(current => ({ ...current, ...update }));
  }, [onCanvasContextChange]);

  const updateDrawer = useCallback(update => {
    onCanvasContextChange(current => ({ ...current, drawer: { ...current.drawer, ...update } }));
  }, [onCanvasContextChange]);

  const connectionKeys = useMemo(
    () => new Set(connections.map(edge => edgeKey(edge.source, edge.target))),
    [connections]
  );
  const hierarchyEdges = useMemo(() => getSubjectHierarchyEdges(subjects), [subjects]);
  const positionedSubjects = useMemo(() => layoutSubjects(subjects, connections), [connections, subjects]);
  const manualPositions = canvasContext.nodePositions ?? {};
  const manualPositionsRef = useRef(manualPositions);
  useEffect(() => {
    manualPositionsRef.current = manualPositions;
  }, [manualPositions]);
  const displaySubjects = useMemo(() => positionedSubjects.map(subject => {
    const persistedPosition = subject.layoutPosition
      ? {
          x: subject.layoutPosition.normalizedX * (CANVAS_WORLD_WIDTH - NODE_WIDTH),
          y: subject.layoutPosition.normalizedY * (CANVAS_WORLD_HEIGHT - NODE_HEIGHT),
        }
      : null;
    return { ...subject, ...persistedPosition, ...manualPositions[subject.id] };
  }), [manualPositions, positionedSubjects]);
  const displaySubjectsById = useMemo(() => new Map(displaySubjects.map(subject => [subject.id, subject])), [displaySubjects]);
  const positionedHierarchyEdges = useMemo(() => getSubjectHierarchyEdges(displaySubjects), [displaySubjects]);

  const getCenteredPan = useCallback((subjectsToCenter, nextZoom) => {
    const canvas = canvasRef.current;
    if (!canvas || !subjectsToCenter.length) return { x: 0, y: 0 };
    const centroid = subjectsToCenter.reduce(
      (total, subject) => ({ x: total.x + subject.x + NODE_WIDTH / 2, y: total.y + subject.y + NODE_HEIGHT / 2 }),
      { x: 0, y: 0 },
    );
    const canvasBounds = canvas.getBoundingClientRect();
    return {
      x: canvasBounds.width / 2 - (centroid.x / subjectsToCenter.length) * nextZoom,
      y: canvasBounds.height / 2 - (centroid.y / subjectsToCenter.length) * nextZoom,
    };
  }, []);

  useEffect(() => {
    if (hasCenteredSubjectLayout || !displaySubjects.length) return;
    updateCanvasContext({
      pan: getCenteredPan(displaySubjects, zoom),
      hasCenteredSubjectLayout: true,
    });
  }, [displaySubjects, getCenteredPan, hasCenteredSubjectLayout, updateCanvasContext, zoom]);

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
    updateCanvasContext({ pan: { x: drag.panX + event.clientX - drag.clientX, y: drag.panY + event.clientY - drag.clientY } });
  }, [updateCanvasContext]);

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
    updateCanvasContext({ pan: { x: pointerX - worldX * nextZoom, y: pointerY - worldY * nextZoom }, zoom: nextZoom });
  }, [pan, updateCanvasContext, zoom]);

  const selectForConnection = useCallback(id => {
    if (connectionStart === null) {
      updateCanvasContext({ connectionStart: id });
      return;
    }
    if (connectionStart !== id && !connectionKeys.has(edgeKey(connectionStart, id))) {
      onConnect(connectionStart, id);
    }
    updateCanvasContext({ connectionStart: null });
  }, [connectionKeys, connectionStart, onConnect, updateCanvasContext]);

  const toggleConnectMode = useCallback(() => {
    updateCanvasContext({ connectMode: !connectMode, connectionStart: null, openSubjectId: null });
  }, [connectMode, updateCanvasContext]);

  const resetCanvas = useCallback(() => {
    const gatheredPositions = Object.fromEntries(positionedSubjects.map(subject => [
      subject.id,
      { x: subject.x, y: subject.y },
    ]));
    manualPositionsRef.current = gatheredPositions;
    onCanvasContextChange(current => ({ ...current, nodePositions: gatheredPositions }));
    void onSaveLayout(positionedSubjects.map(subject => ({
      subjectId: subject.id,
      normalizedX: subject.x / (CANVAS_WORLD_WIDTH - NODE_WIDTH),
      normalizedY: subject.y / (CANVAS_WORLD_HEIGHT - NODE_HEIGHT),
    })));
    updateCanvasContext({
      pan: getCenteredPan(positionedSubjects, 1),
      zoom: 1,
      hasCenteredSubjectLayout: true,
    });
  }, [getCenteredPan, onCanvasContextChange, onSaveLayout, positionedSubjects, updateCanvasContext]);

  const moveSubject = useCallback((id, x, y) => {
    const position = {
      x: Math.min(CANVAS_WORLD_WIDTH - NODE_WIDTH, Math.max(0, x)),
      y: Math.min(CANVAS_WORLD_HEIGHT - NODE_HEIGHT, Math.max(0, y)),
    };
    manualPositionsRef.current = { ...manualPositionsRef.current, [id]: position };
    onCanvasContextChange(current => ({
      ...current,
      nodePositions: { ...(current.nodePositions ?? {}), [id]: position },
    }));
  }, [onCanvasContextChange]);

  const saveMovedSubject = useCallback(id => {
    const position = manualPositionsRef.current[id];
    if (!position) return;
    void onSaveLayout([{
      subjectId: id,
      normalizedX: position.x / (CANVAS_WORLD_WIDTH - NODE_WIDTH),
      normalizedY: position.y / (CANVAS_WORLD_HEIGHT - NODE_HEIGHT),
    }]);
  }, [onSaveLayout]);

  const removeOpenSubject = useCallback(() => {
    if (openSubjectId === null) return;
    onRemoveSubject(openSubjectId);
    updateCanvasContext({
      openSubjectId: null,
      connectionStart: connectionStart === openSubjectId ? null : connectionStart,
    });
  }, [connectionStart, onRemoveSubject, openSubjectId, updateCanvasContext]);

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
            <svg className="connections" width={CANVAS_WORLD_WIDTH} height={CANVAS_WORLD_HEIGHT} aria-hidden="true">
            <defs>
              <marker id="hierarchy-arrow-1" markerWidth="8" markerHeight="8" refX="7" refY="4" orient="auto"><path d="M0,0 L8,4 L0,8 Z" fill="#2e9483"/></marker>
              <marker id="hierarchy-arrow-2" markerWidth="8" markerHeight="8" refX="7" refY="4" orient="auto"><path d="M0,0 L8,4 L0,8 Z" fill="#547fc8"/></marker>
              <marker id="hierarchy-arrow-3" markerWidth="8" markerHeight="8" refX="7" refY="4" orient="auto"><path d="M0,0 L8,4 L0,8 Z" fill="#c58c35"/></marker>
            </defs>
            {positionedHierarchyEdges.map(edge => {
              const x1 = edge.parent.x + 105;
              const y1 = edge.parent.y + 73;
              const x2 = edge.child.x + 105;
              const y2 = edge.child.y + 73;
              return <line key={edge.id} className={`hierarchy-edge hierarchy-level-${edge.level}`} x1={x1} y1={y1} x2={x2} y2={y2} markerEnd={`url(#hierarchy-arrow-${Math.min(edge.level, 3)})`}/>;
            })}
            {connections.map(edge => {
              const source = displaySubjectsById.get(edge.source);
              const target = displaySubjectsById.get(edge.target);
              if (!source || !target) return null;
              const x1 = source.x + 105;
              const y1 = source.y + 73;
              const x2 = target.x + 105;
              const y2 = target.y + 73;
              return <g key={edge.id}><line x1={x1} y1={y1} x2={x2} y2={y2}/><circle cx={x1} cy={y1} r="4"/><circle cx={x2} cy={y2} r="4"/></g>;
            })}
          </svg>
          {displaySubjects.map(subject => (
            <SubjectNode
              key={subject.id}
              subject={subject}
              noteCount={notesBySubject.get(subject.id)?.length ?? 0}
              isSelected={connectionStart === subject.id}
              connectMode={connectMode}
              zoom={zoom}
              onOpen={id => {
                const subject = subjectsById.get(id);
                updateCanvasContext({
                  openSubjectId: id,
                  drawer: {
                    subjectId: id,
                    editingId: null,
                    title: '',
                    excerpt: '',
                    metrics: [],
                    editingSubject: false,
                    subjectName: subject?.name ?? '',
                    subjectDescription: subject?.description ?? '',
                    parentSubjectId: subject?.parentSubjectId ?? '',
                  },
                });
              }}
              onSelect={selectForConnection}
              onMove={moveSubject}
              onMoveEnd={saveMovedSubject}
            />
          ))}
        </div>
        <div className="graph-legend"><span><i className="manual-link"/>{connections.length} related links</span><span><i className="hierarchy-link"/>{hierarchyEdges.length} hierarchy links</span><span>{connectMode ? 'Choose two subjects' : 'Drag to arrange'}</span></div>
        <div className="canvas-controls">
          <button onClick={onCreateSubject}><Plus size={16}/> Add node</button>
          <button className={connectionsOpen ? 'active' : ''} onClick={() => updateCanvasContext({ connectionsOpen: !connectionsOpen })}><GitBranch size={16}/> Links ({connections.length})</button>
          <button className={connectMode ? 'active' : ''} onClick={toggleConnectMode}><GitBranch size={16}/>{connectMode ? 'Done' : 'Connect'}</button>
          <button onClick={resetCanvas}><RotateCcw size={16}/> Gather subjects</button>
          <span>{Math.round(zoom * 100)}%</span>
        </div>
        {connectionsOpen ? (
          <aside className="connection-panel" onPointerDown={event => event.stopPropagation()}>
            <div className="connection-panel-head"><div><span>CONNECTIONS</span><strong>{connections.length} links</strong></div><IconButton label="Close connections" onClick={() => updateCanvasContext({ connectionsOpen: false })}><X size={18}/></IconButton></div>
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
              subjects={subjects}
              topics={topics}
              notes={notesBySubject.get(openSubject.id) ?? []}
              goals={goalsBySubject.get(openSubject.id) ?? []}
            metricDefinitions={metricDefinitions}
            drawer={drawer}
            onDrawerChange={updateDrawer}
            onClose={() => updateCanvasContext({ openSubjectId: null })}
            onAddNote={onAddNote}
            onUpdateNote={onUpdateNote}
            onRemoveNote={onRemoveNote}
            onCreateMetricDefinition={onCreateMetricDefinition}
            onCreateTopic={onCreateTopic}
            onUpdateSubject={onUpdateSubject}
              onRemoveSubject={removeOpenSubject}
              onCreateGoal={onCreateGoal}
              onRemoveGoal={onRemoveGoal}
              onCompleteGoal={onCompleteGoal}
              onSetSubGoalCompletion={onSetSubGoalCompletion}
          />
        ) : null}
      </div>
    </div>
  );
}
