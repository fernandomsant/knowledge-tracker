import { useCallback, useEffect, useMemo, useReducer } from 'react';
import { PALETTE } from '../data/seed';
import { knowledgeClient } from '../knowledge/api/knowledgeClient';

function knowledgeReducer(state, action) {
  const orderGoals = goals => goals
    .toSorted((left, right) => (left.priorityOrder ?? left.priorityPosition ?? Number.MAX_SAFE_INTEGER) - (right.priorityOrder ?? right.priorityPosition ?? Number.MAX_SAFE_INTEGER) || new Date(left.createdAtUtc) - new Date(right.createdAtUtc))
    .map((goal, priorityOrder) => ({ ...goal, priorityOrder }));
  const applyMetricDelta = (goals, metrics, direction) => goals.map(goal => {
    if (goal.kind !== 1) return goal;
    const value = metrics.find(metric => metric.definition.id === goal.metricDefinition.id)?.value ?? 0;
    return value ? { ...goal, currentValue: Math.max(0, goal.currentValue + direction * value) } : goal;
  });
  switch (action.type) {
    case 'knowledge/loading': return { ...state, status: 'loading', error: null };
    case 'knowledge/loaded': return { ...state, ...action.knowledge, goals: orderGoals(action.knowledge.goals), status: 'ready', error: null };
    case 'knowledge/failed': return { ...state, status: 'error', error: action.error };
    case 'request/failed': return { ...state, error: action.error };
    case 'request/clear': return { ...state, error: null };
    case 'subject/add': return { ...state, subjects: [...state.subjects, action.subject] };
    case 'subject/update': return { ...state, subjects: state.subjects.map(subject => subject.id === action.subject.id ? { ...subject, ...action.subject } : subject) };
    case 'subject/remove': return {
      ...state,
      subjects: state.subjects.filter(subject => subject.id !== action.id),
      notes: state.notes.filter(note => note.subjectId !== action.id),
      connections: state.connections.filter(connection => connection.source !== action.id && connection.target !== action.id),
    };
    case 'note/add': return { ...state, notes: [...state.notes, action.note], goals: applyMetricDelta(state.goals, action.note.metrics, 1) };
    case 'note/update': {
      const previous = state.notes.find(note => note.id === action.note.id);
      const goals = applyMetricDelta(applyMetricDelta(state.goals, previous?.metrics ?? [], -1), action.note.metrics, 1);
      return { ...state, notes: state.notes.map(note => note.id === action.note.id ? { ...note, ...action.note } : note), goals };
    }
    case 'connection/add': return { ...state, connections: [...state.connections, action.connection] };
    case 'connection/remove': return { ...state, connections: state.connections.filter(connection => connection.id !== action.id) };
    case 'goal/add': return { ...state, goals: orderGoals([...state.goals, action.goal]) };
    case 'goal/remove': return { ...state, goals: state.goals.filter(goal => goal.id !== action.id) };
    case 'goal/complete': return { ...state, goals: state.goals.map(goal => goal.id === action.id ? { ...goal, isCompleted: true, completedAtUtc: action.completedAtUtc } : goal) };
    case 'goal/prioritize': {
      const ranked = orderGoals(state.goals);
      const index = ranked.findIndex(goal => goal.id === action.id);
      const destination = ranked.findIndex(goal => goal.id === action.swapWithId);
      if (index < 0 || destination < 0) return state;
      [ranked[index], ranked[destination]] = [ranked[destination], ranked[index]];
      return { ...state, goals: ranked.map((goal, priorityOrder) => ({ ...goal, priorityOrder })) };
    }
    case 'sub-goal/complete': return { ...state, goals: state.goals.map(goal => ({ ...goal, subGoals: goal.subGoals?.map(subGoal => subGoal.id === action.id ? { ...subGoal, isCompleted: action.isCompleted, completedAtUtc: action.isCompleted ? action.completedAtUtc : null } : subGoal) ?? [] })) };
    default: return state;
  }
}

const initialState = { subjects: [], notes: [], connections: [], goals: [], metricDefinitions: [], status: 'loading', error: null };
const noteDateFormatter = new Intl.DateTimeFormat('en', { month: 'short', day: 'numeric' });
const errorMessage = reason => reason instanceof Error ? reason.message : 'Your knowledge space could not be updated. Try again.';

function toSubject(subject, index) {
  return { id: subject.id, name: subject.name, description: subject.description, parentSubjectId: subject.parentSubjectId, color: PALETTE[index % PALETTE.length] };
}

function toNote(note) {
  return { id: note.id, subjectId: note.subjectId, title: note.title, excerpt: note.content, metrics: note.metrics ?? [], studyDuration: note.studyDuration, studyStartedAtUtc: note.studyStartedAtUtc, date: noteDateFormatter.format(new Date(note.studyStartedAtUtc)) };
}

const toConnection = connection => ({ id: connection.id, source: connection.subjectId, target: connection.connectedSubjectId });

