import { z } from "zod";

export const PatientSchema = z.object({
  id: z.string().uuid(),
  firstName: z.string(),
  lastName: z.string(),
  fullName: z.string(),
  dateOfBirth: z.string(), // ISO date string from API
  email: z.string().email(),
  phoneNumber: z.string(),
  status: z.enum(["Active", "Inactive", "Deceased", "Transferred"]),
  city: z.string(),
  state: z.string(),
  createdAt: z.string(),
  updatedAt: z.string(),
});

export type Patient = z.infer<typeof PatientSchema>;

export const PatientListResponseSchema = z.object({
  items: z.array(PatientSchema),
  totalCount: z.number(),
  page: z.number(),
  pageSize: z.number(),
  totalPages: z.number(),
});

export type PatientListResponse = z.infer<typeof PatientListResponseSchema>;
