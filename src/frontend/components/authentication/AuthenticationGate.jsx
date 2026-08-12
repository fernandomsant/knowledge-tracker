import { useState } from 'react';
import { Brain } from '../../icons';
import { AuthenticationContext } from '../../authentication/context/AuthenticationContext';
import { useAuthentication } from '../../authentication/hooks/useAuthentication';

function AuthenticationForm({ onSubmit, pending, error }) {
  const [mode, setMode] = useState('login');
  const [login, setLogin] = useState('');
  const [password, setPassword] = useState('');
  const isRegistration = mode === 'register';

  const submit = event => {
    event.preventDefault();
    onSubmit(mode, { login: login.trim(), password });
  };

  return (
    <main className="authentication-page">
      <section className="authentication-promise" aria-hidden="true">
        <div className="authentication-brand"><span><Brain size={22}/></span><strong>knowly</strong></div>
        <div className="authentication-statement"><span>YOUR PERSONAL STUDY MAP</span><h1>Make each hour of study connect.</h1><p>Capture the subjects, notes, and relationships that turn scattered learning into durable understanding.</p></div>
        <div className="authentication-thread"><i/><i/><i/><b>ideas become a map</b></div>
      </section>
      <section className="authentication-card-area">
        <form className="authentication-card" onSubmit={submit}>
          <div className="authentication-mode" role="tablist" aria-label="Authentication mode">
            <button type="button" className={!isRegistration ? 'active' : ''} onClick={() => setMode('login')}>Sign in</button>
            <button type="button" className={isRegistration ? 'active' : ''} onClick={() => setMode('register')}>Create account</button>
          </div>
          <div className="authentication-copy"><span>{isRegistration ? 'START YOUR SPACE' : 'WELCOME BACK'}</span><h2>{isRegistration ? 'Begin with one subject.' : 'Return to your map.'}</h2><p>{isRegistration ? 'Choose a login and password to create your personal study space.' : 'Sign in to continue organising what you are learning.'}</p></div>
          <label>Login<input autoComplete="username" value={login} onChange={event => setLogin(event.target.value)} required maxLength="256" /></label>
          <label>Password<input type="password" autoComplete={isRegistration ? 'new-password' : 'current-password'} value={password} onChange={event => setPassword(event.target.value)} required minLength="1" maxLength="1024" /></label>
          {error ? <p className="authentication-error" role="alert">{error}</p> : null}
          <button className="authentication-submit" disabled={pending}>{pending ? 'Working…' : isRegistration ? 'Create account' : 'Sign in'}</button>
        </form>
      </section>
    </main>
  );
}

export function AuthenticationGate({ children }) {
  const { status, accessToken, user, login, register } = useAuthentication();
  const [pending, setPending] = useState(false);
  const [error, setError] = useState('');

  const submit = async (mode, credentials) => {
    setPending(true);
    setError('');
    try {
      await (mode === 'register' ? register(credentials) : login(credentials));
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : 'Authentication is unavailable. Try again.');
    } finally {
      setPending(false);
    }
  };

  if (status === 'restoring') {
    return <main className="authentication-loading" aria-live="polite">Restoring your study space…</main>;
  }

  return status === 'authenticated'
    ? <AuthenticationContext.Provider value={{ accessToken, user }}>{children}</AuthenticationContext.Provider>
    : <AuthenticationForm onSubmit={submit} pending={pending} error={error}/>;
}
