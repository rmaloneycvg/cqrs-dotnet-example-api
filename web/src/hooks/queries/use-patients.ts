import { queryOptions, useQuery } from "@tanstack/react-query";
import { api } from "@/lib/api";
import { queryKeys } from "@/lib/query-keys";
import {
  PatientListResponseSchema,
  PatientSchema,
  type Patient,
  type PatientListResponse,
} from "@/schemas/patient.schema";

interface PatientListFilters {
  page?: number;
  pageSize?: number;
  status?: string;
  searchTerm?: string;
}

export function patientsQueryOptions(filters: PatientListFilters = {}) {
  const { page = 1, pageSize = 20, status, searchTerm } = filters;
  const params = new URLSearchParams();
  params.set("page", String(page));
  params.set("pageSize", String(pageSize));
  if (status) params.set("status", status);
  if (searchTerm) params.set("searchTerm", searchTerm);

  return queryOptions({
    queryKey: queryKeys.patients.list({ page, status, search: searchTerm }),
    queryFn: async ({ signal }) => {
      const data = await api.get<PatientListResponse>(
        `/api/patients?${params.toString()}`,
        signal
      );
      return PatientListResponseSchema.parse(data);
    },
  });
}

export function patientDetailQueryOptions(id: string) {
  return queryOptions({
    queryKey: queryKeys.patients.detail(id),
    queryFn: async ({ signal }) => {
      const data = await api.get<Patient>(`/api/patients/${id}`, signal);
      return PatientSchema.parse(data);
    },
    enabled: !!id,
  });
}

export function usePatients(filters: PatientListFilters = {}) {
  return useQuery(patientsQueryOptions(filters));
}

export function usePatient(id: string) {
  return useQuery(patientDetailQueryOptions(id));
}
