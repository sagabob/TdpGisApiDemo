import { useEffect, useRef, useState } from 'react';
import axios from 'axios';
import { fetchWorkspaceEntities } from '@/api/fetchWorkspaceEntities';
import type { GisConnectionDto } from '@/types/gisWorkspace';
import {
  readWorkspaceEntitiesFromSession,
  writeWorkspaceEntitiesToSession,
} from '@/lib/workspaceEntitiesSession';

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

export function useWorkspaceEntities() {
  const [workspaceEntities, setWorkspaceEntities] = useState<GisConnectionDto[] | null>(() =>
    readWorkspaceEntitiesFromSession(),
  );
  const [workspaceEntitiesLoading, setWorkspaceEntitiesLoading] = useState(
    () => readWorkspaceEntitiesFromSession() === null,
  );
  const [workspaceEntitiesError, setWorkspaceEntitiesError] = useState<string | null>(null);
  const [selectedEntityIds, setSelectedEntityIds] = useState<string[]>([]);
  const hasSeededSelection = useRef(false);

  useEffect(() => {
    const cached = readWorkspaceEntitiesFromSession();
    if (cached !== null) {
      setWorkspaceEntities(cached);
      setWorkspaceEntitiesError(null);
      setWorkspaceEntitiesLoading(false);
      return;
    }

    const controller = new AbortController();
    fetchWorkspaceEntities({ signal: controller.signal })
      .then((list) => {
        setWorkspaceEntitiesError(null);
        setWorkspaceEntities(list);
        writeWorkspaceEntitiesToSession(list);
      })
      .catch((err: unknown) => {
        setWorkspaceEntitiesError(getErrorMessage(err));
        setWorkspaceEntities(null);
      })
      .finally(() => {
        setWorkspaceEntitiesLoading(false);
      });
    return () => controller.abort();
  }, []);

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
