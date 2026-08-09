import { useCallback, useMemo, useReducer } from 'react';
import { initialConnections, initialNotes, initialSubjects, PALETTE } from '../data/seed';

function knowledgeReducer(state, action) {
  switch (action.type) {
    case 'subject/add':
      return { ...state, subjects: [...state.subjects, action.subject] };
    case 'subject/remove':
      return {
        ...state,
        subjects: state.subjects.filter(subject => subject.id !== action.id),
        notes: state.notes.filter(note => note.subjectId !== action.id),
        connections: state.connections.filter(connection =>
          connection.source !== action.id && connection.target !== action.id
        ),
      };
    case 'subject/move':
      return {
        ...state,
        subjects: state.subjects.map(subject =>
          subject.id === action.id ? { ...subject, x: action.x, y: action.y } : subject
        ),
      };
    case 'note/add':
      return { ...state, notes: [...state.notes, action.note] };
    case 'note/update':
      return {
        ...state,
        notes: state.notes.map(note => note.id === action.note.id ? { ...note, ...action.note } : note),
      };
    case 'connection/add':
      return { ...state, connections: [...state.connections, action.connection] };
    case 'connection/remove':
      return {
        ...state,
        connections: state.connections.filter(connection => connection.id !== action.id),
      };
    default:
      return state;
  }
}

const initialState = {
  subjects: initialSubjects,
  notes: initialNotes,
  connections: initialConnections,
};

export function useKnowledgeStore() {
  const [state, dispatch] = useReducer(knowledgeReducer, initialState);

  const subjectsById = useMemo(
    () => new Map(state.subjects.map(subject => [subject.id, subject])),
    [state.subjects]
  );

  const notesBySubject = useMemo(() => {
    const index = new Map(state.subjects.map(subject => [subject.id, []]));
    state.notes.forEach(note => index.get(note.subjectId)?.push(note));
    return index;
  }, [state.notes, state.subjects]);

  const addSubject = useCallback(name => {
    const index = state.subjects.length;
    const id = `${name.toLowerCase().replace(/[^a-z0-9]+/g, '-')}-${Date.now()}`;
    dispatch({
      type: 'subject/add',
      subject: {
        id,
        name,
        color: PALETTE[index % PALETTE.length],
        x: 120 + (index % 3) * 260,
        y: 110 + Math.floor(index / 3) * 210,
      },
    });
  }, [state.subjects.length]);

  const removeSubject = useCallback(id => dispatch({ type: 'subject/remove', id }), []);
  const moveSubject = useCallback((id, x, y) => dispatch({ type: 'subject/move', id, x, y }), []);

  const addNote = useCallback((subjectId, title, excerpt) => dispatch({
    type: 'note/add',
    note: { id: crypto.randomUUID(), subjectId, title, excerpt, status: 'Draft', date: 'Today' },
  }), []);

  const updateNote = useCallback((id, title, excerpt) => dispatch({
    type: 'note/update',
    note: { id, title, excerpt },
  }), []);

  const connectSubjects = useCallback((source, target) => {
    dispatch({
      type: 'connection/add',
      connection: { id: crypto.randomUUID(), source, target },
    });
  }, []);

  const removeConnection = useCallback(id => dispatch({ type: 'connection/remove', id }), []);

  return {
    ...state,
    subjectsById,
    notesBySubject,
    addSubject,
    removeSubject,
    moveSubject,
    addNote,
    updateNote,
    connectSubjects,
    removeConnection,
  };
}