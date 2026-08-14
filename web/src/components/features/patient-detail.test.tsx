import { screen, waitFor } from "@testing-library/react";
import { describe, it, expect, vi, beforeEach } from "vitest";
import { renderWithProviders } from "@/test/utils";
import { PatientDetail } from "./patient-detail";

// Mock the realtime subscription hook (no WebSocket in tests)
vi.mock("@/hooks/use-realtime-subscription", () => ({
  useRealtimeSubscription: vi.fn(),
}));

// Mock the API module
vi.mock("@/lib/api", () => ({
  api: {
    get: vi.fn(),
  },
  ApiError: class extends Error {
    status: number;
    statusText: string;
    constructor(status: number, statusText: string) {
      super(`${status} ${statusText}`);
      this.status = status;
      this.statusText = statusText;
    }
  },
}));

const { api } = await import("@/lib/api");
const mockGet = vi.mocked(api.get);

const mockPatient = {
  id: "00000000-0000-0000-0000-000000000001",
  firstName: "Jane",
  lastName: "Doe",
  fullName: "Jane Doe",
  dateOfBirth: "1985-03-15",
  email: "jane@example.com",
  phoneNumber: "555-1234",
  status: "Active" as const,
  city: "Denver",
  state: "CO",
  createdAt: "2026-01-01T00:00:00Z",
  updatedAt: "2026-08-12T12:00:00Z",
};

describe("PatientDetail", () => {
  beforeEach(() => {
    mockGet.mockReset();
  });

  it("shows loading skeleton initially", () => {
    mockGet.mockReturnValue(new Promise(() => {})); // Never resolves
    renderWithProviders(<PatientDetail patientId="test-id" />);
    // Skeleton elements are rendered during loading
    expect(document.querySelector(".animate-pulse")).toBeInTheDocument();
  });

  it("renders patient data after loading", async () => {
    mockGet.mockResolvedValue(mockPatient);
    renderWithProviders(<PatientDetail patientId={mockPatient.id} />);

    await waitFor(() => {
      expect(screen.getByText("Jane Doe")).toBeInTheDocument();
    });

    expect(screen.getByText("jane@example.com")).toBeInTheDocument();
    expect(screen.getByText("555-1234")).toBeInTheDocument();
    expect(screen.getByText("Active")).toBeInTheDocument();
    expect(screen.getByText("Denver, CO")).toBeInTheDocument();
  });

  it("shows error state on failure", async () => {
    mockGet.mockRejectedValue(new Error("Network error"));
    renderWithProviders(<PatientDetail patientId="bad-id" />);

    await waitFor(() => {
      expect(screen.getByRole("alert")).toBeInTheDocument();
    });

    expect(screen.getByText(/Failed to load patient/)).toBeInTheDocument();
  });
});
