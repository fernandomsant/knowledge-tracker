const apiBaseUrl = (import.meta.env.VITE_API_BASE_URL ?? '').replace(/\/$/, '');

export class AuthenticationError extends Error {
  constructor(message, status) {
    super(message);
    this.name = 'AuthenticationError';
    this.status = status;
  }
}

async function request(path, body) {
  const response = await fetch(`${apiBaseUrl}${path}`, {
    method: 'POST',
    credentials: 'include',
    headers: body ? { 'Content-Type': 'application/json' } : undefined,
    body: body ? JSON.stringify(body) : undefined,
  });

  if (!response.ok) {
    const messages = {
      401: 'Your login or password is incorrect.',
      409: 'This login is already in use.',
    };
    throw new AuthenticationError(messages[response.status] ?? 'Authentication is unavailable. Try again.', response.status);
  }

  return response.status === 204 ? null : response.json();
}

export const authenticationClient = {
  login: credentials => request('/api/authentication/login', credentials),
  refresh: () => request('/api/authentication/refresh'),
  async register(credentials) {
    await request('/api/authentication/register', credentials);
    return request('/api/authentication/login', credentials);
  },
};
