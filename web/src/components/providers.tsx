"use client";

import { QueryClientProvider } from "@tanstack/react-query";
import { getQueryClient } from "@/lib/query-client";
import { useSignalR } from "@/hooks/use-signalr";
import { useAuthStore } from "@/stores/auth.store";

function SignalRProvider({ children }: { children: React.ReactNode }) {
  const accessToken = useAuthStore((s) => s.user?.accessToken);
  useSignalR(accessToken);
  return <>{children}</>;
}

export function Providers({ children }: { children: React.ReactNode }) {
  const queryClient = getQueryClient();

  return (
    <QueryClientProvider client={queryClient}>
      <SignalRProvider>{children}</SignalRProvider>
    </QueryClientProvider>
  );
}
