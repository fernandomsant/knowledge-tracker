# Deployment is localhost-dependent

Affected files:

- `src/frontend/knowledge/api/knowledgeClient.js:1`
- `src/frontend/authentication/api/authenticationClient.js:1`

Both clients default to `http://localhost:5015`. If a deployed frontend omits `VITE_API_BASE_URL`, each browser calls its own machine instead of the API.

Improvement suggestion: require an explicit production API base URL, or intentionally use same-origin requests in production while retaining the localhost default only for development. Do not implement until requested.
