import type { Meta, StoryObj } from "@storybook/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { PatientDetail } from "./patient-detail";
import { queryKeys } from "@/lib/query-keys";
import type { Patient } from "@/schemas/patient.schema";

const MOCK_PATIENT: Patient = {
  id: "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  firstName: "Jane",
  lastName: "Doe",
  fullName: "Jane Doe",
  dateOfBirth: "1985-03-15",
  email: "jane.doe@example.com",
  phoneNumber: "555-0123",
  status: "Active",
  city: "Denver",
  state: "CO",
  createdAt: "2024-01-10T08:30:00Z",
  updatedAt: "2025-06-20T14:22:00Z",
};

function createMockQueryClient(patient: Patient | null, options?: { error?: boolean }) {
  const client = new QueryClient({
    defaultOptions: { queries: { retry: false, gcTime: 0 } },
  });

  if (options?.error) {
    const rejectFn = () => Promise.reject("Network error: failed to fetch patient");
    client.setQueryDefaults(queryKeys.patients.detail(patient?.id ?? "error-id"), {
      queryFn: rejectFn,
    });
  } else if (patient) {
    client.setQueryData(queryKeys.patients.detail(patient.id), patient);
  }

  return client;
}

function QueryWrapper({ client, children }: { client: QueryClient; children: React.ReactNode }) {
  return <QueryClientProvider client={client}>{children}</QueryClientProvider>;
}

const meta = {
  title: "Features/PatientDetail",
  component: PatientDetail,
  tags: ["autodocs"],
  parameters: {
    layout: "padded",
  },
} satisfies Meta<typeof PatientDetail>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {
  args: { patientId: MOCK_PATIENT.id },
  decorators: [
    (Story) => (
      <QueryWrapper client={createMockQueryClient(MOCK_PATIENT)}>
        <Story />
      </QueryWrapper>
    ),
  ],
};

export const InactivePatient: Story = {
  args: { patientId: MOCK_PATIENT.id },
  decorators: [
    (Story) => (
      <QueryWrapper client={createMockQueryClient({ ...MOCK_PATIENT, status: "Inactive" })}>
        <Story />
      </QueryWrapper>
    ),
  ],
};

export const DeceasedPatient: Story = {
  args: { patientId: MOCK_PATIENT.id },
  decorators: [
    (Story) => (
      <QueryWrapper client={createMockQueryClient({ ...MOCK_PATIENT, status: "Deceased" })}>
        <Story />
      </QueryWrapper>
    ),
  ],
};

export const TransferredPatient: Story = {
  args: { patientId: MOCK_PATIENT.id },
  decorators: [
    (Story) => (
      <QueryWrapper client={createMockQueryClient({ ...MOCK_PATIENT, status: "Transferred" })}>
        <Story />
      </QueryWrapper>
    ),
  ],
};

export const Loading: Story = {
  args: { patientId: "loading-id" },
  render: (args) => {
    const client = new QueryClient({
      defaultOptions: { queries: { retry: false, gcTime: 0 } },
    });
    client.setQueryDefaults(queryKeys.patients.detail("loading-id"), {
      queryFn: () => new Promise(() => {}),
    });
    return (
      <QueryWrapper client={client}>
        <PatientDetail {...args} />
      </QueryWrapper>
    );
  },
};

export const Error: Story = {
  args: { patientId: "error-id" },
  render: (args) => {
    const client = new QueryClient({
      defaultOptions: {
        queries: {
          retry: false,
          gcTime: 0,
          queryFn: () => Promise.reject("Network error: failed to fetch patient"),
        },
      },
    });
    return (
      <QueryWrapper client={client}>
        <PatientDetail {...args} />
      </QueryWrapper>
    );
  },
};
