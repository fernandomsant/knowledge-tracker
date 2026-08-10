import { memo, useCallback, useEffect, useMemo, useRef, useState } from 'react';
import {
  ArrowRight, Bell, Brain, Check, ChevronDown, Clock3, FileText, Folder,
  GitBranch, Hash, HelpCircle, LayoutDashboard, Library, List, Menu,
  Maximize2, MoreHorizontal, Network, Plus, Search, Settings, Share2, Sparkles, Tag, X, Zap,
} from './icons';
import { NOTE_STATUSES } from './data/seed';
import { useAuthenticationSession } from './authentication/context/AuthenticationContext';
import { useKnowledgeStore } from './hooks/useKnowledgeStore';
import { IconButton } from './components/IconButton';
import { KnowledgeGraph } from './components/KnowledgeGraph';
import { getSubjectParentOptions } from './knowledge/utils/subjectHierarchy';

const NAV_ITEMS = [
  { label: 'Overview', Icon: LayoutDashboard },
  { label: 'My notes', Icon: FileText },
  { label: 'Collections', Icon: Library },
  { label: 'Graph view', Icon: Network },
];

const initialCanvasContext = {
  pan: { x: 0, y: 0 },
  zoom: 1,
  connectMode: false,
  connectionStart: null,
  openSubjectId: null,
  connectionsOpen: false,
  drawer: {
    subjectId: null,
    editingId: null,
    title: '',
    excerpt: '',
    metrics: [],
    editingSubject: false,
    subjectName: '',
    subjectDescription: '',
    parentSubjectId: '',
  },
};

const Sidebar = memo(function Sidebar({
  subjects, notesBySubject, noteCount, activeNav, activeSubject,
  onNavigate, onSelectSubject, onCreateSubject, open, onClose,
}) {
  return (
    <>
      {open ? <button className="sidebar-scrim" aria-label="Close menu" onClick={onClose}/> : null}
      <aside className={`sidebar ${open ? 'is-open' : ''}`}>
        <div className="brand"><span className="brand-mark"><Brain size={20}/></span><strong>knowly</strong><span className="beta">BETA</span></div>
        <button className="workspace-switcher">
          <span className="avatar">AM</span><span><strong>Alex Morgan</strong><small>Personal workspace</small></span><ChevronDown size={16}/>
        </button>
        <nav className="primary-nav" aria-label="Primary navigation">
          {NAV_ITEMS.map(({ label, Icon }) => (
            <button key={label} className={activeNav === label ? 'active' : ''} onClick={() => onNavigate(label)}>
              <Icon size={18}/><span>{label}</span>{label === 'My notes' ? <em>{noteCount}</em> : null}
            </button>
          ))}
        </nav>
        <section className="subjects-nav">
          <div className="sidebar-label"><span>Subjects</span><IconButton label="Create subject" onClick={onCreateSubject}><Plus size={15}/></IconButton></div>
          <button className={activeSubject === 'all' ? 'active' : ''} onClick={() => onSelectSubject('all')}>
            <span className="subject-all"><Folder size={15}/> All subjects</span><small>{noteCount}</small>
          </button>
          {subjects.map(subject => (
            <button key={subject.id} className={activeSubject === subject.id ? 'active' : ''} onClick={() => onSelectSubject(subject.id)}>
              <span><i className={`color-dot ${subject.color}`}/>{subject.name}</span><small>{notesBySubject.get(subject.id)?.length ?? 0}</small>
            </button>
          ))}
        </section>
        <div className="sidebar-bottom">
          <button><Settings size={17}/>Settings</button><button><HelpCircle size={17}/>Help center</button>
          <div className="upgrade"><span><Zap size={16}/></span><strong>Unlock more space</strong><p>Unlimited notes, exports and advanced connections.</p><button>Upgrade plan <ArrowRight size={14}/></button></div>
        </div>
      </aside>
    </>
  );
});

const Topbar = memo(function Topbar({ activeNav, query, onQueryChange, onOpenMenu }) {
  return (
    <header className="topbar">
      <div className="crumbs"><IconButton label="Open menu" className="menu-button" onClick={onOpenMenu}><Menu size={20}/></IconButton><span>Workspace</span><b>/</b><strong>{activeNav}</strong></div>
      <div className="top-actions">
        <label className="search"><Search size={17}/><input value={query} onChange={event => onQueryChange(event.target.value)} placeholder="Search notes..."/><kbd>âŒ˜ K</kbd></label>
        <IconButton label="Notifications" className="notification"><Bell size={18}/><i/></IconButton><span className="avatar">AM</span>
      </div>
    </header>
  );
});

