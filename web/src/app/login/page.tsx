"use client";

import dynamic from "next/dynamic";

/**
 * Login page that conditionally loads dev or OIDC login based on AUTH_MODE.
 *
 * NEXT_PUBLIC_* env vars are inlined at BUILD time by Next.js. This means:
 * - When built with NEXT_PUBLIC_AUTH_MODE=oidc (staging/prod), the dev-login
 *   chunk is never generated — it's dead code eliminated by the bundler.
 * - When built with NEXT_PUBLIC_AUTH_MODE=dev, the OIDC login is excluded.
 *
 * This guarantees the dev role-picker is never shipped in production bundles.
 */

function LoginSkeleton() {
  return (
    <div className="flex min-h-screen items-center justify-center">
      <div className="animate-pulse text-gray-400">Loading...</div>
    </div>
  );
}

// Build-time constant — Next.js inlines NEXT_PUBLIC_* at compile time
const AUTH_MODE = process.env.NEXT_PUBLIC_AUTH_MODE;

// Conditional dynamic imports — bundler eliminates the dead branch
const LoginComponent =
  AUTH_MODE === "dev"
    ? dynamic(() => import("./dev-login"), { loading: () => <LoginSkeleton /> })
    : dynamic(() => import("./oidc-login"), { loading: () => <LoginSkeleton /> });

export default function LoginPage() {
  return <LoginComponent />;
}
