import type { Meta, StoryObj } from "@storybook/react";
import { Input } from "./input";

const meta = {
  title: "UI/Input",
  component: Input,
  tags: ["autodocs"],
  parameters: {
    layout: "centered",
  },
  argTypes: {
    type: {
      control: "select",
      options: ["text", "email", "password", "search", "number", "tel", "url"],
    },
    disabled: { control: "boolean" },
    placeholder: { control: "text" },
  },
} satisfies Meta<typeof Input>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {
  args: {
    placeholder: "Enter text...",
  },
};

export const Email: Story = {
  args: {
    type: "email",
    placeholder: "you@example.com",
  },
};

export const Password: Story = {
  args: {
    type: "password",
    placeholder: "Enter password",
  },
};

export const Search: Story = {
  args: {
    type: "search",
    placeholder: "Search patients...",
    "aria-label": "Search patients",
  },
};

export const WithValue: Story = {
  args: {
    defaultValue: "Dr. Jane Smith",
  },
};

export const Disabled: Story = {
  args: {
    placeholder: "Cannot edit",
    disabled: true,
  },
};

export const WithLabel: Story = {
  render: (args) => (
    <div className="space-y-2 w-64">
      <label htmlFor="email-input" className="text-sm font-medium">
        Email Address
      </label>
      <Input id="email-input" type="email" placeholder="you@example.com" {...args} />
    </div>
  ),
};
