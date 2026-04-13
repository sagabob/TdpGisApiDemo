import { useEffect, useState } from 'react';

export type AuthSessionState = 'loading' | 'signedIn' | 'signedOut';

/**
 * Sign-in state from `/api/auth/session` (tokens are HTTP-only cookies).
 */
export function useAuthSession(): AuthSessionState {
  const [session, setSession] = useState<AuthSessionState>('loading');

  useEffect(() => {
    let cancelled = false;
    (async () => {
      try {
        const res = await fetch('/api/auth/session', {
          credentials: 'same-origin',
          headers: { Accept: 'application/json' },
        });
        if (!res.ok) throw new Error('session');
        const data = (await res.json()) as { authenticated?: boolean };
        if (!cancelled) setSession(data.authenticated ? 'signedIn' : 'signedOut');
      } catch {
        if (!cancelled) setSession('signedOut');
      }
    })();
    return () => {
      cancelled = true;
    };
  }, []);

  return session;
}
