import { render, screen, waitFor } from "@testing-library/react";
import { describe, it, expect, beforeEach, vi } from "vitest";
import { useAuthStore } from "@/stores/auth.store";
import { AuthGuard } from "./auth-guard";

// Mock next/navigation
const mockReplace = vi.fn();
vi.mock("next/navigation", () => ({
  useRouter: () => ({
    replace: mockReplace,
    push: vi.fn(),
    back: vi.fn(),
    forward: vi.fn(),
    refresh: vi.fn(),
    prefetch: vi.fn(),
  }),
}));

describe("AuthGuard", () => {
  beforeEach(() => {
    mockReplace.mockClear();
    useAuthStore.setState({ user: null, isAuthenticated: false });
  });

  it("renders children when authenticated and hydrated", async () => {
    useAuthStore.setState({
      isAuthenticated: true,
      user: {
        userId: "test-user",
        name: "Test User",
        role: "admin",
        orgId: "org-1",
        accessToken: "token",
      },
    });

    render(
      <AuthGuard>
        <div>Protected content</div>
      </AuthGuard>
    );

    await waitFor(() => {
      expect(screen.getByText("Protected content")).toBeInTheDocument();
    });
  });

  it("redirects to login when not authenticated after hydration", async () => {
    render(
      <AuthGuard>
        <div>Protected content</div>
      </AuthGuard>
    );

    await waitFor(() => {
      expect(mockReplace).toHaveBeenCalledWith("/login");
    });
  });

  it("does not render children when not authenticated", async () => {
    render(
      <AuthGuard>
        <div>Protected content</div>
      </AuthGuard>
    );

    await waitFor(() => {
      expect(mockReplace).toHaveBeenCalled();
    });

    expect(screen.queryByText("Protected content")).not.toBeInTheDocument();
  });
});
