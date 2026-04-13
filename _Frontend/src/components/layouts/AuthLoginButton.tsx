import { useAuthSession } from '@/hooks/useAuthSession';

/** inset from right: leave room for Mapbox NavigationControl (zoom) top-right */
const btnClassName =
  'fixed top-4 right-14 z-40 box-border inline-flex h-[42px] shrink-0 items-center justify-center gap-2 rounded-md border-2 border-solid border-slate-300 bg-white px-3.5 text-sm font-medium text-slate-800 shadow-md transition hover:bg-slate-50 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-blue-500/30 sm:right-16 md:right-20';

/** Microsoft “four squares” mark (brand colors). */
function MicrosoftIcon({ className }: { className?: string }) {
  return (
    <svg
      className={className}
      viewBox="0 0 21 21"
      width={20}
      height={20}
      aria-hidden
      xmlns="http://www.w3.org/2000/svg"
    >
      <rect x="1" y="1" width="9" height="9" fill="#f25022" />
      <rect x="1" y="11" width="9" height="9" fill="#00a4ef" />
      <rect x="11" y="1" width="9" height="9" fill="#7fba00" />
      <rect x="11" y="11" width="9" height="9" fill="#ffb900" />
    </svg>
  );
}

/**
 * Sign-in state comes from `/api/auth/session` because tokens live in HTTP-only cookies (not readable in JS).
 * Use `vercel dev` or deployed app so `/api/auth/*` exists.
 */
export function AuthLoginButton() {
  const session = useAuthSession();

  if (session === 'loading') {
    return (
      <div
        className={`${btnClassName} min-w-[148px] cursor-default border-slate-200 bg-slate-100 text-transparent shadow-sm animate-pulse`}
        aria-hidden
      >
        <span className="inline-block h-5 w-5 rounded-sm bg-slate-200" />
        <span className="h-4 w-16 rounded bg-slate-200" />
      </div>
    );
  }

  if (session === 'signedIn') {
    return (
      <a
        href="/api/auth/logout"
        className={btnClassName}
        aria-label="Sign out from Microsoft"
      >
        <MicrosoftIcon className="shrink-0" />
        Sign out
      </a>
    );
  }

  return (
    <a
      href="/api/auth/login"
      className={btnClassName}
      aria-label="Sign in with Microsoft"
    >
      <MicrosoftIcon className="shrink-0" />
      Sign in
    </a>
  );
}
