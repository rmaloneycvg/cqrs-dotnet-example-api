export const queryKeys = {
  patients: {
    all: ["patients"] as const,
    lists: () => [...queryKeys.patients.all, "list"] as const,
    list: (filters: { page?: number; status?: string; search?: string }) =>
      [...queryKeys.patients.lists(), filters] as const,
    details: () => [...queryKeys.patients.all, "detail"] as const,
    detail: (id: string) => [...queryKeys.patients.details(), id] as const,
  },
  billing: {
    all: ["billing"] as const,
    byPatient: (patientId: string) =>
      [...queryKeys.billing.all, patientId] as const,
  },
} as const;
