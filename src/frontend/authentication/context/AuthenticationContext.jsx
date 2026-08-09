import { createContext, useContext } from 'react';

export const AuthenticationContext = createContext(null);

export function useAuthenticationSession() {
  const session = useContext(AuthenticationContext);
  if (session === null) throw new Error('useAuthenticationSession must be used inside AuthenticationGate.');
  return session;
}
