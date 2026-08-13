"use client";

import { useRouter } from "next/navigation";
import { useEffect, useState } from "react";
import { useAuthStore, type UserRole } from "@/stores/auth.store";

/**
 * DEV ONLY — This component is tree-shaken from production builds.
 * Provides quick role switching without a real IdP.
 */

interface RoleOption {
  role: UserRole;
  label: string;
  description: string;
  permissions: string[];
  color: string;
}

const ROLE_OPTIONS: RoleOption[] = [
  {
    role: "admin",
    label: "Admin",
    description: "Full system access — all operations, batch processing, user management",
    permissions: ["PHI Read", "PHI Write", "Batch Operations", "Admin Panel"],
    color: "bg-red-600 hover:bg-red-700",
  },
  {
    role: "physician",
    label: "Physician",
    description: "Clinical access — view and modify patient records, prescriptions",
    permissions: ["PHI Read", "PHI Write", "Prescriptions"],
    color: "bg-blue-600 hover:bg-blue-700",
  },
  {
    role: "clinician",
    label: "Clinician",
    description: "Standard clinical staff — patient care workflows",
    permissions: ["PHI Read", "PHI Write"],
    color: "bg-green-600 hover:bg-green-700",
  },
  {
    role: "nurse",
    label: "Nurse",
    description: "Nursing staff — patient vitals, notes, care coordination",
    permissions: ["PHI Read", "PHI Write"],
    color: "bg-teal-600 hover:bg-teal-700",
  },
  {
    role: "readonly",
    label: "Read-Only (Intern)",
    description: "Observation only — cannot modify patient data",
    permissions: ["PHI Read"],
    color: "bg-gray-600 hover:bg-gray-700",
  },
];

export default function DevLogin() {
  const router = useRouter();
  const login = useAuthStore((s) => s.login);
  const isAuthenticated = useAuthStore((s) => s.isAuthenticated);
  const [isLoggingIn, setIsLoggingIn] = useState(false);

  useEffect(() => {
    if (isAuthenticated) {
      router.replace("/patients");
    }
  }, [isAuthenticated, router]);

  async function handleLogin(role: UserRole) {
    setIsLoggingIn(true);
    await login(role);
    router.push("/patients");
  }

  if (isAuthenticated) return null;

  return (
    <div className="flex min-h-screen flex-col items-center justify-center p-8 bg-gray-50">
      <div className="w-full max-w-2xl">
        <div className="text-center mb-8">
          <h1 className="text-3xl font-bold text-gray-900">Clinic Dashboard</h1>
          <p className="mt-2 text-gray-600">
            Select a role to sign in as a test user
          </p>
          <div className="mt-2 inline-block rounded-full bg-amber-100 px-3 py-1 text-xs font-medium text-amber-800">
            ⚠️ Development Mode — Not for production use
          </div>
        </div>

        <div className="space-y-3">
          {ROLE_OPTIONS.map((option) => (
            <button
              key={option.role}
              onClick={() => handleLogin(option.role)}
              className={`w-full rounded-lg p-4 text-left text-white transition-all ${option.color} shadow-sm hover:shadow-md`}
            >
              <div className="flex items-center justify-between">
                <div>
                  <h3 className="font-semibold text-lg">{option.label}</h3>
                  <p className="text-sm text-white/80 mt-0.5">
                    {option.description}
                  </p>
                </div>
                <svg
                  className="h-5 w-5 flex-shrink-0 opacity-60"
                  fill="none"
                  viewBox="0 0 24 24"
                  strokeWidth={2}
                  stroke="currentColor"
                >
                  <path
                    strokeLinecap="round"
                    strokeLinejoin="round"
                    d="M13.5 4.5L21 12m0 0l-7.5 7.5M21 12H3"
                  />
                </svg>
              </div>
              <div className="mt-2 flex flex-wrap gap-1.5">
                {option.permissions.map((perm) => (
                  <span
                    key={perm}
                    className="inline-block rounded-full bg-white/20 px-2 py-0.5 text-xs"
                  >
                    {perm}
                  </span>
                ))}
              </div>
            </button>
          ))}
        </div>

        <div className="mt-6 rounded-lg border border-gray-200 bg-white p-4 text-sm text-gray-500">
          <p className="font-medium text-gray-700 mb-1">How it works:</p>
          <ul className="list-disc pl-5 space-y-1">
            <li>Each role maps to JWT claims sent with every API request</li>
            <li>The API&apos;s authorization policies enforce access based on the <code className="text-xs bg-gray-100 px-1 rounded">role</code> claim</li>
            <li>SignalR connections use the same token for WebSocket auth</li>
            <li>Switch roles anytime by clicking &quot;Switch&quot; in the header</li>
          </ul>
        </div>
      </div>
    </div>
  );
}
