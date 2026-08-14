"use client";

import { useRouter } from "next/navigation";
import { useAuthStore } from "@/stores/auth.store";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { cn } from "@/lib/utils";

const ROLE_VARIANT: Record<string, "destructive" | "default" | "success" | "secondary" | "outline"> = {
  admin: "destructive",
  physician: "default",
  clinician: "success",
  nurse: "success",
  readonly: "secondary",
};

const ROLE_INITIALS: Record<string, string> = {
  admin: "A",
  physician: "P",
  clinician: "C",
  nurse: "N",
  readonly: "R",
};

export function UserBadge() {
  const router = useRouter();
  const user = useAuthStore((s) => s.user);
  const logout = useAuthStore((s) => s.logout);

  if (!user) return null;

  function handleLogout() {
    logout();
    router.push("/login");
  }

  function handleSwitchRole() {
    logout();
    router.push("/login");
  }

  return (
    <div className="flex items-center gap-2">
      {/* Compact pill: avatar initial + name + role — all inline */}
      <div className="hidden sm:flex items-center gap-2 rounded-full bg-muted/60 pl-1 pr-3 py-1">
        <div className={cn(
          "flex h-6 w-6 items-center justify-center rounded-full text-[11px] font-bold text-white shrink-0",
          ROLE_VARIANT[user.role] === "destructive" ? "bg-red-500" :
          ROLE_VARIANT[user.role] === "success" ? "bg-emerald-500" :
          "bg-primary"
        )}>
          {ROLE_INITIALS[user.role] ?? "?"}
        </div>
        <span className="text-sm font-medium truncate max-w-[120px]">{user.name.split("(")[0].trim()}</span>
        <Badge variant={ROLE_VARIANT[user.role] ?? "secondary"} className="text-[10px] px-1.5 py-0 leading-tight">
          {user.role}
        </Badge>
      </div>

      {/* Mobile: just the avatar */}
      <div className={cn(
        "flex sm:hidden h-8 w-8 items-center justify-center rounded-full text-xs font-bold text-white shrink-0",
        ROLE_VARIANT[user.role] === "destructive" ? "bg-red-500" :
        ROLE_VARIANT[user.role] === "success" ? "bg-emerald-500" :
        "bg-primary"
      )}>
        {ROLE_INITIALS[user.role] ?? "?"}
      </div>

      {/* Actions */}
      <Button
        variant="ghost"
        size="icon"
        onClick={handleSwitchRole}
        title="Switch role"
        className="h-8 w-8 text-muted-foreground"
      >
        <svg className="h-4 w-4" fill="none" viewBox="0 0 24 24" strokeWidth={1.5} stroke="currentColor">
          <path strokeLinecap="round" strokeLinejoin="round" d="M7.5 21 3 16.5m0 0L7.5 12M3 16.5h13.5m0-13.5L21 7.5m0 0L16.5 12M21 7.5H7.5" />
        </svg>
      </Button>
      <Button
        variant="ghost"
        size="icon"
        onClick={handleLogout}
        title="Logout"
        className="h-8 w-8 text-muted-foreground"
      >
        <svg className="h-4 w-4" fill="none" viewBox="0 0 24 24" strokeWidth={1.5} stroke="currentColor">
          <path strokeLinecap="round" strokeLinejoin="round" d="M15.75 9V5.25A2.25 2.25 0 0 0 13.5 3h-6a2.25 2.25 0 0 0-2.25 2.25v13.5A2.25 2.25 0 0 0 7.5 21h6a2.25 2.25 0 0 0 2.25-2.25V15m3 0 3-3m0 0-3-3m3 3H9" />
        </svg>
      </Button>
    </div>
  );
}
