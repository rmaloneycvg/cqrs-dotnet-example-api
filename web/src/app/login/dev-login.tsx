"use client";

import { useRouter } from "next/navigation";
import { useEffect, useState } from "react";
import { useAuthStore, type UserRole } from "@/stores/auth.store";
import { Card, CardContent } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { cn } from "@/lib/utils";

/**
 * DEV ONLY — This component is tree-shaken from production builds.
 * Provides quick role switching without a real IdP.
 */

interface RoleOption {
  role: UserRole;
  label: string;
  description: string;
  permissions: string[];
  iconColor: string;
}

const ROLE_OPTIONS: RoleOption[] = [
  {
    role: "admin",
    label: "Administrator",
    description: "Full system access — all operations, batch processing, user management",
    permissions: ["PHI Read", "PHI Write", "Batch Ops", "Admin"],
    iconColor: "bg-red-500",
  },
  {
    role: "physician",
    label: "Physician",
    description: "Clinical access — view and modify patient records, prescriptions",
    permissions: ["PHI Read", "PHI Write", "Prescriptions"],
    iconColor: "bg-blue-500",
  },
  {
    role: "clinician",
    label: "Clinician",
    description: "Standard clinical staff — patient care workflows",
    permissions: ["PHI Read", "PHI Write"],
    iconColor: "bg-emerald-500",
  },
  {
    role: "nurse",
    label: "Nurse",
    description: "Nursing staff — patient vitals, notes, care coordination",
    permissions: ["PHI Read", "PHI Write"],
    iconColor: "bg-teal-500",
  },
  {
    role: "readonly",
    label: "Read-Only",
    description: "Observation only — cannot modify patient data",
    permissions: ["PHI Read"],
    iconColor: "bg-gray-400",
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
    <div className="flex min-h-screen">
      {/* Left panel — branding */}
      <div className="hidden lg:flex lg:w-[480px] bg-sidebar text-sidebar-foreground flex-col justify-between p-10">
        <div>
          <div className="flex items-center gap-3">
            <div className="flex h-10 w-10 items-center justify-center rounded-xl bg-primary">
              <svg className="h-6 w-6 text-primary-foreground" fill="none" viewBox="0 0 24 24" strokeWidth={2} stroke="currentColor">
                <path strokeLinecap="round" strokeLinejoin="round" d="M12 4.5v15m7.5-7.5h-15" />
              </svg>
            </div>
            <div>
              <h1 className="text-xl font-bold text-white">Clinic Dashboard</h1>
              <p className="text-sm text-sidebar-foreground/60">Healthcare Platform</p>
            </div>
          </div>
          <p className="mt-8 text-sm text-sidebar-foreground/70 leading-relaxed max-w-sm">
            Real-time patient management with CQRS architecture, event sourcing, and
            HIPAA-compliant data handling.
          </p>
        </div>
        <div className="text-xs text-sidebar-foreground/40">
          <p>Development Environment</p>
          <p className="mt-1">CQRS .NET Example API + Next.js</p>
        </div>
      </div>

      {/* Right panel — role selection */}
      <div className="flex-1 flex flex-col items-center justify-center p-6 sm:p-10">
        <div className="w-full max-w-lg">
          {/* Mobile branding */}
          <div className="lg:hidden text-center mb-8">
            <div className="flex items-center justify-center gap-3 mb-3">
              <div className="flex h-10 w-10 items-center justify-center rounded-xl bg-primary">
                <svg className="h-6 w-6 text-primary-foreground" fill="none" viewBox="0 0 24 24" strokeWidth={2} stroke="currentColor">
                  <path strokeLinecap="round" strokeLinejoin="round" d="M12 4.5v15m7.5-7.5h-15" />
                </svg>
              </div>
              <h1 className="text-xl font-bold">Clinic Dashboard</h1>
            </div>
          </div>

          <div className="mb-6">
            <h2 className="text-2xl font-bold">Select a role</h2>
            <p className="text-muted-foreground mt-1">Choose a test user to sign in</p>
          </div>

          <Badge variant="warning" className="mb-6">
            ⚠️ Development Mode
          </Badge>

          <div className="space-y-3">
            {ROLE_OPTIONS.map((option) => (
              <Card
                key={option.role}
                className={cn(
                  "cursor-pointer transition-all hover:shadow-md hover:border-primary/30 active:scale-[0.99]",
                  isLoggingIn && "pointer-events-none opacity-50"
                )}
                onClick={() => handleLogin(option.role)}
              >
                <CardContent className="flex items-center gap-4 py-4 px-5">
                  {/* Role icon */}
                  <div className={cn("flex h-10 w-10 shrink-0 items-center justify-center rounded-lg text-white", option.iconColor)}>
                    <svg className="h-5 w-5" fill="none" viewBox="0 0 24 24" strokeWidth={1.5} stroke="currentColor">
                      <path strokeLinecap="round" strokeLinejoin="round" d="M15.75 6a3.75 3.75 0 1 1-7.5 0 3.75 3.75 0 0 1 7.5 0ZM4.501 20.118a7.5 7.5 0 0 1 14.998 0A17.933 17.933 0 0 1 12 21.75c-2.676 0-5.216-.584-7.499-1.632Z" />
                    </svg>
                  </div>

                  {/* Content */}
                  <div className="flex-1 min-w-0">
                    <div className="flex items-center justify-between">
                      <h3 className="font-semibold text-sm">{option.label}</h3>
                      <svg className="h-4 w-4 text-muted-foreground shrink-0" fill="none" viewBox="0 0 24 24" strokeWidth={2} stroke="currentColor">
                        <path strokeLinecap="round" strokeLinejoin="round" d="m8.25 4.5 7.5 7.5-7.5 7.5" />
                      </svg>
                    </div>
                    <p className="text-xs text-muted-foreground mt-0.5 truncate">
                      {option.description}
                    </p>
                    <div className="flex flex-wrap gap-1 mt-2">
                      {option.permissions.map((perm) => (
                        <span
                          key={perm}
                          className="inline-block rounded bg-muted px-1.5 py-0.5 text-[10px] font-medium text-muted-foreground"
                        >
                          {perm}
                        </span>
                      ))}
                    </div>
                  </div>
                </CardContent>
              </Card>
            ))}
          </div>

          <Card className="mt-6 bg-muted/50 border-muted">
            <CardContent className="py-3 px-4">
              <p className="text-xs text-muted-foreground leading-relaxed">
                Each role maps to JWT claims sent with every API request.
                The API authorization policies enforce access based on the <code className="rounded bg-background px-1 text-[10px] font-mono">role</code> claim.
                Switch roles anytime from the header.
              </p>
            </CardContent>
          </Card>
        </div>
      </div>
    </div>
  );
}