const StatCard = memo(function StatCard({ Icon, color, label, value, detail }) {
  return <article className="stat-card"><span className={`stat-icon ${color}`}><Icon size={20}/></span><div><small>{label}</small><strong>{value}</strong><p>{detail}</p></div></article>;
});

const NotesList = memo(function NotesList({ notes, subjectsById }) {
  return (
    <div className="notes-list">
      {notes.map(note => {
        const subject = subjectsById.get(note.subjectId) ?? { name: 'Unsorted', color: 'purple' };
        const statusClass = note.status.toLowerCase().replace(' ', '-');
        return (
          <article className="note-row" key={note.id}>
            <span className={`file-box ${subject.color}`}><FileText size={19}/></span>
            <div className="note-copy"><strong>{note.title}</strong><p>{note.excerpt}</p></div>
            <span className={`status ${statusClass}`}><i/>{note.status}</span><time>{note.date}</time>
            <span className={`subject-tag ${subject.color}`}>{subject.name}</span>
            <IconButton label={`Options for ${note.title}`}><MoreHorizontal size={18}/></IconButton>
          </article>
        );
      })}
    </div>
  );
});

function SubjectModal({ open, name, parentSubjectId, parentOptions, onNameChange, onParentChange, onClose, onCreate }) {
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
        <div className="modal-head"><div><span>NEW SUBJECT</span><h2>Create a subject</h2></div><IconButton type="button" label="Close modal" onClick={onClose}><X size={19}/></IconButton></div>
        <p>Group related ideas into a subject. Choose a parent to build a knowledge path of up to four levels.</p>
        <label>Subject name<input ref={inputRef} value={name} onChange={event => onNameChange(event.target.value)} placeholder="e.g. Behavioral economics"/></label>
        <label>Parent subject<select value={parentSubjectId} onChange={event => onParentChange(event.target.value)}><option value="">Top-level subject</option>{parentOptions.map(subject => <option key={subject.id} value={subject.id}>{subject.label}</option>)}</select><small className="hierarchy-hint">For example: Physics / Mechanics / Kinematics</small></label>
        <div className="modal-actions"><button type="button" className="ghost-button" onClick={onClose}>Cancel</button><button className="primary-button" disabled={!name.trim()}>Create subject</button></div>
      </form>
    </div>
  );
}

