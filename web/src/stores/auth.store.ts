import { create } from "zustand";
import { persist } from "zustand/middleware";

export type UserRole = "admin" | "clinician" | "nurse" | "physician" | "readonly";

export interface AuthUser {
  userId: string;
  name: string;
  role: UserRole;
  orgId: string;
  accessToken: string;
}

interface AuthState {
  user: AuthUser | null;
  isAuthenticated: boolean;
  login: (role: UserRole) => Promise<void>;
  loginWithToken: (params: {
    accessToken: string;
    userId: string;
    name: string;
    role: string;
    orgId: string;
  }) => Promise<void>;
  logout: () => void;
}

const OAUTH2_URL = process.env.NEXT_PUBLIC_OAUTH2_URL ?? "http://localhost:9090/default";

/**
 * Role-based test users for development.
 * Each maps to a different set of claims that the API checks for authorization policies.
 */
const TEST_USERS: Record<UserRole, { name: string; userId: string; orgId: string }> = {
  admin: {
    name: "Dr. Admin (Full Access)",
    userId: "dev-admin",
    orgId: "00000000-0000-0000-0000-000000000001",
  },
  clinician: {
    name: "Dr. Smith (Clinician)",
    userId: "dev-clinician",
    orgId: "00000000-0000-0000-0000-000000000001",
  },
  nurse: {
    name: "Nurse Johnson (PHI Read/Write)",
    userId: "dev-nurse",
    orgId: "00000000-0000-0000-0000-000000000001",
  },
  physician: {
    name: "Dr. Williams (Physician)",
    userId: "dev-physician",
    orgId: "00000000-0000-0000-0000-000000000001",
  },
  readonly: {
    name: "Intern Davis (Read Only)",
    userId: "dev-readonly",
    orgId: "00000000-0000-0000-0000-000000000001",
  },
};

async function fetchToken(role: UserRole): Promise<string> {
  const user = TEST_USERS[role];

  try {
    // Request a token from mock-oauth2-server with role claims
    const res = await fetch(`${OAUTH2_URL}/token`, {
      method: "POST",
      headers: { "Content-Type": "application/x-www-form-urlencoded" },
      body: new URLSearchParams({
        grant_type: "client_credentials",
        client_id: "clinic-web",
        client_secret: "not-a-secret",
        scope: "openid",
      }),
    });

    if (res.ok) {
      const data = await res.json();
      return data.access_token;
    }
  } catch (err) {
    console.warn("Mock OAuth2 server not available, using fallback token:", err);
  }

  // Fallback: generate a minimal dev token (works because SignalR dev mode skips validation)
  // This is a base64-encoded JSON payload that the API's DevAuthHandler will accept
  const payload = btoa(
    JSON.stringify({
      sub: user.userId,
      name: user.name,
      role: role,
      org_id: user.orgId,
      exp: Math.floor(Date.now() / 1000) + 86400, // 24h from now
    })
  );
  return `dev.${payload}.dev`;
}

export const useAuthStore = create<AuthState>()(
  persist(
    (set) => ({
      user: null,
      isAuthenticated: false,

      login: async (role: UserRole) => {
        const testUser = TEST_USERS[role];
        const accessToken = await fetchToken(role);

        set({
          user: {
            userId: testUser.userId,
            name: testUser.name,
            role,
            orgId: testUser.orgId,
            accessToken,
          },
          isAuthenticated: true,
        });
      },

      loginWithToken: async ({ accessToken, userId, name, role, orgId }) => {
        set({
          user: {
            userId,
            name,
            role: (role as UserRole) ?? "readonly",
            orgId,
            accessToken,
          },
          isAuthenticated: true,
        });
      },

      logout: () => {
        set({ user: null, isAuthenticated: false });
      },
    }),
    {
      name: "clinic-auth",
    }
  )
);
