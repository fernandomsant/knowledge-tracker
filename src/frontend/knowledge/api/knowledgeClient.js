const apiBaseUrl = (import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5015').replace(/\/$/, '');

export class KnowledgeApiError extends Error {
  constructor(message, status) {
    super(message);
    this.name = 'KnowledgeApiError';
    this.status = status;
  }
}

async function request(accessToken, path, { method = 'GET', body } = {}) {
  const response = await fetch(`${apiBaseUrl}${path}`, {
    method,
    credentials: 'include',
    headers: { Authorization: `Bearer ${accessToken}`, ...(body ? { 'Content-Type': 'application/json' } : {}) },
    body: body ? JSON.stringify(body) : undefined,
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
    const [subjects, connectionGroups, metricDefinitions] = await Promise.all([
      Promise.all(summaries.map(subject => request(accessToken, `/api/subjects/${subject.id}`))),
      Promise.all(summaries.map(subject => request(accessToken, `/api/subjects/${subject.id}/connections`))),
      request(accessToken, '/api/study-metric-definitions'),
    ]);
    return { subjects, metricDefinitions, connections: [...new Map(connectionGroups.flat().map(item => [item.id, item])).values()] };
  },
  createSubject: (accessToken, name) => request(accessToken, '/api/subjects', { method: 'POST', body: { name } }),
  updateSubject: (accessToken, id, name, description) => request(accessToken, `/api/subjects/${id}`, {
    method: 'PUT', body: { name, description },
  }),
  deleteSubject: (accessToken, id) => request(accessToken, `/api/subjects/${id}`, { method: 'DELETE' }),
  createStudyNote: (accessToken, subjectId, title, content, metrics) => request(accessToken, `/api/subjects/${subjectId}/notes`, {
    method: 'POST', body: { title, content, metrics, studyDuration: '00:00:00', studyStartedAtUtc: new Date().toISOString() },
  }),
  updateStudyNote: (accessToken, id, title, content, metrics) => request(accessToken, `/api/study-notes/${id}`, {
    method: 'PUT', body: { title, content, metrics, studyDuration: '00:00:00' },
  }),
  createMetricDefinition: (accessToken, name, numberKind) => request(accessToken, '/api/study-metric-definitions', {
    method: 'POST', body: { name, numberKind },
  }),
  createConnection: (accessToken, source, target) => request(accessToken, '/api/subject-connections', {
    method: 'POST', body: { subjectId: source, connectedSubjectId: target },
  }),
  deleteConnection: (accessToken, id) => request(accessToken, `/api/subject-connections/${id}`, { method: 'DELETE' }),
};
