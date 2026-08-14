import type { Meta, StoryObj } from "@storybook/react";
import { Select } from "./select";

const meta = {
  title: "UI/Select",
  component: Select,
  tags: ["autodocs"],
  parameters: {
    layout: "centered",
  },
  argTypes: {
    disabled: { control: "boolean" },
  },
} satisfies Meta<typeof Select>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {
  render: (args) => (
    <Select aria-label="Select option" {...args}>
      <option value="">Select an option</option>
      <option value="active">Active</option>
      <option value="inactive">Inactive</option>
      <option value="transferred">Transferred</option>
    </Select>
  ),
};

export const WithLabel: Story = {
  render: (args) => (
    <div className="space-y-2 w-64">
      <label htmlFor="status-select" className="text-sm font-medium">
        Patient Status
      </label>
      <Select id="status-select" {...args}>
        <option value="">All Statuses</option>
        <option value="Active">Active</option>
        <option value="Inactive">Inactive</option>
        <option value="Deceased">Deceased</option>
        <option value="Transferred">Transferred</option>
      </Select>
    </div>
  ),
};

export const Preselected: Story = {
  render: (args) => (
    <Select defaultValue="Active" aria-label="Status" {...args}>
      <option value="Active">Active</option>
      <option value="Inactive">Inactive</option>
      <option value="Deceased">Deceased</option>
      <option value="Transferred">Transferred</option>
    </Select>
  ),
};

export const Disabled: Story = {
  render: () => (
    <Select disabled aria-label="Disabled select">
      <option value="active">Active</option>
      <option value="inactive">Inactive</option>
    </Select>
  ),
};
