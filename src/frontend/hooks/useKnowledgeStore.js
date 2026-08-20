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
    case 'goal-activity/loaded': return { ...state, goalActivity: action.activity };
    case 'knowledge/failed': return { ...state, status: 'error', error: action.error };
    case 'request/failed': return { ...state, error: action.error };
    case 'request/clear': return { ...state, error: null };
    case 'topic/add': return { ...state, topics: [...state.topics, action.topic] };
    case 'topic/remove': return { ...state, topics: state.topics.filter(topic => topic.id !== action.id) };
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
    case 'note/classification-refresh': {
      const refreshedById = new Map(action.notes.map(note => [note.id, toNote(note)]));
      return { ...state, notes: state.notes.map(note => refreshedById.get(note.id) ?? note) };
    }
    case 'note/remove': {
      const note = state.notes.find(candidate => candidate.id === action.id);
      return { ...state, notes: state.notes.filter(candidate => candidate.id !== action.id), goals: applyMetricDelta(state.goals, note?.metrics ?? [], -1) };
    }
    case 'connection/add': return { ...state, connections: [...state.connections, action.connection] };
    case 'connection/remove': return { ...state, connections: state.connections.filter(connection => connection.id !== action.id) };
    case 'goal/add': return { ...state, goals: orderGoals([...state.goals, action.goal]) };
    case 'goal/update': return { ...state, goals: orderGoals(state.goals.map(goal => goal.id === action.goal.id ? action.goal : goal)) };
    case 'goal/remove': return { ...state, goals: state.goals.filter(goal => goal.id !== action.id) };
    case 'goal/complete': return { ...state, goals: state.goals.map(goal => goal.id === action.id && (goal.period === 0 || goal.period === 4) ? { ...goal, isCompleted: true, completedAtUtc: action.completedAtUtc } : goal) };
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

const initialState = { subjects: [], notes: [], connections: [], goals: [], topics: [], metricDefinitions: [], goalActivity: [], status: 'loading', error: null };
const noteDateFormatter = new Intl.DateTimeFormat('en', { month: 'short', day: 'numeric' });
const errorMessage = reason => reason instanceof Error ? reason.message : 'Your knowledge space could not be updated. Try again.';

function toSubject(subject, index) {
  return { id: subject.id, name: subject.name, description: subject.description, parentSubjectId: subject.parentSubjectId, layoutPosition: subject.layoutPosition, color: PALETTE[index % PALETTE.length] };
}

function toNote(note) {
  return { id: note.id, subjectId: note.subjectId, topicId: note.topicId, title: note.title, excerpt: note.content, metrics: note.metrics ?? [], studyDuration: note.studyDuration, studyStartedAtUtc: note.studyStartedAtUtc, version: note.version ?? 1, classification: note.classification ?? { status: 'Pending', scores: [] }, date: noteDateFormatter.format(new Date(note.studyStartedAtUtc)) };
}

const toConnection = connection => ({ id: connection.id, source: connection.subjectId, target: connection.connectedSubjectId });

function toKnowledgeState(knowledge) {
  const notes = [...new Map(
    knowledge.subjects
      .flatMap(subject => subject.studyNotes)
      .map(note => [note.id, note])
  ).values()];
  return {
    subjects: knowledge.subjects.map(toSubject),
    metricDefinitions: knowledge.metricDefinitions,
    topics: knowledge.topics,
    notes: notes.map(toNote),
    goals: knowledge.goals,
    connections: knowledge.connections.map(toConnection),
  };
}

