"use client";

import { useEffect, useRef, useLayoutEffect } from "react";
import { HubConnectionState } from "@microsoft/signalr";
import { useQueryClient, type QueryClient } from "@tanstack/react-query";
import { getSignalRConnection, disconnectSignalR } from "@/lib/signalr";
import { queryKeys } from "@/lib/query-keys";
import { useRealtimeStore } from "@/stores/realtime.store";

/**
 * Manages the SignalR connection lifecycle.
 * Connects on mount (when accessToken is available), disconnects on unmount.
 * Registers event handlers that invalidate TanStack Query cache on push events.
 *
 * Uses refs for queryClient and store setters to avoid reconnection on identity changes.
 * Event handlers are registered once per connection — not re-registered on re-renders.
 */
export function useSignalR(accessToken: string | undefined) {
  const queryClient = useQueryClient();
  const setStatus = useRealtimeStore((s) => s.setStatus);
  const setLastEvent = useRealtimeStore((s) => s.setLastEvent);

  // Refs to avoid stale closures in event handlers without adding to effect deps
  const queryClientRef = useRef<QueryClient>(queryClient);
  const setLastEventRef = useRef(setLastEvent);

  useLayoutEffect(() => {
    queryClientRef.current = queryClient;
  });

  useLayoutEffect(() => {
    setLastEventRef.current = setLastEvent;
  });

  useEffect(() => {
    if (!accessToken) {
      setStatus("disconnected");
      return;
    }

    let cancelled = false;
    const connection = getSignalRConnection(accessToken);

    // Register lifecycle handlers
    connection.onreconnecting(() => {
      if (!cancelled) setStatus("reconnecting");
    });
    connection.onreconnected(() => {
      if (!cancelled) setStatus("connected");
    });
    connection.onclose(() => {
      if (!cancelled) setStatus("disconnected");
    });

    // Register event handlers — use refs to always read latest queryClient
    connection.on("PatientCreated", () => {
      setLastEventRef.current("PatientCreated");
      queryClientRef.current.invalidateQueries({ queryKey: queryKeys.patients.lists() });
    });

    connection.on("PatientUpdated", (payload) => {
      setLastEventRef.current("PatientUpdated");
      queryClientRef.current.invalidateQueries({ queryKey: queryKeys.patients.lists() });
      if (payload?.patientId) {
        queryClientRef.current.invalidateQueries({
          queryKey: queryKeys.patients.detail(payload.patientId),
        });
      }
    });

    connection.on("PatientDeactivated", (payload) => {
      setLastEventRef.current("PatientDeactivated");
      queryClientRef.current.invalidateQueries({ queryKey: queryKeys.patients.lists() });
      if (payload?.patientId) {
        queryClientRef.current.invalidateQueries({
          queryKey: queryKeys.patients.detail(payload.patientId),
        });
      }
    });

    connection.on("EntityInvalidated", (payload) => {
      setLastEventRef.current("EntityInvalidated");
      if (payload?.entityType === "Patient" && payload?.entityId) {
        queryClientRef.current.invalidateQueries({
          queryKey: queryKeys.patients.detail(payload.entityId),
        });
      }
    });

    // Connect
    async function start() {
      setStatus("connecting");
      try {
        if (connection.state === HubConnectionState.Disconnected) {
          await connection.start();
          if (!cancelled) setStatus("connected");
        }
      } catch (err) {
        console.error("SignalR connection failed:", err);
        if (!cancelled) setStatus("disconnected");
      }
    }

    start();

    return () => {
      cancelled = true;
      disconnectSignalR();
      setStatus("disconnected");
    };
  }, [accessToken, setStatus]); // Only reconnect when token changes
}
