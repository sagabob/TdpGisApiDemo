import { useContext, useEffect, useRef, useState } from 'react';
import SearchContext from '@/contexts/SearchContext';

/** Width is reserved by the parent in `SearchBar`; children use `w-full`. */

function LockIcon({ className }: { className?: string }) {
  return (
    <svg
      className={className}
      xmlns="http://www.w3.org/2000/svg"
      viewBox="0 0 24 24"
      fill="currentColor"
      aria-hidden
    >
      <path
        fillRule="evenodd"
        d="M12 1.5a5.25 5.25 0 00-5.25 5.25v3a3 3 0 00-3 3v6.75a3 3 0 003 3h10.5a3 3 0 003-3v-6.75a3 3 0 00-3-3v-3c0-2.9-2.35-5.25-5.25-5.25zm3.75 8.25v-3a3.75 3.75 0 10-7.5 0v3h7.5z"
        clipRule="evenodd"
      />
    </svg>
  );
}

function EntitySearchLoadingButton({ label }: { label: string }) {
  return (
    <div className="relative z-1 w-full">
      <button
        type="button"
        disabled
        aria-busy="true"
        aria-label={label}
        className="box-border flex h-10.5 w-full cursor-wait items-center justify-center rounded-md border-2 border-solid border-slate-300 bg-white px-2 text-sm shadow-md outline-none ring-0 disabled:cursor-wait disabled:opacity-100"
      >
      <svg
        className="h-5 w-5 shrink-0 animate-spin text-blue-600"
        xmlns="http://www.w3.org/2000/svg"
        fill="none"
        viewBox="0 0 24 24"
        aria-hidden
      >
        <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" />
        <path
          className="opacity-75"
          fill="currentColor"
          d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"
        />
      </svg>
      </button>
    </div>
  );
}

export function WorkspaceEntityFilters() {
  const {
    workspaceEntities,
    workspaceEntitiesLoading,
    workspaceEntitiesError,
    selectedEntityIds,
    toggleEntitySelection,
  } = useContext(SearchContext);

  const [open, setOpen] = useState(false);
  const rootRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (!open) return;
    const onDocMouseDown = (e: MouseEvent) => {
      if (rootRef.current && !rootRef.current.contains(e.target as Node)) {
        setOpen(false);
      }
    };
    const onKey = (e: KeyboardEvent) => {
      if (e.key === 'Escape') setOpen(false);
    };
    document.addEventListener('mousedown', onDocMouseDown);
    document.addEventListener('keydown', onKey);
    return () => {
      document.removeEventListener('mousedown', onDocMouseDown);
      document.removeEventListener('keydown', onKey);
    };
  }, [open]);

  if (workspaceEntitiesLoading) {
    return <EntitySearchLoadingButton label="Loading entities" />;
  }

  if (workspaceEntitiesError !== null) {
    return (
      <button
        type="button"
        disabled
        title={workspaceEntitiesError}
        className="h-10.5 w-full rounded-md border border-dashed border-amber-300 bg-amber-50 px-2 text-xs text-amber-900"
      >
        Entities unavailable
      </button>
    );
  }

  if (!workspaceEntities?.length) {
    return (
      <button
        type="button"
        disabled
        className="h-10.5 w-full rounded-md border border-dashed border-slate-300 bg-slate-50 px-2 text-xs text-slate-600"
      >
        No entities
      </button>
    );
  }

  const total = workspaceEntities.length;
  const selected = selectedEntityIds.length;

  return (
    <div ref={rootRef} className="relative w-full">
      <button
        type="button"
        aria-expanded={open}
        aria-haspopup="listbox"
        aria-controls="workspace-entity-listbox"
        title="Search in entities"
        aria-label={`Entities: ${selected} of ${total} selected. Open to change.`}
        onClick={() => setOpen((v) => !v)}
        className="flex h-10.5 w-full min-w-0 items-center justify-center gap-1 rounded-md border border-solid border-gray-300 bg-white px-2 text-sm font-medium text-slate-700 shadow-sm transition hover:bg-slate-50 focus:border-blue-600 focus:outline-none focus:ring-2 focus:ring-blue-500/30"
      >
        <span className="min-w-0 truncate text-xs sm:text-sm">Entities</span>
        <span className="shrink-0 rounded bg-slate-100 px-1 py-0.5 text-xs tabular-nums text-slate-600">
          {selected}/{total}
        </span>
        <svg
          className={`h-4 w-4 text-slate-500 transition ${open ? 'rotate-180' : ''}`}
          fill="none"
          stroke="currentColor"
          viewBox="0 0 24 24"
          aria-hidden
        >
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 9l-7 7-7-7" />
        </svg>
      </button>

      {open && (
        <div
          id="workspace-entity-listbox"
          role="listbox"
          className="absolute right-0 top-[calc(100%+6px)] z-60 w-[min(18rem,calc(100vw-1.5rem))] rounded-md border border-slate-200 bg-white py-2 shadow-xl"
        >
          <p className="border-b border-slate-100 px-3 pb-2 text-xs font-medium text-slate-600">
            Search in entities
          </p>
          <div className="max-h-[min(14rem,50vh)] overflow-y-auto overscroll-contain px-2 pt-2">
            <ul className="m-0 list-none space-y-2 p-0">
              {workspaceEntities.map((ent) => {
                const id = String(ent.id);
                const checked = selectedEntityIds.includes(id);
                const isPrivate = ent.isPrivate === true;
                const label = ent.entityLabel || ent.name;
                return (
                  <li key={id} className="flex items-start gap-2">
                    <input
                      id={`entity-${id}`}
                      type="checkbox"
                      checked={checked}
                      onChange={() => toggleEntitySelection(id)}
                      className="mt-0.5 h-4 w-4 shrink-0 rounded border-slate-300 text-blue-600 focus:ring-blue-500"
                    />
                    <label
                      htmlFor={`entity-${id}`}
                      className="flex min-w-0 cursor-pointer items-start gap-1.5 text-left text-sm leading-snug text-slate-800"
                    >
                      <span className="min-w-0">{label}</span>
                      {isPrivate ? (
                        <span
                          className="mt-0.5 inline-flex shrink-0 items-center text-slate-500"
                          title="Private entity"
                          aria-label="Private entity"
                        >
                          <LockIcon className="h-3.5 w-3.5" />
                        </span>
                      ) : null}
                    </label>
                  </li>
                );
              })}
            </ul>
          </div>
          {selectedEntityIds.length === 0 && (
            <p className="m-0 border-t border-amber-100 bg-amber-50 px-3 py-2 text-xs text-amber-800">
              Select at least one entity to search.
            </p>
          )}
        </div>
      )}
    </div>
  );
}