function toKnowledgeState(knowledge) {
  return {
    subjects: knowledge.subjects.map(toSubject),
    metricDefinitions: knowledge.metricDefinitions,
    notes: knowledge.subjects.flatMap(subject => subject.studyNotes.map(toNote)),
    goals: knowledge.goals,
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
  const goalsBySubject = useMemo(() => {
    const index = new Map(state.subjects.map(subject => [subject.id, []]));
    state.goals.forEach(goal => index.get(goal.subjectId)?.push(goal));
    return index;
  }, [state.goals, state.subjects]);

  const addSubject = useCallback(async (name, parentSubjectId) => {
    try {
      const subject = await knowledgeClient.createSubject(accessToken, name, parentSubjectId);
      dispatch({ type: 'subject/add', subject: toSubject(subject, state.subjects.length) });
      dispatch({ type: 'request/clear' });
      return subject;
    } catch (reason) {
      dispatch({ type: 'request/failed', error: errorMessage(reason) });
      return null;
    }
  }, [accessToken, state.subjects.length]);

  const updateSubject = useCallback(async (id, name, description, parentSubjectId) => {
    try {
      const subject = await knowledgeClient.updateSubject(accessToken, id, name, description, parentSubjectId);
      dispatch({ type: 'subject/update', subject: { id, name: subject.name, description: subject.description, parentSubjectId: subject.parentSubjectId } });
      dispatch({ type: 'request/clear' });
      return subject;
    } catch (reason) {
      dispatch({ type: 'request/failed', error: errorMessage(reason) });
      return null;
    }
  }, [accessToken]);

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


  const addNote = useCallback(async (subjectId, title, excerpt, studyDuration, studyStartedAtUtc, metrics) => {
    try {
      const note = await knowledgeClient.createStudyNote(accessToken, subjectId, title, excerpt, studyDuration, studyStartedAtUtc, metrics);
      dispatch({ type: 'note/add', note: toNote(note) });
      dispatch({ type: 'request/clear' });
      return note;
    } catch (reason) {
      dispatch({ type: 'request/failed', error: errorMessage(reason) });
      return null;
    }
  }, [accessToken]);

  const updateNote = useCallback(async (id, title, excerpt, studyDuration, studyStartedAtUtc, metrics) => {
    try {
      const note = await knowledgeClient.updateStudyNote(accessToken, id, title, excerpt, studyDuration, studyStartedAtUtc, metrics);
      dispatch({ type: 'note/update', note: toNote(note) });
      dispatch({ type: 'request/clear' });
      return note;
    } catch (reason) {
      dispatch({ type: 'request/failed', error: errorMessage(reason) });
      return null;
    }
  }, [accessToken]);

  const createMetricDefinition = useCallback(async (name, numberKind) => {
    try {
      const definition = await knowledgeClient.createMetricDefinition(accessToken, name, numberKind);
      dispatch({ type: 'knowledge/loaded', knowledge: { metricDefinitions: [...state.metricDefinitions, definition] } });
      dispatch({ type: 'request/clear' });
      return definition;
    } catch (reason) {
      dispatch({ type: 'request/failed', error: errorMessage(reason) });
      return null;
    }
  }, [accessToken, state.metricDefinitions]);

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

  const addSubjectGoal = useCallback(async (subjectId, goal) => {
    try {
      const created = await knowledgeClient.createSubjectGoal(accessToken, subjectId, goal);
      dispatch({ type: 'goal/add', goal: created });
      dispatch({ type: 'request/clear' });
      return created;
    } catch (reason) {
      dispatch({ type: 'request/failed', error: errorMessage(reason) });
      return null;
    }
  }, [accessToken]);

  const removeSubjectGoal = useCallback(async id => {
    try {
      await knowledgeClient.deleteSubjectGoal(accessToken, id);
      dispatch({ type: 'goal/remove', id });
      dispatch({ type: 'request/clear' });
      return true;
    } catch (reason) {
      dispatch({ type: 'request/failed', error: errorMessage(reason) });
      return false;
    }
  }, [accessToken]);

  const completeSubjectGoal = useCallback(async id => {
    try {
      await knowledgeClient.completeSubjectGoal(accessToken, id);
      dispatch({ type: 'goal/complete', id, completedAtUtc: new Date().toISOString() });
      dispatch({ type: 'request/clear' });
      return true;
    } catch (reason) {
      dispatch({ type: 'request/failed', error: errorMessage(reason) });
      return false;
    }
  }, [accessToken]);
  const prioritizeSubjectGoal = useCallback(async (id, swapWithId) => {
    try {
      await knowledgeClient.swapSubjectGoalPriority(accessToken, id, swapWithId);
      dispatch({ type: 'goal/prioritize', id, swapWithId });
      return true;
    } catch (reason) {
      dispatch({ type: 'request/failed', error: errorMessage(reason) });
      return false;
    }
  }, [accessToken]);
  const setSubGoalCompletion = useCallback(async (id, isCompleted) => {
    try {
      await knowledgeClient.setSubGoalCompletion(accessToken, id, isCompleted);
      dispatch({ type: 'sub-goal/complete', id, isCompleted, completedAtUtc: new Date().toISOString() });
      dispatch({ type: 'request/clear' });
      return true;
    } catch (reason) {
      dispatch({ type: 'request/failed', error: errorMessage(reason) });
      return false;
    }
  }, [accessToken]);

  return { ...state, subjectsById, notesBySubject, goalsBySubject, addSubject, updateSubject, removeSubject, addNote, updateNote, createMetricDefinition, connectSubjects, removeConnection, addSubjectGoal, removeSubjectGoal, completeSubjectGoal, prioritizeSubjectGoal, setSubGoalCompletion };
}
