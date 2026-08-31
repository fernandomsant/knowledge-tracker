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

async function streamClassificationUpdates(accessToken, checkpoint, onUpdate, signal) {
  const url = new URL(`${apiBaseUrl}/api/study-notes/classification-events`);
  url.searchParams.set('sinceUtc', checkpoint.completedAtUtc);
  url.searchParams.set('afterJobId', checkpoint.jobId);
  const response = await fetch(url, {
    credentials: 'include',
    headers: {
      Accept: 'text/event-stream',
      Authorization: `Bearer ${accessToken}`,
    },
    signal,
  });
  if (!response.ok) {
    const problem = await response.json().catch(() => null);
    throw new KnowledgeApiError(problem?.detail ?? 'Live classification updates are unavailable.', response.status);
  }
  if (!response.body)
    throw new KnowledgeApiError('Live classification updates are unavailable.', response.status);

  const reader = response.body.getReader();
  const decoder = new TextDecoder();
  let buffer = '';
  while (true) {
    const { done, value } = await reader.read();
    if (done) break;
    buffer += decoder.decode(value, { stream: true });

    let boundary = buffer.indexOf('\n\n');
    while (boundary >= 0) {
      const block = buffer.slice(0, boundary);
      buffer = buffer.slice(boundary + 2);
      const lines = block.split('\n');
      const eventName = lines.find(line => line.startsWith('event:'))?.slice(6).trim();
      const data = lines.filter(line => line.startsWith('data:')).map(line => line.slice(5).trimStart()).join('\n');
      if (eventName === 'note-classification' && data)
        onUpdate(JSON.parse(data));
      boundary = buffer.indexOf('\n\n');
    }
  }

  if (!signal.aborted)
    throw new KnowledgeApiError('The live classification connection closed.', 0);
}

export const knowledgeClient = {
  async load(accessToken) {
    const summaries = await request(accessToken, '/api/subjects');
    const [subjects, connectionGroups, goalGroups, metricDefinitions, topics, notes] = await Promise.all([
      Promise.all(summaries.map(subject => request(accessToken, `/api/subjects/${subject.id}`))),
      Promise.all(summaries.map(subject => request(accessToken, `/api/subjects/${subject.id}/connections`))),
      Promise.all(summaries.map(subject => request(accessToken, `/api/subjects/${subject.id}/goals`))),
      request(accessToken, '/api/study-metric-definitions'),
      request(accessToken, '/api/topics'),
      request(accessToken, '/api/study-notes'),
    ]);
    return { subjects, metricDefinitions, topics, notes, goals: goalGroups.flat(), connections: [...new Map(connectionGroups.flat().map(item => [item.id, item])).values()] };
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
  createUnclassifiedStudyNote: (accessToken, title, content, studyDuration, studyStartedAtUtc, metrics) => request(accessToken, '/api/study-notes', {
    method: 'POST', body: { title, content, metrics, studyDuration, studyStartedAtUtc },
  }),
  updateStudyNote: (accessToken, id, topicId, title, content, studyDuration, studyStartedAtUtc, metrics) => request(accessToken, `/api/study-notes/${id}`, {
    method: 'PUT', body: { topicId, title, content, metrics, studyDuration, studyStartedAtUtc },
  }),
  listStudyNotes: accessToken => request(accessToken, '/api/study-notes'),
  streamClassificationUpdates,
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
