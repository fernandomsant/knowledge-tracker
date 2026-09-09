import { useEffect, useRef, useState } from 'react';
import { Check, Copy, KeyRound, X } from '../../icons';
import { authenticationClient } from '../../authentication/api/authenticationClient';
import { IconButton } from '../IconButton';

const MCP_SCOPES = [
  'subjects:read', 'subjects:write', 'topics:read', 'topics:write',
  'notes:read', 'notes:write', 'goals:read', 'goals:write',
  'connections:read', 'connections:write', 'layouts:read', 'layouts:write',
  'metrics:read', 'metrics:write',
];

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
  const [pending, setPending] = useState(false);
  const [error, setError] = useState('');
  const [createdToken, setCreatedToken] = useState(null);
  const [copied, setCopied] = useState(false);

  useEffect(() => {
    if (!open) return undefined;
    setName('Knowly MCP client');
    setExpiresAt(defaultExpiration());
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
    if (!name.trim() || !expiresAt || pending) return;
    setPending(true);
    setError('');
    try {
      const request = {
        name: name.trim(),
        expiresAtUtc: new Date(`${expiresAt}T23:59:59.999`).toISOString(),
        scopes: MCP_SCOPES,
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
            {error ? <p className="modal-error" role="alert">{error}</p> : null}
            <div className="modal-actions"><button type="button" className="primary-button" onClick={onClose}>Done</button></div>
          </>
        ) : (
          <>
            <p>Use this token to connect an MCP client to your knowledge space. It includes read and write access to your study data.</p>
            <label>Token name<input ref={inputRef} value={name} onChange={event => setName(event.target.value)} placeholder="e.g. Claude desktop" maxLength={200} disabled={pending} required /></label>
            <label>Expires on<input type="date" value={expiresAt} min={dateInputValue(new Date())} onChange={event => setExpiresAt(event.target.value)} disabled={pending} required /></label>
            {error ? <p className="modal-error" role="alert">{error}</p> : null}
            <div className="modal-actions"><button type="button" className="ghost-button" onClick={onClose} disabled={pending}>Cancel</button><button type="submit" className="primary-button" disabled={!name.trim() || !expiresAt || pending}>{pending ? 'Creating…' : 'Generate token'}</button></div>
          </>
        )}
      </form>
    </div>
  );
}
