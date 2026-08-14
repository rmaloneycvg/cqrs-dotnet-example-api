import type { Meta, StoryObj } from "@storybook/react";
import { useAuthStore, type UserRole } from "@/stores/auth.store";
import { UserBadge } from "./user-badge";

function setMockUser(role: UserRole, name: string) {
  useAuthStore.setState({
    user: {
      userId: `dev-${role}`,
      name,
      role,
      orgId: "00000000-0000-0000-0000-000000000001",
      accessToken: "mock-token",
    },
    isAuthenticated: true,
  });
}

const meta = {
  title: "Layout/UserBadge",
  component: UserBadge,
  tags: ["autodocs"],
  parameters: {
    layout: "centered",
  },
  decorators: [
    (Story) => {
      setMockUser("admin", "Dr. Admin (Full Access)");
      return <Story />;
    },
  ],
} satisfies Meta<typeof UserBadge>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Admin: Story = {
  decorators: [
    (Story) => {
      setMockUser("admin", "Dr. Admin (Full Access)");
      return <Story />;
    },
  ],
};

export const Physician: Story = {
  decorators: [
    (Story) => {
      setMockUser("physician", "Dr. Williams (Physician)");
      return <Story />;
    },
  ],
};

export const Clinician: Story = {
  decorators: [
    (Story) => {
      setMockUser("clinician", "Dr. Smith (Clinician)");
      return <Story />;
    },
  ],
};

export const Nurse: Story = {
  decorators: [
    (Story) => {
      setMockUser("nurse", "Nurse Johnson (PHI Read/Write)");
      return <Story />;
    },
  ],
};

export const ReadOnly: Story = {
  decorators: [
    (Story) => {
      setMockUser("readonly", "Intern Davis (Read Only)");
      return <Story />;
    },
  ],
};

export const NotAuthenticated: Story = {
  decorators: [
    (Story) => {
      useAuthStore.setState({ user: null, isAuthenticated: false });
      return <Story />;
    },
  ],
};