const CanvasOverlay = memo(function CanvasOverlay({ open, onClose, graphProps }) {
  useEffect(() => {
    if (!open) return undefined;
    const closeOnEscape = event => {
      if (event.key === 'Escape') onClose();
    };
    document.body.classList.add('canvas-expanded');
    window.addEventListener('keydown', closeOnEscape);
    return () => {
      document.body.classList.remove('canvas-expanded');
      window.removeEventListener('keydown', closeOnEscape);
    };
  }, [onClose, open]);

  if (!open) return null;

  return (
    <div className="canvas-overlay" onMouseDown={event => { if (event.target === event.currentTarget) onClose(); }}>
      <section className="expanded-canvas-panel" aria-modal="true" role="dialog" aria-label="Expanded knowledge canvas">
        <header className="expanded-canvas-head">
          <div><span>KNOWLEDGE CANVAS</span><h2>Your knowledge space</h2></div>
          <IconButton label="Minimize canvas" onClick={onClose}><X size={20}/></IconButton>
        </header>
        <KnowledgeGraph {...graphProps}/>
      </section>
    </div>
  );
});
export default function App() {
  const { accessToken } = useAuthenticationSession();
  const {
    subjects, notes, connections, metricDefinitions, subjectsById, notesBySubject, status: knowledgeStatus, error: knowledgeError,
    addSubject, updateSubject, removeSubject, moveSubject, addNote, updateNote, createMetricDefinition, connectSubjects, removeConnection,
  } = useKnowledgeStore(accessToken);
  const [activeNav, setActiveNav] = useState('Overview');
  const [activeSubject, setActiveSubject] = useState('all');
  const [activeFilter, setActiveFilter] = useState('All notes');
  const [view, setView] = useState('canvas');
  const [query, setQuery] = useState('');
  const [copied, setCopied] = useState(false);
  const [modalOpen, setModalOpen] = useState(false);
  const [newSubjectName, setNewSubjectName] = useState('');
  const [newSubjectParentId, setNewSubjectParentId] = useState('');
  const [menuOpen, setMenuOpen] = useState(false);
  const [canvasExpanded, setCanvasExpanded] = useState(false);
  const [canvasContext, setCanvasContext] = useState(initialCanvasContext);
  const copiedTimerRef = useRef(null);

  useEffect(() => {
    const focusSearch = event => {
      if ((event.metaKey || event.ctrlKey) && event.key.toLowerCase() === 'k') {
        event.preventDefault();
        document.querySelector('.search input')?.focus();
      }
    };
    window.addEventListener('keydown', focusSearch);
    return () => window.removeEventListener('keydown', focusSearch);
  }, []);

  useEffect(() => () => clearTimeout(copiedTimerRef.current), []);

  const filteredNotes = useMemo(() => {
    const normalizedQuery = query.trim().toLowerCase();
    return notes.filter(note => {
      if (activeSubject !== 'all' && note.subjectId !== activeSubject) return false;
      if (activeFilter !== 'All notes' && note.status !== activeFilter) return false;
      return !normalizedQuery || `${note.title} ${note.excerpt}`.toLowerCase().includes(normalizedQuery);
    });
  }, [notes, activeSubject, activeFilter, query]);

  const recentNotes = useMemo(() => notes.slice(-3).reverse(), [notes]);
  const parentOptions = useMemo(() => getSubjectParentOptions(subjects), [subjects]);
  const selectedSubject = activeSubject === 'all' ? null : subjectsById.get(activeSubject);

  const closeModal = useCallback(() => {
    setModalOpen(false);
    setNewSubjectName('');
    setNewSubjectParentId('');
  }, []);

  const openModal = useCallback(() => setModalOpen(true), []);
  const openMenu = useCallback(() => setMenuOpen(true), []);
  const closeMenu = useCallback(() => setMenuOpen(false), []);
  const expandCanvas = useCallback(() => setCanvasExpanded(true), []);
  const minimizeCanvas = useCallback(() => setCanvasExpanded(false), []);

  const handleNavigate = useCallback(label => {
    setActiveNav(label);
    setMenuOpen(false);
    if (label === 'Graph view') setView('canvas');
    else if (label === 'My notes') setView('list');
  }, []);

  const handleShare = useCallback(() => {
    clearTimeout(copiedTimerRef.current);
    setCopied(true);
    copiedTimerRef.current = setTimeout(() => setCopied(false), 1800);
  }, []);

  const handleCreateSubject = useCallback(async event => {
    event.preventDefault();
    const name = newSubjectName.trim();
    if (!name) return;
    if (!await addSubject(name, newSubjectParentId || null)) return;
    setActiveSubject('all');
    closeModal();
  }, [addSubject, closeModal, newSubjectName, newSubjectParentId]);

  return (
    <div className="app-shell">
      <Sidebar
        subjects={subjects}
        notesBySubject={notesBySubject}
        noteCount={notes.length}
        activeNav={activeNav}
        activeSubject={activeSubject}
        onNavigate={handleNavigate}
        onSelectSubject={setActiveSubject}
        onCreateSubject={openModal}
        open={menuOpen}
        onClose={closeMenu}
      />
      <div className="page-wrap">
        <Topbar activeNav={activeNav} query={query} onQueryChange={setQuery} onOpenMenu={openMenu}/>
        <main>
          <section className="page-intro">
            <div><span className="eyebrow"><Sparkles size={14}/> YOUR KNOWLEDGE SPACE</span><h1>Good morning, Alex.</h1><p>{selectedSubject ? `Exploring ${selectedSubject.name}.` : 'Gather your ideas, find the patterns, and keep learning.'}</p></div>
            <button className={`share-button ${copied ? 'success' : ''}`} onClick={handleShare}>{copied ? <Check size={17}/> : <Share2 size={17}/>} {copied ? 'Link copied' : 'Share space'}</button>
          </section>
          <section className="stats-grid">
            <StatCard Icon={FileText} color="teal" label="Total notes" value={notes.length} detail="Saved to your space"/>
            <StatCard Icon={GitBranch} color="blue" label="Connections" value={connections.length} detail="Saved to your space"/>
            <StatCard Icon={Clock3} color="amber" label="Study streak" value="7 days" detail="Best: 14 days"/>
            <StatCard Icon={Hash} color="purple" label="Topics covered" value={subjects.length} detail="In your knowledge space"/>
          </section>
          <section className="workspace-panel">
            <header className="panel-head">
              <div><span>SUBJECT MAP</span><h2>Your knowledge space</h2><p>Arrange subjects, connect related thinking, then open a node to work with its notes.</p></div>
              <div className="panel-actions">
                <button className="primary-button" onClick={openModal}><Plus size={16}/> New node</button>
                <div className="view-toggle"><button className={view === 'canvas' ? 'active' : ''} onClick={() => setView('canvas')}><Network size={15}/>Canvas</button><button className={view === 'list' ? 'active' : ''} onClick={() => setView('list')}><List size={15}/>Notes list</button></div>
                <button className="expand-canvas-button" onClick={expandCanvas} disabled={view !== 'canvas'}><Maximize2 size={16}/> Expand canvas</button>
                <IconButton label="Workspace options"><MoreHorizontal size={19}/></IconButton>
              </div>
            </header>
            <div className="filter-row">
              <div>{['All notes', ...NOTE_STATUSES].map(filter => <button key={filter} className={activeFilter === filter ? 'active' : ''} onClick={() => setActiveFilter(filter)}>{filter}{filter === 'All notes' ? <small>{notes.length}</small> : null}</button>)}</div>
              <button className="filter-button"><Tag size={15}/>Filter<ChevronDown size={14}/></button>
            </div>
            {knowledgeStatus === 'loading' ? <p role="status">Loading your knowledge space…</p> : null}
            {knowledgeError ? <p role="alert">{knowledgeError}</p> : null}
            {view === 'canvas' ? (
              <KnowledgeGraph
                subjects={subjects}
                subjectsById={subjectsById}
                notesBySubject={notesBySubject}
                connections={connections}
                canvasContext={canvasContext}
                onCanvasContextChange={setCanvasContext}
                onMoveSubject={moveSubject}
                onConnect={connectSubjects}
                onAddNote={addNote}
                onUpdateNote={updateNote}
                metricDefinitions={metricDefinitions}
                onCreateMetricDefinition={createMetricDefinition}
                onUpdateSubject={updateSubject}
                onCreateSubject={openModal}
                onRemoveSubject={removeSubject}
                onRemoveConnection={removeConnection}
              />
            ) : <NotesList notes={filteredNotes} subjectsById={subjectsById}/>}
          </section>
          <section className="bottom-grid">
            <article className="activity-card">
              <div className="card-title"><div><span>RECENT ACTIVITY</span><h3>Keep the thread going</h3></div><button onClick={() => setView('list')}>View all <ArrowRight size={14}/></button></div>
              {recentNotes.map(note => { const subject = subjectsById.get(note.subjectId); return <div className="activity-row" key={note.id}><span className={`file-box ${subject?.color ?? 'purple'}`}><FileText size={17}/></span><div><strong>{note.title}</strong><small>{subject?.name} Â· {note.date}</small></div><ArrowRight size={16}/></div>; })}
            </article>
            <article className="focus-card"><span className="nudge"><Sparkles size={13}/> A LITTLE NUDGE</span><h3>Connect the dots.</h3><p>A few of your newest notes are still floating alone. Link them to a subject or connect related themes.</p><button>Explore suggestions <ArrowRight size={15}/></button></article>
          </section>
        </main>
      </div>
      <CanvasOverlay
        open={canvasExpanded}
        onClose={minimizeCanvas}
        graphProps={{
          subjects,
          subjectsById,
          notesBySubject,
          connections,
          canvasContext,
          onCanvasContextChange: setCanvasContext,
          onMoveSubject: moveSubject,
          onConnect: connectSubjects,
          onAddNote: addNote,
          onUpdateNote: updateNote,
          metricDefinitions,
          onCreateMetricDefinition: createMetricDefinition,
          onUpdateSubject: updateSubject,
          onCreateSubject: openModal,
          onRemoveSubject: removeSubject,
          onRemoveConnection: removeConnection,
        }}
      />      <SubjectModal open={modalOpen} name={newSubjectName} parentSubjectId={newSubjectParentId} parentOptions={parentOptions} onNameChange={setNewSubjectName} onParentChange={setNewSubjectParentId} onClose={closeModal} onCreate={handleCreateSubject}/>
    </div>
  );
}
