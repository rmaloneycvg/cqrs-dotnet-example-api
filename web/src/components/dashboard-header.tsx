"use client";

import { RealtimeIndicator } from "@/components/features/realtime-indicator";
import { UserBadge } from "@/components/user-badge";

interface DashboardHeaderProps {
  title: string;
  description?: string;
  children?: React.ReactNode;
}

export function DashboardHeader({ title, description, children }: DashboardHeaderProps) {
  return (
    <header className="sticky top-0 z-20 border-b bg-card/80 backdrop-blur-sm">
      <div className="flex h-16 items-center justify-between px-4 sm:px-6 lg:px-8">
        {/* Left: Title + description */}
        <div className="flex items-center gap-4 min-w-0">
          {/* Spacer for mobile hamburger */}
          <div className="w-8 lg:hidden" aria-hidden="true" />
          <div className="min-w-0">
            <h1 className="text-lg font-semibold truncate">{title}</h1>
            {description && (
              <p className="text-sm text-muted-foreground truncate hidden sm:block">
                {description}
              </p>
            )}
          </div>
        </div>

        {/* Right: Actions + user */}
        <div className="flex items-center gap-3 sm:gap-4">
          {children}
          <RealtimeIndicator />
          <div className="hidden sm:block h-6 w-px bg-border" aria-hidden="true" />
          <UserBadge />
        </div>
      </div>
    </header>
  );
}
