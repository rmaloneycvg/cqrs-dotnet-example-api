import type { Meta, StoryObj } from "@storybook/react";
import { useAuthStore } from "@/stores/auth.store";
import { AuthGuard } from "./auth-guard";

const meta = {
  title: "Layout/AuthGuard",
  component: AuthGuard,
  tags: ["autodocs"],
  parameters: {
    layout: "fullscreen",
    nextjs: { appDirectory: true },
  },
} satisfies Meta<typeof AuthGuard>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Authenticated: Story = {
  args: {
    children: (
      <div className="p-8">
        <h1 className="text-2xl font-bold">Protected Content</h1>
        <p className="text-muted-foreground mt-2">
          This content is only visible to authenticated users.
        </p>
      </div>
    ),
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
      // Simulate hydration complete
      (useAuthStore.persist as any).hasHydrated = () => true;
      return <Story />;
    },
  ],
};

export const Loading: Story = {
  args: {
    children: <div>This should not be visible during loading.</div>,
  },
  decorators: [
    (Story) => {
      useAuthStore.setState({ user: null, isAuthenticated: false });
      // Simulate hydration not yet complete
      (useAuthStore.persist as any).hasHydrated = () => false;
      return <Story />;
    },
  ],
};
