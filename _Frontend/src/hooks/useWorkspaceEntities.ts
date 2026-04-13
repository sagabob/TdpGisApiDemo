import { useEffect, useRef, useState } from 'react';
import axios from 'axios';
import { ensureGisApiCookie } from '@/api/ensureGisApiCookie';
import { fetchWorkspaceEntities } from '@/api/fetchWorkspaceEntities';
import type { GisConnectionDto } from '@/types/gisWorkspace';
import {
  clearWorkspaceEntitiesAuthCache,
  readWorkspaceEntitiesFromSession,
  writeWorkspaceEntitiesToSession,
  type WorkspaceEntitiesAuthScope,
} from '@/lib/workspaceEntitiesSession';
import { useAuthSession, type AuthSessionState } from '@/hooks/useAuthSession';

function getErrorMessage(err: unknown): string {
  let msg = 'Failed to load workspace entities.';
  if (axios.isAxiosError(err)) {
    const data = err.response?.data;
    if (data && typeof data === 'object' && data !== null && 'message' in data) {
      msg = String((data as { message: string }).message);
    } else if (err.message) {
      msg = err.message;
    }
  } else if (err instanceof Error) {
    msg = err.message;
  }
  return msg;
}

const authRedirectOnFirstVisit = import.meta.env.VITE_AUTH_REDIRECT_ON_LOAD === 'true';

export function useWorkspaceEntities() {
  const session = useAuthSession();

  useEffect(() => {
    if (!authRedirectOnFirstVisit || session !== 'signedOut') return;
    window.location.assign('/api/auth/login');
  }, [session]);

  const [workspaceEntities, setWorkspaceEntities] = useState<GisConnectionDto[] | null>(null);
  const [workspaceEntitiesLoading, setWorkspaceEntitiesLoading] = useState(true);
  const [workspaceEntitiesError, setWorkspaceEntitiesError] = useState<string | null>(null);
  const [selectedEntityIds, setSelectedEntityIds] = useState<string[]>([]);
  const hasSeededSelection = useRef(false);
  const prevSessionRef = useRef<AuthSessionState | null>(null);

  useEffect(() => {
    hasSeededSelection.current = false;
  }, [session]);

  useEffect(() => {
    if (session === 'loading') {
      return;
    }

    if (session === 'signedIn' && prevSessionRef.current === 'signedOut') {
      clearWorkspaceEntitiesAuthCache();
    }
    prevSessionRef.current = session;

    const scope: WorkspaceEntitiesAuthScope = session === 'signedIn' ? 'auth' : 'anon';
    const cached = readWorkspaceEntitiesFromSession(scope);
    if (cached !== null) {
      setWorkspaceEntities(cached);
      setWorkspaceEntitiesError(null);
      setWorkspaceEntitiesLoading(false);
      return;
    }

    setWorkspaceEntitiesLoading(true);
    const controller = new AbortController();

    const run = async () => {
      await ensureGisApiCookie();
      return fetchWorkspaceEntities({ signal: controller.signal });
    };

    run()
      .then((list) => {
        setWorkspaceEntitiesError(null);
        setWorkspaceEntities(list);
        writeWorkspaceEntitiesToSession(list, scope);
      })
      .catch((err: unknown) => {
        setWorkspaceEntitiesError(getErrorMessage(err));
        setWorkspaceEntities(null);
      })
      .finally(() => {
        setWorkspaceEntitiesLoading(false);
      });
    return () => controller.abort();
  }, [session]);

  useEffect(() => {
    if (
      workspaceEntities &&
      workspaceEntities.length > 0 &&
      !hasSeededSelection.current
    ) {
      setSelectedEntityIds(workspaceEntities.map((e) => e.id));
      hasSeededSelection.current = true;
    }
    if (!workspaceEntities?.length) {
      hasSeededSelection.current = false;
      setSelectedEntityIds([]);
    }
  }, [workspaceEntities]);

  const toggleEntitySelection = (entityId: string) => {
    setSelectedEntityIds((prev) =>
      prev.includes(entityId) ? prev.filter((id) => id !== entityId) : [...prev, entityId],
    );
  };

  return {
    workspaceEntities,
    workspaceEntitiesLoading,
    workspaceEntitiesError,
    selectedEntityIds,
    setSelectedEntityIds,
    toggleEntitySelection,
  };
}
