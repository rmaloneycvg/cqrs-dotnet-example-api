"use client";

import { QueryClientProvider } from "@tanstack/react-query";
import { getQueryClient } from "@/lib/query-client";
import { useSignalR } from "@/hooks/use-signalr";
import { useAuthStore } from "@/stores/auth.store";

function SignalRConnection() {
  const accessToken = useAuthStore((s) => s.user?.accessToken);
  useSignalR(accessToken);
  return null;
}

export function Providers({ children }: { children: React.ReactNode }) {
  const queryClient = getQueryClient();

  return (
    <QueryClientProvider client={queryClient}>
      <SignalRConnection />
      {children}
    </QueryClientProvider>
  );
}
