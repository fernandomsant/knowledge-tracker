import { useEffect, useRef, useState } from 'react';
import { Check, Copy, KeyRound, X } from '../../icons';
import { authenticationClient } from '../../authentication/api/authenticationClient';
import { IconButton } from '../IconButton';

const MCP_SCOPES = [
  ['subjects:read', 'Read subjects'], ['subjects:write', 'Create and edit subjects'],
  ['topics:read', 'Read topics'], ['topics:write', 'Create and edit topics'],
  ['notes:read', 'Read notes'], ['notes:write', 'Create and edit notes'],
  ['goals:read', 'Read goals'], ['goals:write', 'Create and edit goals'],
  ['connections:read', 'Read connections'], ['connections:write', 'Create and edit connections'],
  ['layouts:read', 'Read layouts'], ['layouts:write', 'Edit layouts'],
  ['metrics:read', 'Read metrics'], ['metrics:write', 'Create and edit metrics'],
];
const defaultScopes = MCP_SCOPES.map(([scope]) => scope);

const dateInputValue = date => date.toISOString().slice(0, 10);
const defaultExpiration = () => {
  const date = new Date();
  date.setDate(date.getDate() + 30);
  return dateInputValue(date);
};

export function McpAccessTokenModal({ open, accessToken, refreshAccessToken, onClose }) {
  const inputRef = useRef(null);
  const [name, setName] = useState('Knowly MCP client');
  const [expiresAt, setExpiresAt] = useState(defaultExpiration);
  const [scopes, setScopes] = useState(defaultScopes);
  const [pending, setPending] = useState(false);
  const [error, setError] = useState('');
  const [createdToken, setCreatedToken] = useState(null);
  const [copied, setCopied] = useState(false);

  useEffect(() => {
    if (!open) return undefined;
    setName('Knowly MCP client');
    setExpiresAt(defaultExpiration());
    setScopes(defaultScopes);
    setPending(false);
    setError('');
    setCreatedToken(null);
    setCopied(false);
    const frame = requestAnimationFrame(() => inputRef.current?.focus());
    return () => cancelAnimationFrame(frame);
  }, [open]);

  if (!open) return null;

  const submit = async event => {
    event.preventDefault();
    if (!name.trim() || !expiresAt || scopes.length === 0 || pending) return;
    setPending(true);
    setError('');
    try {
      const request = {
        name: name.trim(),
        expiresAtUtc: new Date(`${expiresAt}T23:59:59.999`).toISOString(),
        scopes,
      };
      let result;
      try {
        result = await authenticationClient.createMcpAccessToken(accessToken, request);
      } catch (reason) {
        if (reason?.status !== 401) throw reason;
        const refreshedSession = await refreshAccessToken();
        if (!refreshedSession) throw reason;
        result = await authenticationClient.createMcpAccessToken(refreshedSession.accessToken, request);
      }
      setCreatedToken(result);
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : 'The MCP access token could not be created. Try again.');
    } finally {
      setPending(false);
    }
  };

  const copyToken = async () => {
    try {
      await navigator.clipboard.writeText(createdToken.accessToken);
      setCopied(true);
    } catch {
      setError('Copying is unavailable. Select the token and copy it manually.');
    }
  };

  const toggleScope = scope => {
    setScopes(current => current.includes(scope)
      ? current.filter(value => value !== scope)
      : [...current, scope]);
  };

  return (
    <div className="modal-backdrop" onMouseDown={event => { if (event.target === event.currentTarget && !pending) onClose(); }}>
      <form className="modal mcp-token-modal" onSubmit={submit}>
        <div className="modal-head">
          <div><span><KeyRound size={13}/> MCP ACCESS</span><h2>{createdToken ? 'Token created' : 'Create an MCP token'}</h2></div>
          <IconButton type="button" label="Close MCP token dialog" onClick={onClose} disabled={pending}><X size={19}/></IconButton>
        </div>
        {createdToken ? (
          <>
            <p>Copy this token now. For security, it will not be shown again after you close this dialog.</p>
            <label>Access token<textarea className="mcp-token-value" value={createdToken.accessToken} readOnly rows="3" onFocus={event => event.target.select()} /></label>
            <button type="button" className="primary-button mcp-copy-button" onClick={() => void copyToken}>{copied ? <Check size={15}/> : <Copy size={15}/>} {copied ? 'Copied' : 'Copy token'}</button>
            <p className="mcp-token-install">Replace <code>McpServer:AccessToken</code> in the MCP server’s <code>appsettings.mcp.local.json</code> (or <code>McpServer__AccessToken</code> in its environment), then restart the MCP server. This token authenticates the MCP server to Knowly; it is not sent to the MCP client directly.</p>
            {error ? <p className="modal-error" role="alert">{error}</p> : null}
            <div className="modal-actions"><button type="button" className="primary-button" onClick={onClose}>Done</button></div>
          </>
        ) : (
          <>
            <p>Choose exactly what this MCP client may read or change.</p>
            <label>Token name<input ref={inputRef} value={name} onChange={event => setName(event.target.value)} placeholder="e.g. Claude desktop" maxLength={200} disabled={pending} required /></label>
            <label>Expires on<input type="date" value={expiresAt} min={dateInputValue(new Date())} onChange={event => setExpiresAt(event.target.value)} disabled={pending} required /></label>
            <fieldset className="mcp-scopes" disabled={pending}>
              <legend>Permissions</legend>
              <div>{MCP_SCOPES.map(([scope, label]) => <label key={scope}><input type="checkbox" checked={scopes.includes(scope)} onChange={() => toggleScope(scope)} />{label}</label>)}</div>
            </fieldset>
            {error ? <p className="modal-error" role="alert">{error}</p> : null}
            <div className="modal-actions"><button type="button" className="ghost-button" onClick={onClose} disabled={pending}>Cancel</button><button type="submit" className="primary-button" disabled={!name.trim() || !expiresAt || scopes.length === 0 || pending}>{pending ? 'Creating…' : 'Generate token'}</button></div>
          </>
        )}
      </form>
    </div>
  );
}
