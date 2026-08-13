"use client";

import { useEffect, useRef, useCallback } from "react";
import { HubConnectionState } from "@microsoft/signalr";
import { useQueryClient } from "@tanstack/react-query";
import { getSignalRConnection, disconnectSignalR } from "@/lib/signalr";
import { queryKeys } from "@/lib/query-keys";
import { useRealtimeStore } from "@/stores/realtime.store";

/**
 * Manages the SignalR connection lifecycle.
 * Connects on mount, disconnects on unmount.
 * Registers event handlers that invalidate TanStack Query cache on push events.
 */
export function useSignalR(accessToken?: string) {
  const queryClient = useQueryClient();
  const setStatus = useRealtimeStore((s) => s.setStatus);
  const setLastEvent = useRealtimeStore((s) => s.setLastEvent);
  const connectionRef = useRef<ReturnType<typeof getSignalRConnection> | null>(null);

  const connect = useCallback(async () => {
    if (!accessToken) return;

    setStatus("connecting");
    const connection = getSignalRConnection(accessToken);
    connectionRef.current = connection;

    // Register reconnection handlers
    connection.onreconnecting(() => setStatus("reconnecting"));
    connection.onreconnected(() => setStatus("connected"));
    connection.onclose(() => setStatus("disconnected"));

    // Register event handlers
    connection.on("PatientCreated", (payload) => {
      setLastEvent("PatientCreated");
      queryClient.invalidateQueries({ queryKey: queryKeys.patients.lists() });
    });

    connection.on("PatientUpdated", (payload) => {
      setLastEvent("PatientUpdated");
      // Invalidate both the list and the specific detail if we have the ID
      queryClient.invalidateQueries({ queryKey: queryKeys.patients.lists() });
      if (payload?.patientId) {
        queryClient.invalidateQueries({
          queryKey: queryKeys.patients.detail(payload.patientId),
        });
      }
    });

    connection.on("PatientDeactivated", (payload) => {
      setLastEvent("PatientDeactivated");
      queryClient.invalidateQueries({ queryKey: queryKeys.patients.lists() });
      if (payload?.patientId) {
        queryClient.invalidateQueries({
          queryKey: queryKeys.patients.detail(payload.patientId),
        });
      }
    });

    connection.on("EntityInvalidated", (payload) => {
      setLastEvent("EntityInvalidated");
      if (payload?.entityType === "Patient" && payload?.entityId) {
        queryClient.invalidateQueries({
          queryKey: queryKeys.patients.detail(payload.entityId),
        });
      }
    });

    // Connect
    try {
      if (connection.state === HubConnectionState.Disconnected) {
        await connection.start();
        setStatus("connected");
      }
    } catch (err) {
      console.error("SignalR connection failed:", err);
      setStatus("disconnected");
    }
  }, [accessToken, queryClient, setStatus, setLastEvent]);

  useEffect(() => {
    connect();
    return () => {
      disconnectSignalR();
      setStatus("disconnected");
    };
  }, [connect, setStatus]);

  return connectionRef;
}
