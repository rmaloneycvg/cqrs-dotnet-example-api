import type { Meta, StoryObj } from "@storybook/react";
import { useAuthStore } from "@/stores/auth.store";
import { useRealtimeStore } from "@/stores/realtime.store";
import { DashboardHeader } from "./dashboard-header";
import { Button } from "@/components/ui/button";

const meta = {
  title: "Layout/DashboardHeader",
  component: DashboardHeader,
  tags: ["autodocs"],
  parameters: {
    layout: "fullscreen",
  },
  decorators: [
    (Story) => {
      useAuthStore.setState({
        user: {
          userId: "dev-admin",
          name: "Dr. Admin",
          role: "admin",
          orgId: "00000000-0000-0000-0000-000000000001",
          accessToken: "mock-token",
        },
        isAuthenticated: true,
      });
      useRealtimeStore.setState({ status: "connected" });
      return <Story />;
    },
  ],
} satisfies Meta<typeof DashboardHeader>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {
  args: {
    title: "Patients",
    description: "Manage patient records",
  },
};

export const WithActions: Story = {
  args: {
    title: "Patients",
    description: "Manage patient records",
    children: <Button size="sm">Add Patient</Button>,
  },
};

export const TitleOnly: Story = {
  args: {
    title: "Dashboard",
  },
};

export const Disconnected: Story = {
  args: {
    title: "Patients",
    description: "Manage patient records",
  },
  decorators: [
    (Story) => {
      useRealtimeStore.setState({ status: "disconnected" });
      return <Story />;
    },
  ],
};

export const Reconnecting: Story = {
  args: {
    title: "Patients",
    description: "Manage patient records",
  },
  decorators: [
    (Story) => {
      useRealtimeStore.setState({ status: "reconnecting" });
      return <Story />;
    },
  ],
};
