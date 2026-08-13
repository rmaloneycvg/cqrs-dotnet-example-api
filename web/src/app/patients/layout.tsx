"use client";

import { AuthGuard } from "@/components/auth-guard";
import { UserBadge } from "@/components/user-badge";

export default function PatientsLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return (
    <AuthGuard>
      <div className="min-h-screen">
        <header className="border-b bg-white px-6 py-3 flex items-center justify-between">
          <div className="flex items-center gap-4">
            <h1 className="text-lg font-semibold text-gray-900">
              Clinic Dashboard
            </h1>
            <nav className="flex gap-2 text-sm">
              <a
                href="/patients"
                className="rounded-md px-3 py-1 text-gray-700 hover:bg-gray-100"
              >
                Patients
              </a>
            </nav>
          </div>
          <UserBadge />
        </header>
        <main className="p-6">{children}</main>
      </div>
    </AuthGuard>
  );
}