export function useKnowledgeStore(accessToken, refreshAccessToken) {
  const [state, dispatch] = useReducer(knowledgeReducer, initialState);

  const execute = useCallback(async operation => {
    try {
      return await operation(accessToken);
    } catch (reason) {
      if (reason?.status !== 401) throw reason;
      const refreshedSession = await refreshAccessToken();
      if (!refreshedSession) throw reason;
      return operation(refreshedSession.accessToken);
    }
  }, [accessToken, refreshAccessToken]);

  useEffect(() => {
    let current = true;
    dispatch({ type: 'knowledge/loading' });
    void execute(token => knowledgeClient.load(token))
      .then(knowledge => { if (current) dispatch({ type: 'knowledge/loaded', knowledge: toKnowledgeState(knowledge) }); })
      .catch(reason => { if (current) dispatch({ type: 'knowledge/failed', error: errorMessage(reason) }); });
    return () => { current = false; };
  }, [execute]);

  const subjectsById = useMemo(() => new Map(state.subjects.map(subject => [subject.id, subject])), [state.subjects]);
  const pendingClassificationSubjects = useMemo(() => [...new Set(
    state.notes
      .filter(note => ['Pending', 'Processing', 'RetryScheduled'].includes(note.classification?.status))
      .map(note => note.subjectId)
  )].sort().join(','), [state.notes]);

  useEffect(() => {
    if (!pendingClassificationSubjects) return undefined;
    let current = true;
    const subjectIds = pendingClassificationSubjects.split(',');
    const refresh = async () => {
      try {
        const groups = await Promise.all(subjectIds.map(subjectId =>
          execute(token => knowledgeClient.listStudyNotes(token, subjectId))
        ));
        if (current) dispatch({ type: 'note/classification-refresh', notes: groups.flat() });
      } catch (reason) {
        if (current && reason?.status === 401) dispatch({ type: 'request/failed', error: errorMessage(reason) });
      }
    };
    const interval = window.setInterval(() => { void refresh(); }, 3000);
    return () => { current = false; window.clearInterval(interval); };
  }, [execute, pendingClassificationSubjects]);
  const directNotesBySubject = useMemo(() => {
    const index = new Map(state.subjects.map(subject => [subject.id, []]));
    state.notes.forEach(note => index.get(note.subjectId)?.push(note));
    return index;
  }, [state.notes, state.subjects]);
  const notesBySubject = useMemo(() => {
    const childrenBySubject = new Map(state.subjects.map(subject => [subject.id, []]));
    state.subjects.forEach(subject => {
      if (subject.parentSubjectId) childrenBySubject.get(subject.parentSubjectId)?.push(subject.id);
    });

    const aggregate = (subjectId, visiting = new Set()) => {
      if (visiting.has(subjectId)) return [];
      const nextVisiting = new Set(visiting).add(subjectId);
      return [
        ...(directNotesBySubject.get(subjectId) ?? []),
        ...(childrenBySubject.get(subjectId) ?? []).flatMap(childId => aggregate(childId, nextVisiting)),
      ];
    };

    return new Map(state.subjects.map(subject => [subject.id, aggregate(subject.id)]));
  }, [directNotesBySubject, state.subjects]);
  const goalsBySubject = useMemo(() => {
    const index = new Map(state.subjects.map(subject => [subject.id, []]));
    state.goals.forEach(goal => index.get(goal.subjectId)?.push(goal));
    return index;
  }, [state.goals, state.subjects]);

  const addSubject = useCallback(async (name, parentSubjectId) => {
    try {
      const subject = await execute(token => knowledgeClient.createSubject(token, name, parentSubjectId));
      dispatch({ type: 'subject/add', subject: toSubject(subject, state.subjects.length) });
      dispatch({ type: 'request/clear' });
      return subject;
    } catch (reason) {
      dispatch({ type: 'request/failed', error: errorMessage(reason) });
      return null;
    }
  }, [execute, state.subjects.length]);

  const updateSubject = useCallback(async (id, name, description, parentSubjectId) => {
    try {
      const subject = await execute(token => knowledgeClient.updateSubject(token, id, name, description, parentSubjectId));
      dispatch({ type: 'subject/update', subject: { id, name: subject.name, description: subject.description, parentSubjectId: subject.parentSubjectId } });
      dispatch({ type: 'request/clear' });
      return subject;
    } catch (reason) {
      dispatch({ type: 'request/failed', error: errorMessage(reason) });
      return null;
    }
  }, [execute]);

  const removeSubject = useCallback(async id => {
    try {
      await execute(token => knowledgeClient.deleteSubject(token, id));
      dispatch({ type: 'subject/remove', id });
      dispatch({ type: 'request/clear' });
      return true;
    } catch (reason) {
      dispatch({ type: 'request/failed', error: errorMessage(reason) });
      return false;
    }
  }, [execute]);


  const addNote = useCallback(async (subjectId, topicId, title, excerpt, studyDuration, studyStartedAtUtc, metrics) => {
    try {
      const note = await execute(token => knowledgeClient.createStudyNote(token, subjectId, topicId, title, excerpt, studyDuration, studyStartedAtUtc, metrics));
      dispatch({ type: 'note/add', note: toNote(note) });
      dispatch({ type: 'request/clear' });
      return note;
    } catch (reason) {
      dispatch({ type: 'request/failed', error: errorMessage(reason) });
      return null;
    }
  }, [execute]);

  const updateNote = useCallback(async (id, topicId, title, excerpt, studyDuration, studyStartedAtUtc, metrics) => {
    try {
      const note = await execute(token => knowledgeClient.updateStudyNote(token, id, topicId, title, excerpt, studyDuration, studyStartedAtUtc, metrics));
      dispatch({ type: 'note/update', note: toNote(note) });
      dispatch({ type: 'request/clear' });
      return note;
    } catch (reason) {
      dispatch({ type: 'request/failed', error: errorMessage(reason) });
      return null;
    }
  }, [execute]);

  const removeNote = useCallback(async id => {
    try {
      await execute(token => knowledgeClient.deleteStudyNote(token, id));
      dispatch({ type: 'note/remove', id });
      dispatch({ type: 'request/clear' });
      return true;
    } catch (reason) {
      dispatch({ type: 'request/failed', error: errorMessage(reason) });
      return false;
    }
  }, [execute]);

  const createMetricDefinition = useCallback(async (name, numberKind) => {
    try {
      const definition = await execute(token => knowledgeClient.createMetricDefinition(token, name, numberKind));
      dispatch({ type: 'knowledge/loaded', knowledge: { metricDefinitions: [...state.metricDefinitions, definition] } });
      dispatch({ type: 'request/clear' });
      return definition;
    } catch (reason) {
      dispatch({ type: 'request/failed', error: errorMessage(reason) });
      return null;
    }
  }, [execute, state.metricDefinitions]);

  const createTopic = useCallback(async (subjectId, name) => {
    try {
      const topic = await execute(token => knowledgeClient.createTopic(token, subjectId, name));
      dispatch({ type: 'topic/add', topic });
      dispatch({ type: 'request/clear' });
      return topic;
    } catch (reason) {
      dispatch({ type: 'request/failed', error: errorMessage(reason) });
      return null;
    }
  }, [execute]);

  const removeTopic = useCallback(async id => {
    try {
      await execute(token => knowledgeClient.deleteTopic(token, id));
      dispatch({ type: 'topic/remove', id });
      dispatch({ type: 'request/clear' });
      return true;
    } catch (reason) {
      dispatch({ type: 'request/failed', error: errorMessage(reason) });
      return false;
    }
  }, [execute]);

  const saveSubjectLayout = useCallback(async (positions, { keepalive = false } = {}) => {
    try {
      await execute(token => knowledgeClient.saveSubjectLayout(token, positions, keepalive));
      dispatch({ type: 'request/clear' });
      return true;
    } catch (reason) {
      dispatch({ type: 'request/failed', error: errorMessage(reason) });
      return false;
    }
  }, [execute]);

  const connectSubjects = useCallback(async (source, target) => {
    try {
      const connection = await execute(token => knowledgeClient.createConnection(token, source, target));
      dispatch({ type: 'connection/add', connection: toConnection(connection) });
      dispatch({ type: 'request/clear' });
      return connection;
    } catch (reason) {
      dispatch({ type: 'request/failed', error: errorMessage(reason) });
      return null;
    }
  }, [execute]);

  const removeConnection = useCallback(async id => {
    try {
      await execute(token => knowledgeClient.deleteConnection(token, id));
      dispatch({ type: 'connection/remove', id });
      dispatch({ type: 'request/clear' });
      return true;
    } catch (reason) {
      dispatch({ type: 'request/failed', error: errorMessage(reason) });
      return false;
    }
  }, [execute]);

  const addSubjectGoal = useCallback(async (subjectId, goal) => {
    try {
      const created = await execute(token => knowledgeClient.createSubjectGoal(token, subjectId, goal));
      dispatch({ type: 'goal/add', goal: created });
      dispatch({ type: 'request/clear' });
      return created;
    } catch (reason) {
      dispatch({ type: 'request/failed', error: errorMessage(reason) });
      return null;
    }
  }, [execute]);

  const updateSubjectGoal = useCallback(async (id, goal) => {
    try {
      const updated = await execute(token => knowledgeClient.updateSubjectGoal(token, id, goal));
      dispatch({ type: 'goal/update', goal: updated });
      dispatch({ type: 'request/clear' });
      return updated;
    } catch (reason) {
      dispatch({ type: 'request/failed', error: errorMessage(reason) });
      return null;
    }
  }, [execute]);

  const removeSubjectGoal = useCallback(async id => {
    try {
      await execute(token => knowledgeClient.deleteSubjectGoal(token, id));
      dispatch({ type: 'goal/remove', id });
      dispatch({ type: 'request/clear' });
      return true;
    } catch (reason) {
      dispatch({ type: 'request/failed', error: errorMessage(reason) });
      return false;
    }
  }, [execute]);

  const completeSubjectGoal = useCallback(async id => {
    try {
      await execute(token => knowledgeClient.completeSubjectGoal(token, id));
      dispatch({ type: 'goal/complete', id, completedAtUtc: new Date().toISOString() });
      dispatch({ type: 'request/clear' });
      return true;
    } catch (reason) {
      dispatch({ type: 'request/failed', error: errorMessage(reason) });
      return false;
    }
  }, [execute]);
  const prioritizeSubjectGoal = useCallback(async (id, swapWithId) => {
    try {
      await execute(token => knowledgeClient.swapSubjectGoalPriority(token, id, swapWithId));
      dispatch({ type: 'goal/prioritize', id, swapWithId });
      return true;
    } catch (reason) {
      dispatch({ type: 'request/failed', error: errorMessage(reason) });
      return false;
    }
  }, [execute]);
  const setSubGoalCompletion = useCallback(async (id, isCompleted) => {
    try {
      await execute(token => knowledgeClient.setSubGoalCompletion(token, id, isCompleted));
      dispatch({ type: 'sub-goal/complete', id, isCompleted, completedAtUtc: new Date().toISOString() });
      dispatch({ type: 'request/clear' });
      return true;
    } catch (reason) {
      dispatch({ type: 'request/failed', error: errorMessage(reason) });
      return false;
    }
  }, [execute]);

  const loadGoalActivity = useCallback(async (from, to) => {
    try {
      const activity = await execute(token => knowledgeClient.getGoalActivity(token, from, to));
      dispatch({ type: 'goal-activity/loaded', activity });
      return activity;
    } catch (reason) {
      dispatch({ type: 'request/failed', error: errorMessage(reason) });
      return [];
    }
  }, [execute]);

  return { ...state, subjectsById, directNotesBySubject, notesBySubject, goalsBySubject, addSubject, updateSubject, removeSubject, addNote, updateNote, removeNote, createMetricDefinition, createTopic, removeTopic, saveSubjectLayout, connectSubjects, removeConnection, addSubjectGoal, updateSubjectGoal, removeSubjectGoal, completeSubjectGoal, prioritizeSubjectGoal, setSubGoalCompletion, loadGoalActivity };
}
