"use client";

import { useEffect } from "react";
import { HubConnectionState } from "@microsoft/signalr";
import { getSignalRConnection } from "@/lib/signalr";

/**
 * Subscribe to a specific entity when the component mounts.
 * Unsubscribes when the component unmounts or the entity ID changes.
 *
 * Usage:
 *   useRealtimeSubscription("Patient", patientId);
 */
export function useRealtimeSubscription(
  entityType: string,
  entityId: string | undefined
) {
  useEffect(() => {
    if (!entityId) return;

    const connection = getSignalRConnection();
    if (connection.state !== HubConnectionState.Connected) return;

    // Subscribe
    if (entityType === "Patient") {
      connection.invoke("SubscribeToPatient", entityId).catch(console.error);
    } else {
      connection.invoke("SubscribeToEntity", entityType, entityId).catch(console.error);
    }

    // Cleanup: unsubscribe on unmount or entity change
    return () => {
      if (connection.state !== HubConnectionState.Connected) return;

      if (entityType === "Patient") {
        connection.invoke("UnsubscribeFromPatient", entityId).catch(console.error);
      } else {
        connection.invoke("UnsubscribeFromEntity", entityType, entityId).catch(console.error);
      }
    };
  }, [entityType, entityId]);
}
