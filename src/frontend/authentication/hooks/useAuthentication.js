import { useCallback, useEffect, useRef, useState } from 'react';
import { authenticationClient } from '../api/authenticationClient';

const unauthenticatedState = { status: 'unauthenticated', accessToken: null, expiresIn: 0 };

export function useAuthentication() {
  const [session, setSession] = useState({ status: 'restoring', accessToken: null, expiresIn: 0 });
  const refreshPromiseRef = useRef(null);

  const applySession = useCallback(response => {
    setSession({
      status: 'authenticated',
      accessToken: response.accessToken,
      expiresIn: response.expiresIn,
    });
    return response;
  }, []);

  const refresh = useCallback(async () => {
    if (!refreshPromiseRef.current) {
      refreshPromiseRef.current = authenticationClient
        .refresh()
        .then(applySession)
        .catch(() => {
          setSession(unauthenticatedState);
          return null;
        })
        .finally(() => {
          refreshPromiseRef.current = null;
        });
    }

    return refreshPromiseRef.current;
  }, [applySession]);

  useEffect(() => {
    void refresh();
  }, [refresh]);

  useEffect(() => {
    if (session.status !== 'authenticated') return undefined;

    const refreshDelay = Math.max(0, (session.expiresIn - 60) * 1000);
    const timer = window.setTimeout(() => void refresh(), refreshDelay);
    return () => window.clearTimeout(timer);
  }, [refresh, session.expiresIn, session.status]);

  const login = useCallback(
    credentials => authenticationClient.login(credentials).then(applySession),
    [applySession]
  );
  const register = useCallback(
    credentials => authenticationClient.register(credentials).then(applySession),
    [applySession]
  );

  return { ...session, login, register };
}
