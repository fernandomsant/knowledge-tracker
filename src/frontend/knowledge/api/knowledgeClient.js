const apiBaseUrl = (import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5015').replace(/\/$/, '');

export class KnowledgeApiError extends Error {
  constructor(message, status) {
    super(message);
    this.name = 'KnowledgeApiError';
    this.status = status;
  }
}

async function request(accessToken, path, { method = 'GET', body, keepalive = false, workspaceId } = {}) {
  const response = await fetch(`${apiBaseUrl}${path}`, {
    method,
    credentials: 'include',
    headers: {
      Authorization: `Bearer ${accessToken}`,
      ...(workspaceId ? { 'X-Workspace-Id': workspaceId } : {}),
      ...(body ? { 'Content-Type': 'application/json' } : {}),
    },
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
  async load(accessToken, workspaceId) {
    const requestOptions = { workspaceId };
    const summaries = await request(accessToken, '/api/subjects', requestOptions);
    const [subjects, connectionGroups, goalGroups, metricDefinitions, topics] = await Promise.all([
      Promise.all(summaries.map(subject => request(accessToken, `/api/subjects/${subject.id}`, requestOptions))),
      Promise.all(summaries.map(subject => request(accessToken, `/api/subjects/${subject.id}/connections`, requestOptions))),
      Promise.all(summaries.map(subject => request(accessToken, `/api/subjects/${subject.id}/goals`, requestOptions))),
      request(accessToken, '/api/study-metric-definitions', requestOptions),
      request(accessToken, '/api/topics', requestOptions),
    ]);
    return { subjects, metricDefinitions, topics, goals: goalGroups.flat(), connections: [...new Map(connectionGroups.flat().map(item => [item.id, item])).values()] };
  },
  createSubject: (accessToken, name, parentSubjectId, workspaceId) => request(accessToken, '/api/subjects', { method: 'POST', body: { name, parentSubjectId: parentSubjectId || null }, workspaceId }),
  updateSubject: (accessToken, id, name, description, parentSubjectId, workspaceId) => request(accessToken, `/api/subjects/${id}`, {
    method: 'PUT', body: { name, description, parentSubjectId: parentSubjectId || null }, workspaceId,
  }),
  deleteSubject: (accessToken, id, workspaceId) => request(accessToken, `/api/subjects/${id}`, { method: 'DELETE', workspaceId }),
  saveSubjectLayout: (accessToken, positions, keepalive, workspaceId) => request(accessToken, '/api/subjects/layout', { method: 'PUT', body: { positions }, keepalive, workspaceId }),
  createStudyNote: (accessToken, subjectId, topicId, title, content, studyDuration, studyStartedAtUtc, metrics, workspaceId) => request(accessToken, `/api/subjects/${subjectId}/notes`, {
    method: 'POST', body: { topicId, title, content, metrics, studyDuration, studyStartedAtUtc }, workspaceId,
  }),
  updateStudyNote: (accessToken, id, topicId, title, content, studyDuration, studyStartedAtUtc, metrics, workspaceId) => request(accessToken, `/api/study-notes/${id}`, {
    method: 'PUT', body: { topicId, title, content, metrics, studyDuration, studyStartedAtUtc }, workspaceId,
  }),
  deleteStudyNote: (accessToken, id, workspaceId) => request(accessToken, `/api/study-notes/${id}`, { method: 'DELETE', workspaceId }),
  createMetricDefinition: (accessToken, name, numberKind, workspaceId) => request(accessToken, '/api/study-metric-definitions', {
    method: 'POST', body: { name, numberKind }, workspaceId,
  }),
  createConnection: (accessToken, source, target, workspaceId) => request(accessToken, '/api/subject-connections', {
    method: 'POST', body: { subjectId: source, connectedSubjectId: target }, workspaceId,
  }),
  deleteConnection: (accessToken, id, workspaceId) => request(accessToken, `/api/subject-connections/${id}`, { method: 'DELETE', workspaceId }),
  createSubjectGoal: (accessToken, subjectId, goal, workspaceId) => request(accessToken, `/api/subjects/${subjectId}/goals`, { method: 'POST', body: goal, workspaceId }),
  updateSubjectGoal: (accessToken, id, goal, workspaceId) => request(accessToken, `/api/subject-goals/${id}`, { method: 'PUT', body: goal, workspaceId }),
  deleteSubjectGoal: (accessToken, id, workspaceId) => request(accessToken, `/api/subject-goals/${id}`, { method: 'DELETE', workspaceId }),
  completeSubjectGoal: (accessToken, id, workspaceId) => request(accessToken, `/api/subject-goals/${id}/complete`, { method: 'POST', workspaceId }),
  swapSubjectGoalPriority: (accessToken, id, swapWithId, workspaceId) => request(accessToken, `/api/subject-goals/${id}/priority`, { method: 'PUT', body: { swapWithId }, workspaceId }),
  setSubGoalCompletion: (accessToken, id, isCompleted, workspaceId) => request(accessToken, `/api/subject-sub-goals/${id}/completion`, { method: 'PUT', body: { isCompleted }, workspaceId }),
  createTopic: (accessToken, subjectId, name, workspaceId) => request(accessToken, `/api/subjects/${subjectId}/topics`, { method: 'POST', body: { name }, workspaceId }),
  deleteTopic: (accessToken, id, workspaceId) => request(accessToken, `/api/topics/${id}`, { method: 'DELETE', workspaceId }),
  getGoalActivity: (accessToken, from, to, workspaceId) => request(accessToken, `/api/goal-activity?from=${encodeURIComponent(from)}&to=${encodeURIComponent(to)}`, { workspaceId }),
};
