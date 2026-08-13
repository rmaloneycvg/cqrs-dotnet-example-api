"use client";

import { useRealtimeStore } from "@/stores/realtime.store";

export function RealtimeIndicator() {
  const status = useRealtimeStore((s) => s.status);

  const statusConfig = {
    connected: { label: "Live", className: "bg-green-500" },
    connecting: { label: "Connecting...", className: "bg-yellow-500 animate-pulse" },
    reconnecting: { label: "Reconnecting...", className: "bg-yellow-500 animate-pulse" },
    disconnected: { label: "Offline", className: "bg-red-500" },
  };

  const config = statusConfig[status];

  return (
    <div className="flex items-center gap-2 text-sm">
      <span
        className={`h-2 w-2 rounded-full ${config.className}`}
        aria-hidden="true"
      />
      <span className="text-gray-600 dark:text-gray-400">{config.label}</span>
    </div>
  );
}
