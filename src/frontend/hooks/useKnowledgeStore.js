import { useCallback, useEffect, useMemo, useReducer } from 'react';
import { PALETTE } from '../data/seed';
import { knowledgeClient } from '../knowledge/api/knowledgeClient';

function knowledgeReducer(state, action) {
  switch (action.type) {
    case 'knowledge/loading': return { ...state, status: 'loading', error: null };
    case 'knowledge/loaded': return { ...state, ...action.knowledge, status: 'ready', error: null };
    case 'knowledge/failed': return { ...state, status: 'error', error: action.error };
    case 'request/failed': return { ...state, error: action.error };
    case 'request/clear': return { ...state, error: null };
    case 'subject/add': return { ...state, subjects: [...state.subjects, action.subject] };
    case 'subject/remove': return {
      ...state,
      subjects: state.subjects.filter(subject => subject.id !== action.id),
      notes: state.notes.filter(note => note.subjectId !== action.id),
      connections: state.connections.filter(connection => connection.source !== action.id && connection.target !== action.id),
    };
    case 'subject/move': return { ...state, subjects: state.subjects.map(subject => subject.id === action.id ? { ...subject, x: action.x, y: action.y } : subject) };
    case 'note/add': return { ...state, notes: [...state.notes, action.note] };
    case 'note/update': return { ...state, notes: state.notes.map(note => note.id === action.note.id ? { ...note, ...action.note } : note) };
    case 'connection/add': return { ...state, connections: [...state.connections, action.connection] };
    case 'connection/remove': return { ...state, connections: state.connections.filter(connection => connection.id !== action.id) };
    default: return state;
  }
}

const initialState = { subjects: [], notes: [], connections: [], status: 'loading', error: null };
const noteDateFormatter = new Intl.DateTimeFormat('en', { month: 'short', day: 'numeric' });
const errorMessage = reason => reason instanceof Error ? reason.message : 'Your knowledge space could not be updated. Try again.';

function toSubject(subject, index) {
  return { id: subject.id, name: subject.name, description: subject.description, color: PALETTE[index % PALETTE.length], x: 120 + (index % 3) * 260, y: 110 + Math.floor(index / 3) * 210 };
}

function toNote(note) {
  return { id: note.id, subjectId: note.subjectId, title: note.title, excerpt: note.content, status: 'Draft', date: noteDateFormatter.format(new Date(note.studyStartedAtUtc)) };
}

const toConnection = connection => ({ id: connection.id, source: connection.subjectId, target: connection.connectedSubjectId });

function toKnowledgeState(knowledge) {
  return {
    subjects: knowledge.subjects.map(toSubject),
    notes: knowledge.subjects.flatMap(subject => subject.studyNotes.map(toNote)),
    connections: knowledge.connections.map(toConnection),
  };
}

export function useKnowledgeStore(accessToken) {
  const [state, dispatch] = useReducer(knowledgeReducer, initialState);

  useEffect(() => {
    let current = true;
    dispatch({ type: 'knowledge/loading' });
    void knowledgeClient.load(accessToken)
      .then(knowledge => { if (current) dispatch({ type: 'knowledge/loaded', knowledge: toKnowledgeState(knowledge) }); })
      .catch(reason => { if (current) dispatch({ type: 'knowledge/failed', error: errorMessage(reason) }); });
    return () => { current = false; };
  }, [accessToken]);

  const subjectsById = useMemo(() => new Map(state.subjects.map(subject => [subject.id, subject])), [state.subjects]);
  const notesBySubject = useMemo(() => {
    const index = new Map(state.subjects.map(subject => [subject.id, []]));
    state.notes.forEach(note => index.get(note.subjectId)?.push(note));
    return index;
  }, [state.notes, state.subjects]);

  const addSubject = useCallback(async name => {
    try {
      const subject = await knowledgeClient.createSubject(accessToken, name);
      dispatch({ type: 'subject/add', subject: toSubject(subject, state.subjects.length) });
      dispatch({ type: 'request/clear' });
      return subject;
    } catch (reason) {
      dispatch({ type: 'request/failed', error: errorMessage(reason) });
      return null;
    }
  }, [accessToken, state.subjects.length]);

  const removeSubject = useCallback(async id => {
    try {
      await knowledgeClient.deleteSubject(accessToken, id);
      dispatch({ type: 'subject/remove', id });
      dispatch({ type: 'request/clear' });
      return true;
    } catch (reason) {
      dispatch({ type: 'request/failed', error: errorMessage(reason) });
      return false;
    }
  }, [accessToken]);

  const moveSubject = useCallback((id, x, y) => dispatch({ type: 'subject/move', id, x, y }), []);

  const addNote = useCallback(async (subjectId, title, excerpt) => {
    try {
      const note = await knowledgeClient.createStudyNote(accessToken, subjectId, title, excerpt);
      dispatch({ type: 'note/add', note: toNote(note) });
      dispatch({ type: 'request/clear' });
      return note;
    } catch (reason) {
      dispatch({ type: 'request/failed', error: errorMessage(reason) });
      return null;
    }
  }, [accessToken]);

  const updateNote = useCallback(async (id, title, excerpt) => {
    try {
      const note = await knowledgeClient.updateStudyNote(accessToken, id, title, excerpt);
      dispatch({ type: 'note/update', note: toNote(note) });
      dispatch({ type: 'request/clear' });
      return note;
    } catch (reason) {
      dispatch({ type: 'request/failed', error: errorMessage(reason) });
      return null;
    }
  }, [accessToken]);

  const connectSubjects = useCallback(async (source, target) => {
    try {
      const connection = await knowledgeClient.createConnection(accessToken, source, target);
      dispatch({ type: 'connection/add', connection: toConnection(connection) });
      dispatch({ type: 'request/clear' });
      return connection;
    } catch (reason) {
      dispatch({ type: 'request/failed', error: errorMessage(reason) });
      return null;
    }
  }, [accessToken]);

  const removeConnection = useCallback(async id => {
    try {
      await knowledgeClient.deleteConnection(accessToken, id);
      dispatch({ type: 'connection/remove', id });
      dispatch({ type: 'request/clear' });
      return true;
    } catch (reason) {
      dispatch({ type: 'request/failed', error: errorMessage(reason) });
      return false;
    }
  }, [accessToken]);

  return { ...state, subjectsById, notesBySubject, addSubject, removeSubject, moveSubject, addNote, updateNote, connectSubjects, removeConnection };
}
