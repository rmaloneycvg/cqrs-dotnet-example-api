import type { Meta, StoryObj } from "@storybook/react";
import { Badge } from "./badge";

const meta = {
  title: "UI/Badge",
  component: Badge,
  tags: ["autodocs"],
  argTypes: {
    variant: {
      control: "select",
      options: ["default", "secondary", "destructive", "outline", "success", "warning"],
    },
  },
} satisfies Meta<typeof Badge>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {
  args: { children: "Badge" },
};

export const Success: Story = {
  args: { children: "Active", variant: "success" },
};

export const Destructive: Story = {
  args: { children: "Deceased", variant: "destructive" },
};

export const Secondary: Story = {
  args: { children: "Inactive", variant: "secondary" },
};

export const Outline: Story = {
  args: { children: "Outline", variant: "outline" },
};

export const Warning: Story = {
  args: { children: "Warning", variant: "warning" },
};
