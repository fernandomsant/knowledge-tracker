const apiBaseUrl = (import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5015').replace(/\/$/, '');

export class AuthenticationError extends Error {
  constructor(message, status) {
    super(message);
    this.name = 'AuthenticationError';
    this.status = status;
  }
}

/** @param {string} path @param {unknown} body @param {number} [timeoutMs] */
async function request(path, body, timeoutMs) {
  const controller = timeoutMs ? new AbortController() : null;
  const timeout = controller ? window.setTimeout(() => controller.abort(), timeoutMs) : null;
  let response;
  try {
    response = await fetch(`${apiBaseUrl}${path}`, {
      method: 'POST',
      credentials: 'include',
      headers: body ? { 'Content-Type': 'application/json' } : undefined,
      body: body ? JSON.stringify(body) : undefined,
      signal: controller?.signal,
    });
  } catch (reason) {
    if (reason?.name === 'AbortError') throw new AuthenticationError('Session refresh timed out. Please sign in again.', 408);
    throw reason;
  } finally {
    if (timeout !== null) window.clearTimeout(timeout);
  }

  if (!response.ok) {
    const messages = {
      401: 'Your login or password is incorrect.',
      409: 'This login is already in use.',
    };
    throw new AuthenticationError(messages[response.status] ?? 'Authentication is unavailable. Try again.', response.status);
  }

  return response.status === 204 ? null : response.json();
}

async function authenticatedRequest(path, accessToken) {
  const response = await fetch(`${apiBaseUrl}${path}`, {
    credentials: 'include',
    headers: { Authorization: `Bearer ${accessToken}` },
  });
  if (!response.ok) throw new AuthenticationError('Authentication is unavailable. Try again.', response.status);
  return response.json();
}

async function authenticatedPost(path, accessToken) {
  const response = await fetch(`${apiBaseUrl}${path}`, {
    method: 'POST',
    credentials: 'include',
    headers: { Authorization: `Bearer ${accessToken}` },
  });
  if (!response.ok) throw new AuthenticationError('Authentication is unavailable. Try again.', response.status);
}

export const authenticationClient = {
  login: credentials => request('/api/authentication/login', credentials, undefined),
  refresh: () => request('/api/authentication/refresh', undefined, 10_000),
  logout: accessToken => authenticatedPost('/api/authentication/logout', accessToken),
  currentUser: accessToken => authenticatedRequest('/api/current-user', accessToken),
  async register(credentials) {
    await request('/api/authentication/register', credentials, undefined);
    return request('/api/authentication/login', credentials, undefined);
  },
};
