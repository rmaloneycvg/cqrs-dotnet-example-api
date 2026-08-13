"use client";

import { useRouter } from "next/navigation";
import { useAuthStore } from "@/stores/auth.store";

const ROLE_COLORS: Record<string, string> = {
  admin: "bg-red-100 text-red-800",
  physician: "bg-blue-100 text-blue-800",
  clinician: "bg-green-100 text-green-800",
  nurse: "bg-teal-100 text-teal-800",
  readonly: "bg-gray-100 text-gray-800",
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
    <div className="flex items-center gap-3">
      <div className="text-right">
        <p className="text-sm font-medium text-gray-900">{user.name}</p>
        <span
          className={`inline-block rounded-full px-2 py-0.5 text-xs font-medium ${ROLE_COLORS[user.role] ?? "bg-gray-100 text-gray-800"}`}
        >
          {user.role}
        </span>
      </div>
      <div className="flex gap-1">
        <button
          onClick={handleSwitchRole}
          className="rounded-md px-2 py-1 text-xs text-gray-600 hover:bg-gray-100 transition-colors"
          title="Switch role"
        >
          Switch
        </button>
        <button
          onClick={handleLogout}
          className="rounded-md px-2 py-1 text-xs text-gray-600 hover:bg-gray-100 transition-colors"
          title="Logout"
        >
          Logout
        </button>
      </div>
    </div>
  );
}
