import type { Meta, StoryObj } from "@storybook/react";
import { Sidebar } from "./sidebar";

const meta = {
  title: "Layout/Sidebar",
  component: Sidebar,
  tags: ["autodocs"],
  parameters: {
    layout: "fullscreen",
    nextjs: { appDirectory: true, navigation: { pathname: "/patients" } },
  },
} satisfies Meta<typeof Sidebar>;

export default meta;
type Story = StoryObj<typeof meta>;

export const PatientsActive: Story = {
  parameters: {
    nextjs: { navigation: { pathname: "/patients" } },
  },
};

export const AppointmentsActive: Story = {
  parameters: {
    nextjs: { navigation: { pathname: "/appointments" } },
  },
};

export const BillingActive: Story = {
  parameters: {
    nextjs: { navigation: { pathname: "/billing" } },
  },
};

export const PrescriptionsActive: Story = {
  parameters: {
    nextjs: { navigation: { pathname: "/prescriptions" } },
  },
};
