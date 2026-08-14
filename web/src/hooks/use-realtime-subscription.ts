"use client";

import { useEffect } from "react";
import { HubConnectionState } from "@microsoft/signalr";
import { getSignalRConnection } from "@/lib/signalr";
import { useRealtimeStore } from "@/stores/realtime.store";

/**
 * Subscribe to a specific entity when the component mounts AND connection is active.
 * Unsubscribes on unmount or entity change.
 *
 * Listens to connection status from the realtime store so subscriptions
 * are established even if the component mounts before SignalR connects.
 *
 * Usage:
 *   useRealtimeSubscription("Patient", patientId);
 */
export function useRealtimeSubscription(
  entityType: string,
  entityId: string | undefined
) {
  const connectionStatus = useRealtimeStore((s) => s.status);

  useEffect(() => {
    if (!entityId || connectionStatus !== "connected") return;

    const connection = getSignalRConnection();
    if (connection.state !== HubConnectionState.Connected) return;

    // Subscribe
    if (entityType === "Patient") {
      connection.invoke("SubscribeToPatient", entityId).catch(console.error);
    } else {
      connection.invoke("SubscribeToEntity", entityType, entityId).catch(console.error);
    }

    // Cleanup: unsubscribe on unmount or entity/connection change
    return () => {
      if (connection.state !== HubConnectionState.Connected) return;

      if (entityType === "Patient") {
        connection.invoke("UnsubscribeFromPatient", entityId).catch(console.error);
      } else {
        connection.invoke("UnsubscribeFromEntity", entityType, entityId).catch(console.error);
      }
    };
  }, [entityType, entityId, connectionStatus]);
}
