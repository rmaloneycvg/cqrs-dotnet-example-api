import { describe, it, expect } from "vitest";
import { PatientSchema, PatientListResponseSchema } from "@/schemas/patient.schema";

describe("PatientSchema", () => {
  const validPatient = {
    id: "00000000-0000-0000-0000-000000000001",
    firstName: "John",
    lastName: "Doe",
    fullName: "John Doe",
    dateOfBirth: "1990-01-15",
    email: "john.doe@example.com",
    phoneNumber: "555-0101",
    status: "Active" as const,
    city: "Denver",
    state: "CO",
    createdAt: "2026-01-01T00:00:00Z",
    updatedAt: "2026-08-12T15:00:00Z",
  };

  it("validates a correct patient object", () => {
    const result = PatientSchema.safeParse(validPatient);
    expect(result.success).toBe(true);
  });

  it("rejects invalid email", () => {
    const result = PatientSchema.safeParse({ ...validPatient, email: "not-an-email" });
    expect(result.success).toBe(false);
  });

  it("rejects invalid status", () => {
    const result = PatientSchema.safeParse({ ...validPatient, status: "Unknown" });
    expect(result.success).toBe(false);
  });

  it("rejects missing required fields", () => {
    const { firstName, ...incomplete } = validPatient;
    const result = PatientSchema.safeParse(incomplete);
    expect(result.success).toBe(false);
  });
});

describe("PatientListResponseSchema", () => {
  it("validates a correct list response", () => {
    const response = {
      items: [
        {
          id: "00000000-0000-0000-0000-000000000001",
          firstName: "Jane",
          lastName: "Smith",
          fullName: "Jane Smith",
          dateOfBirth: "1985-06-20",
          email: "jane@example.com",
          phoneNumber: "555-0102",
          status: "Active",
          city: "Boulder",
          state: "CO",
          createdAt: "2026-02-01T00:00:00Z",
          updatedAt: "2026-08-10T12:00:00Z",
        },
      ],
      totalCount: 1,
      page: 1,
      pageSize: 20,
      totalPages: 1,
    };

    const result = PatientListResponseSchema.safeParse(response);
    expect(result.success).toBe(true);
  });

  it("validates empty items list", () => {
    const response = {
      items: [],
      totalCount: 0,
      page: 1,
      pageSize: 20,
      totalPages: 0,
    };

    const result = PatientListResponseSchema.safeParse(response);
    expect(result.success).toBe(true);
  });
});
