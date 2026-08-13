import { create } from "zustand";

type ConnectionStatus = "disconnected" | "connecting" | "connected" | "reconnecting";

interface RealtimeState {
  status: ConnectionStatus;
  lastEvent: { type: string; timestamp: number } | null;
  setStatus: (status: ConnectionStatus) => void;
  setLastEvent: (type: string) => void;
}

export const useRealtimeStore = create<RealtimeState>((set) => ({
  status: "disconnected",
  lastEvent: null,
  setStatus: (status) => set({ status }),
  setLastEvent: (type) => set({ lastEvent: { type, timestamp: Date.now() } }),
}));
