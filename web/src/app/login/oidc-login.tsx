"use client";

import { useEffect, useCallback } from "react";
import { useRouter, useSearchParams } from "next/navigation";
import { useAuthStore } from "@/stores/auth.store";
import { Card, CardContent } from "@/components/ui/card";
import { Button } from "@/components/ui/button";

const OAUTH2_URL = process.env.NEXT_PUBLIC_OAUTH2_URL ?? "";
const CLIENT_ID = "clinic-web";
const REDIRECT_URI =
  typeof window !== "undefined"
    ? `${window.location.origin}/login?callback=true`
    : "";

/**
 * Production OIDC login page.
 * Uses Authorization Code + PKCE flow.
 */
export default function OidcLogin() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const isAuthenticated = useAuthStore((s) => s.isAuthenticated);
  const loginWithToken = useAuthStore((s) => s.loginWithToken);

  const isCallback = searchParams.get("callback") === "true";
  const code = searchParams.get("code");
  const error = searchParams.get("error");

  const handleCallback = useCallback(async () => {
    if (!code) return;

    try {
      const codeVerifier = sessionStorage.getItem("pkce_code_verifier");
      if (!codeVerifier) {
        throw new Error("Missing PKCE code verifier");
      }

      const res = await fetch(`${OAUTH2_URL}/token`, {
        method: "POST",
        headers: { "Content-Type": "application/x-www-form-urlencoded" },
        body: new URLSearchParams({
          grant_type: "authorization_code",
          code,
          redirect_uri: REDIRECT_URI,
          client_id: CLIENT_ID,
          code_verifier: codeVerifier,
        }),
      });

      if (!res.ok) {
        const err = await res.json().catch(() => ({}));
        throw new Error(err.error_description ?? "Token exchange failed");
      }

      const data = await res.json();
      sessionStorage.removeItem("pkce_code_verifier");

      const payload = JSON.parse(atob(data.access_token.split(".")[1]));

      await loginWithToken({
        accessToken: data.access_token,
        userId: payload.sub,
        name: payload.name ?? payload.sub,
        role: payload.role ?? "readonly",
        orgId: payload.org_id ?? "",
      });

      router.replace("/patients");
    } catch (err) {
      console.error("OIDC callback failed:", err);
      router.replace("/login?error=callback_failed");
    }
  }, [code, loginWithToken, router]);

  useEffect(() => {
    if (isCallback && code) {
      handleCallback();
    }
  }, [isCallback, code, handleCallback]);

  if (isAuthenticated && !isCallback) {
    router.replace("/patients");
    return null;
  }

  if (isCallback && code) {
    return (
      <div className="flex min-h-screen items-center justify-center">
        <div className="text-center">
          <div className="inline-block h-8 w-8 animate-spin rounded-full border-4 border-muted border-t-primary" />
          <p className="mt-4 text-muted-foreground">Completing sign in...</p>
        </div>
      </div>
    );
  }

  return (
    <div className="flex min-h-screen">
      {/* Left panel — branding (matches dev login) */}
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
            Secure access to patient records, appointments, and clinical workflows.
            Protected by HIPAA-compliant authentication.
          </p>
        </div>
        <div className="text-xs text-sidebar-foreground/40">
          <p>Protected Health Information</p>
          <p className="mt-1">Access is logged and audited</p>
        </div>
      </div>

      {/* Right panel — SSO button */}
      <div className="flex-1 flex flex-col items-center justify-center p-6 sm:p-10">
        <div className="w-full max-w-sm">
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

          <div className="mb-8 text-center lg:text-left">
            <h2 className="text-2xl font-bold">Welcome back</h2>
            <p className="text-muted-foreground mt-1">
              Sign in with your organization credentials
            </p>
          </div>

          {error && (
            <div className="mb-4 rounded-lg border border-destructive/50 bg-destructive/10 p-3 text-sm text-destructive">
              Sign in failed. Please try again or contact your administrator.
            </div>
          )}

          <Card>
            <CardContent className="pt-6">
              <Button
                onClick={initiateOidcLogin}
                className="w-full h-11"
                size="lg"
              >
                <svg className="h-5 w-5 mr-2" fill="none" viewBox="0 0 24 24" strokeWidth={1.5} stroke="currentColor">
                  <path strokeLinecap="round" strokeLinejoin="round" d="M16.5 10.5V6.75a4.5 4.5 0 1 0-9 0v3.75m-.75 11.25h10.5a2.25 2.25 0 0 0 2.25-2.25v-6.75a2.25 2.25 0 0 0-2.25-2.25H6.75a2.25 2.25 0 0 0-2.25 2.25v6.75a2.25 2.25 0 0 0 2.25 2.25Z" />
                </svg>
                Sign in with SSO
              </Button>
              <p className="mt-4 text-center text-xs text-muted-foreground">
                You will be redirected to your identity provider
              </p>
            </CardContent>
          </Card>
        </div>
      </div>
    </div>
  );
}

async function initiateOidcLogin() {
  const codeVerifier = generateCodeVerifier();
  const codeChallenge = await generateCodeChallenge(codeVerifier);
  sessionStorage.setItem("pkce_code_verifier", codeVerifier);

  const params = new URLSearchParams({
    response_type: "code",
    client_id: CLIENT_ID,
    redirect_uri: REDIRECT_URI,
    scope: "openid profile",
    code_challenge: codeChallenge,
    code_challenge_method: "S256",
    state: crypto.randomUUID(),
  });

  window.location.href = `${OAUTH2_URL}/authorize?${params.toString()}`;
}

function generateCodeVerifier(): string {
  const array = new Uint8Array(32);
  crypto.getRandomValues(array);
  return base64UrlEncode(array);
}

async function generateCodeChallenge(verifier: string): Promise<string> {
  const encoder = new TextEncoder();
  const data = encoder.encode(verifier);
  const digest = await crypto.subtle.digest("SHA-256", data);
  return base64UrlEncode(new Uint8Array(digest));
}

function base64UrlEncode(buffer: Uint8Array): string {
  let str = "";
  buffer.forEach((byte) => (str += String.fromCharCode(byte)));
  return btoa(str).replace(/\+/g, "-").replace(/\//g, "_").replace(/=+$/, "");
}
