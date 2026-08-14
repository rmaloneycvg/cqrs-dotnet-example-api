import { render, screen, fireEvent } from "@testing-library/react";
import { describe, it, expect, beforeEach, vi } from "vitest";
import { useAuthStore } from "@/stores/auth.store";
import { UserBadge } from "./user-badge";

const mockPush = vi.fn();
vi.mock("next/navigation", () => ({
  useRouter: () => ({
    push: mockPush,
    replace: vi.fn(),
    back: vi.fn(),
    forward: vi.fn(),
    refresh: vi.fn(),
    prefetch: vi.fn(),
  }),
}));

describe("UserBadge", () => {
  beforeEach(() => {
    mockPush.mockClear();
    useAuthStore.setState({
      isAuthenticated: true,
      user: {
        userId: "test-user",
        name: "Dr. Test",
        role: "physician",
        orgId: "org-1",
        accessToken: "token",
      },
    });
  });

  it("renders user name and role", () => {
    render(<UserBadge />);
    expect(screen.getByText("Dr. Test")).toBeInTheDocument();
    expect(screen.getByText("physician")).toBeInTheDocument();
  });

  it("renders nothing when no user", () => {
    useAuthStore.setState({ user: null, isAuthenticated: false });
    const { container } = render(<UserBadge />);
    expect(container).toBeEmptyDOMElement();
  });

  it("calls logout and redirects on Logout click", () => {
    render(<UserBadge />);
    fireEvent.click(screen.getByTitle("Logout"));
    expect(useAuthStore.getState().isAuthenticated).toBe(false);
    expect(mockPush).toHaveBeenCalledWith("/login");
  });

  it("calls logout and redirects on Switch click", () => {
    render(<UserBadge />);
    fireEvent.click(screen.getByTitle("Switch role"));
    expect(useAuthStore.getState().isAuthenticated).toBe(false);
    expect(mockPush).toHaveBeenCalledWith("/login");
  });
});
