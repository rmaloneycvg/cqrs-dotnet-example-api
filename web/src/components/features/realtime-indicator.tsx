"use client";

import { useRealtimeStore } from "@/stores/realtime.store";
import { cn } from "@/lib/utils";

export function RealtimeIndicator() {
  const status = useRealtimeStore((s) => s.status);

  const statusConfig = {
    connected: { label: "Live", dotClass: "bg-green-500" },
    connecting: { label: "Connecting...", dotClass: "bg-yellow-500 animate-pulse" },
    reconnecting: { label: "Reconnecting...", dotClass: "bg-yellow-500 animate-pulse" },
    disconnected: { label: "Offline", dotClass: "bg-destructive" },
  };

  const config = statusConfig[status];

  return (
    <div className="flex items-center gap-2 text-sm" aria-live="polite">
      <span
        className={cn("h-2 w-2 rounded-full", config.dotClass)}
        aria-hidden="true"
      />
      <span className="text-muted-foreground">{config.label}</span>
    </div>
  );
}
