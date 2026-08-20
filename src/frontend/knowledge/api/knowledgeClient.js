const apiBaseUrl = (import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5015').replace(/\/$/, '');

export class KnowledgeApiError extends Error {
  constructor(message, status) {
    super(message);
    this.name = 'KnowledgeApiError';
    this.status = status;
  }
}

async function request(accessToken, path, { method = 'GET', body, keepalive = false } = {}) {
  const response = await fetch(`${apiBaseUrl}${path}`, {
    method,
    credentials: 'include',
    headers: { Authorization: `Bearer ${accessToken}`, ...(body ? { 'Content-Type': 'application/json' } : {}) },
    body: body ? JSON.stringify(body) : undefined,
    keepalive,
  });
  if (!response.ok) {
    const problem = await response.json().catch(() => null);
    throw new KnowledgeApiError(problem?.detail ?? 'Your knowledge space could not be updated. Try again.', response.status);
  }
  return response.status === 204 ? null : response.json();
}

export const knowledgeClient = {
  async load(accessToken) {
    const summaries = await request(accessToken, '/api/subjects');
    const [subjects, connectionGroups, goalGroups, metricDefinitions, topics] = await Promise.all([
      Promise.all(summaries.map(subject => request(accessToken, `/api/subjects/${subject.id}`))),
      Promise.all(summaries.map(subject => request(accessToken, `/api/subjects/${subject.id}/connections`))),
      Promise.all(summaries.map(subject => request(accessToken, `/api/subjects/${subject.id}/goals`))),
      request(accessToken, '/api/study-metric-definitions'),
      request(accessToken, '/api/topics'),
    ]);
    return { subjects, metricDefinitions, topics, goals: goalGroups.flat(), connections: [...new Map(connectionGroups.flat().map(item => [item.id, item])).values()] };
  },
  createSubject: (accessToken, name, parentSubjectId) => request(accessToken, '/api/subjects', { method: 'POST', body: { name, parentSubjectId: parentSubjectId || null } }),
  updateSubject: (accessToken, id, name, description, parentSubjectId) => request(accessToken, `/api/subjects/${id}`, {
    method: 'PUT', body: { name, description, parentSubjectId: parentSubjectId || null },
  }),
  deleteSubject: (accessToken, id) => request(accessToken, `/api/subjects/${id}`, { method: 'DELETE' }),
  saveSubjectLayout: (accessToken, positions, keepalive) => request(accessToken, '/api/subjects/layout', { method: 'PUT', body: { positions }, keepalive }),
  createStudyNote: (accessToken, subjectId, topicId, title, content, studyDuration, studyStartedAtUtc, metrics) => request(accessToken, `/api/subjects/${subjectId}/notes`, {
    method: 'POST', body: { topicId, title, content, metrics, studyDuration, studyStartedAtUtc },
  }),
  updateStudyNote: (accessToken, id, topicId, title, content, studyDuration, studyStartedAtUtc, metrics) => request(accessToken, `/api/study-notes/${id}`, {
    method: 'PUT', body: { topicId, title, content, metrics, studyDuration, studyStartedAtUtc },
  }),
  listStudyNotes: (accessToken, subjectId) => request(accessToken, `/api/subjects/${subjectId}/notes`),
  deleteStudyNote: (accessToken, id) => request(accessToken, `/api/study-notes/${id}`, { method: 'DELETE' }),
  createMetricDefinition: (accessToken, name, numberKind) => request(accessToken, '/api/study-metric-definitions', {
    method: 'POST', body: { name, numberKind },
  }),
  createConnection: (accessToken, source, target) => request(accessToken, '/api/subject-connections', {
    method: 'POST', body: { subjectId: source, connectedSubjectId: target },
  }),
  deleteConnection: (accessToken, id) => request(accessToken, `/api/subject-connections/${id}`, { method: 'DELETE' }),
  createSubjectGoal: (accessToken, subjectId, goal) => request(accessToken, `/api/subjects/${subjectId}/goals`, { method: 'POST', body: goal }),
  updateSubjectGoal: (accessToken, id, goal) => request(accessToken, `/api/subject-goals/${id}`, { method: 'PUT', body: goal }),
  deleteSubjectGoal: (accessToken, id) => request(accessToken, `/api/subject-goals/${id}`, { method: 'DELETE' }),
  completeSubjectGoal: (accessToken, id) => request(accessToken, `/api/subject-goals/${id}/complete`, { method: 'POST' }),
  swapSubjectGoalPriority: (accessToken, id, swapWithId) => request(accessToken, `/api/subject-goals/${id}/priority`, { method: 'PUT', body: { swapWithId } }),
  setSubGoalCompletion: (accessToken, id, isCompleted) => request(accessToken, `/api/subject-sub-goals/${id}/completion`, { method: 'PUT', body: { isCompleted } }),
  createTopic: (accessToken, subjectId, name) => request(accessToken, `/api/subjects/${subjectId}/topics`, { method: 'POST', body: { name } }),
  deleteTopic: (accessToken, id) => request(accessToken, `/api/topics/${id}`, { method: 'DELETE' }),
  getGoalActivity: (accessToken, from, to) => request(accessToken, `/api/goal-activity?from=${encodeURIComponent(from)}&to=${encodeURIComponent(to)}`),
};
