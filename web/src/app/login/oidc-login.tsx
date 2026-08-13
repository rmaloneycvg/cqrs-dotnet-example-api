"use client";

import { useEffect, useCallback } from "react";
import { useRouter, useSearchParams } from "next/navigation";
import { useAuthStore } from "@/stores/auth.store";

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

  // Handle OAuth callback
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

      // Decode JWT payload to extract claims
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
      // Clear callback params and show error
      router.replace("/login?error=callback_failed");
    }
  }, [code, loginWithToken, router]);

  useEffect(() => {
    if (isCallback && code) {
      handleCallback();
    }
  }, [isCallback, code, handleCallback]);

  // Already authenticated — redirect
  if (isAuthenticated && !isCallback) {
    router.replace("/patients");
    return null;
  }

  // Processing callback
  if (isCallback && code) {
    return (
      <div className="flex min-h-screen items-center justify-center">
        <div className="text-center">
          <div className="inline-block h-8 w-8 animate-spin rounded-full border-4 border-gray-300 border-t-blue-600" />
          <p className="mt-4 text-gray-600">Completing sign in...</p>
        </div>
      </div>
    );
  }

  return (
    <div className="flex min-h-screen flex-col items-center justify-center p-8 bg-gray-50">
      <div className="w-full max-w-md">
        <div className="text-center mb-8">
          <h1 className="text-3xl font-bold text-gray-900">Clinic Dashboard</h1>
          <p className="mt-2 text-gray-600">
            Sign in with your organization credentials
          </p>
        </div>

        {error && (
          <div className="mb-4 rounded-lg border border-red-200 bg-red-50 p-4 text-sm text-red-700">
            Sign in failed. Please try again or contact your administrator.
          </div>
        )}

        <div className="rounded-lg border border-gray-200 bg-white p-6 shadow-sm">
          <button
            onClick={initiateOidcLogin}
            className="w-full rounded-lg bg-blue-600 px-4 py-3 text-center font-medium text-white hover:bg-blue-700 transition-colors"
          >
            Sign in with SSO
          </button>

          <p className="mt-4 text-center text-xs text-gray-400">
            You will be redirected to your identity provider
          </p>
        </div>

        <p className="mt-6 text-center text-xs text-gray-400">
          Protected by HIPAA-compliant authentication
        </p>
      </div>
    </div>
  );
}

/**
 * Initiates OIDC Authorization Code + PKCE flow.
 */
async function initiateOidcLogin() {
  // Generate PKCE code verifier and challenge
  const codeVerifier = generateCodeVerifier();
  const codeChallenge = await generateCodeChallenge(codeVerifier);

  // Store verifier for callback
  sessionStorage.setItem("pkce_code_verifier", codeVerifier);

  // Build authorization URL
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
