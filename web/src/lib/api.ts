import { useAuthStore } from "@/stores/auth.store";

const API_BASE_URL = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5000";

interface RequestOptions {
  method?: string;
  body?: unknown;
  headers?: Record<string, string>;
  signal?: AbortSignal;
}

class ApiError extends Error {
  constructor(
    public status: number,
    public statusText: string,
    public body?: unknown
  ) {
    super(`API Error: ${status} ${statusText}`);
    this.name = "ApiError";
  }
}

function getAuthHeaders(): Record<string, string> {
  const user = useAuthStore.getState().user;
  if (!user) return {};

  return {
    Authorization: `Bearer ${user.accessToken}`,
    // Dev headers as fallback — API's DevAuthHandler reads these when no real JWT is present
    "X-Dev-Role": user.role,
    "X-Dev-User": user.userId,
    "X-Dev-Org": user.orgId,
  };
}

async function request<T>(path: string, options: RequestOptions = {}): Promise<T> {
  const { method = "GET", body, headers = {}, signal } = options;

  const res = await fetch(`${API_BASE_URL}${path}`, {
    method,
    headers: {
      "Content-Type": "application/json",
      ...getAuthHeaders(),
      ...headers,
    },
    body: body ? JSON.stringify(body) : undefined,
    signal,
  });

  if (!res.ok) {
    if (res.status === 401) {
      // Token expired or invalid — clear auth state
      useAuthStore.getState().logout();
    }
    const errorBody = await res.json().catch(() => null);
    throw new ApiError(res.status, res.statusText, errorBody);
  }

  if (res.status === 204) return undefined as T;
  return res.json();
}

export const api = {
  get: <T>(path: string, signal?: AbortSignal) =>
    request<T>(path, { signal }),

  post: <T>(path: string, body: unknown) =>
    request<T>(path, { method: "POST", body }),

  put: <T>(path: string, body: unknown) =>
    request<T>(path, { method: "PUT", body }),

  delete: <T>(path: string) =>
    request<T>(path, { method: "DELETE" }),
};

export { ApiError };
export { API_BASE_URL };
