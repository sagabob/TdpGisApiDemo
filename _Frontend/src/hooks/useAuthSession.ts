import { useEffect, useState } from 'react';

export type AuthSessionState = 'loading' | 'signedIn' | 'signedOut';

export type AuthSession = {
  status: AuthSessionState;
  /** Signed-in user's email when available from Entra claims. */
  email: string | null;
};

/**
 * Sign-in state from `/api/auth/session` (tokens are HTTP-only cookies).
 */
export function useAuthSession(): AuthSession {
  const [session, setSession] = useState<AuthSession>({ status: 'loading', email: null });

  useEffect(() => {
    let cancelled = false;
    (async () => {
      try {
        const res = await fetch('/api/auth/session', {
          credentials: 'same-origin',
          headers: { Accept: 'application/json' },
        });
        if (!res.ok) throw new Error('session');
        const data = (await res.json()) as { authenticated?: boolean; email?: string | null };
        if (!cancelled) {
          setSession(
            data.authenticated
              ? { status: 'signedIn', email: typeof data.email === 'string' ? data.email : null }
              : { status: 'signedOut', email: null },
          );
        }
      } catch {
        if (!cancelled) setSession({ status: 'signedOut', email: null });
      }
    })();
    return () => {
      cancelled = true;
    };
  }, []);

  return session;
}
