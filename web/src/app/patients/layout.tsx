"use client";

import { AuthGuard } from "@/components/auth-guard";
import { Sidebar } from "@/components/sidebar";

export default function PatientsLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return (
    <AuthGuard>
      <div className="min-h-screen">
        <Sidebar />
        {/* Main content — offset for desktop sidebar */}
        <div className="lg:pl-64">
          {children}
        </div>
      </div>
    </AuthGuard>
  );
}
