import type { Meta, StoryObj } from "@storybook/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { PatientList } from "./patient-list";
import { queryKeys } from "@/lib/query-keys";
import type { Patient, PatientListResponse } from "@/schemas/patient.schema";

const MOCK_PATIENTS: Patient[] = [
  {
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
  },
  {
    id: "b2c3d4e5-f6a7-8901-bcde-f12345678901",
    firstName: "John",
    lastName: "Smith",
    fullName: "John Smith",
    dateOfBirth: "1972-11-22",
    email: "john.smith@example.com",
    phoneNumber: "555-0456",
    status: "Active",
    city: "Boulder",
    state: "CO",
    createdAt: "2023-05-15T10:00:00Z",
    updatedAt: "2025-06-18T09:15:00Z",
  },
  {
    id: "c3d4e5f6-a7b8-9012-cdef-123456789012",
    firstName: "Maria",
    lastName: "Garcia",
    fullName: "Maria Garcia",
    dateOfBirth: "1990-07-04",
    email: "maria.garcia@example.com",
    phoneNumber: "555-0789",
    status: "Inactive",
    city: "Aurora",
    state: "CO",
    createdAt: "2024-03-20T14:00:00Z",
    updatedAt: "2025-05-01T11:30:00Z",
  },
  {
    id: "d4e5f6a7-b8c9-0123-defa-234567890123",
    firstName: "Robert",
    lastName: "Johnson",
    fullName: "Robert Johnson",
    dateOfBirth: "1955-12-01",
    email: "robert.j@example.com",
    phoneNumber: "555-0321",
    status: "Deceased",
    city: "Lakewood",
    state: "CO",
    createdAt: "2022-08-10T09:00:00Z",
    updatedAt: "2025-04-15T16:45:00Z",
  },
  {
    id: "e5f6a7b8-c9d0-1234-efab-345678901234",
    firstName: "Sarah",
    lastName: "Williams",
    fullName: "Sarah Williams",
    dateOfBirth: "2001-09-28",
    email: "sarah.w@example.com",
    phoneNumber: "555-0654",
    status: "Transferred",
    city: "Fort Collins",
    state: "CO",
    createdAt: "2024-11-05T12:00:00Z",
    updatedAt: "2025-06-10T08:00:00Z",
  },
];

function createMockQueryClient(response: PatientListResponse) {
  const client = new QueryClient({
    defaultOptions: { queries: { retry: false, gcTime: Infinity, staleTime: Infinity } },
  });
  client.setQueryData(
    queryKeys.patients.list({ page: 1, status: undefined, search: undefined }),
    response
  );
  return client;
}

function QueryWrapper({ client, children }: { client: QueryClient; children: React.ReactNode }) {
  return <QueryClientProvider client={client}>{children}</QueryClientProvider>;
}

const meta = {
  title: "Features/PatientList",
  component: PatientList,
  tags: ["autodocs"],
  parameters: {
    layout: "padded",
    nextjs: { appDirectory: true },
  },
} satisfies Meta<typeof PatientList>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {
  decorators: [
    (Story) => (
      <QueryWrapper
        client={createMockQueryClient({
          items: MOCK_PATIENTS,
          totalCount: 5,
          page: 1,
          pageSize: 20,
          totalPages: 1,
        })}
      >
        <Story />
      </QueryWrapper>
    ),
  ],
};

export const MultiplePages: Story = {
  decorators: [
    (Story) => (
      <QueryWrapper
        client={createMockQueryClient({
          items: MOCK_PATIENTS,
          totalCount: 87,
          page: 1,
          pageSize: 20,
          totalPages: 5,
        })}
      >
        <Story />
      </QueryWrapper>
    ),
  ],
};

export const Empty: Story = {
  decorators: [
    (Story) => (
      <QueryWrapper
        client={createMockQueryClient({
          items: [],
          totalCount: 0,
          page: 1,
          pageSize: 20,
          totalPages: 0,
        })}
      >
        <Story />
      </QueryWrapper>
    ),
  ],
};

export const Loading: Story = {
  decorators: [
    (Story) => {
      const client = new QueryClient({
        defaultOptions: { queries: { retry: false, gcTime: 0 } },
      });
      client.setQueryDefaults(
        queryKeys.patients.list({ page: 1, status: undefined, search: undefined }),
        { queryFn: () => new Promise(() => {}) }
      );
      return (
        <QueryWrapper client={client}>
          <Story />
        </QueryWrapper>
      );
    },
  ],
};

export const Error: Story = {
  decorators: [
    (Story) => {
      const client = new QueryClient({
        defaultOptions: {
          queries: {
            retry: false,
            gcTime: 0,
            queryFn: () => Promise.reject("API is unavailable"),
          },
        },
      });
      return (
        <QueryWrapper client={client}>
          <Story />
        </QueryWrapper>
      );
    },
  ],
};
