"use client";

import { useRouter } from "next/navigation";
import { useEffect, useState } from "react";
import { useAuthStore } from "@/stores/auth.store";
import { Skeleton } from "@/components/ui/skeleton";

/**
 * Guards authenticated routes. Waits for Zustand persist hydration
 * before rendering children or redirecting — prevents the "flash"
 * caused by async localStorage rehydration.
 */
export function AuthGuard({ children }: { children: React.ReactNode }) {
  const router = useRouter();
  const isAuthenticated = useAuthStore((s) => s.isAuthenticated);
  const [hydrated, setHydrated] = useState(false);

  useEffect(() => {
    // Check if already hydrated (instant on subsequent renders)
    if (useAuthStore.persist.getOptions().skipHydration) {
      setHydrated(true);
      return;
    }

    // Wait for persist rehydration from localStorage
    const unsub = useAuthStore.persist.onFinishHydration(() => {
      setHydrated(true);
    });

    // If already hydrated by the time this runs
    if (useAuthStore.persist.hasHydrated()) {
      setHydrated(true);
    }

    return unsub;
  }, []);

  useEffect(() => {
    if (hydrated && !isAuthenticated) {
      router.replace("/login");
    }
  }, [hydrated, isAuthenticated, router]);

  // Still hydrating — show shell to prevent flash
  if (!hydrated) {
    return (
      <div className="min-h-screen flex">
        {/* Sidebar placeholder */}
        <div className="hidden lg:block w-64 bg-sidebar shrink-0" />
        <div className="flex-1 p-6 space-y-4">
          <Skeleton className="h-16 w-full" />
          <Skeleton className="h-[calc(100vh-8rem)] w-full" />
        </div>
      </div>
    );
  }

  if (!isAuthenticated) return null;

  return <>{children}</>;
}
