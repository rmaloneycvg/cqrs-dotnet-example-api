import type { Meta, StoryObj } from "@storybook/react";
import { Card, CardHeader, CardTitle, CardDescription, CardContent, CardFooter } from "./card";
import { Button } from "./button";

const meta = {
  title: "UI/Card",
  component: Card,
  tags: ["autodocs"],
  parameters: {
    layout: "centered",
  },
} satisfies Meta<typeof Card>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {
  render: () => (
    <Card className="w-[350px]">
      <CardHeader>
        <CardTitle>Card Title</CardTitle>
        <CardDescription>Card description with supporting text.</CardDescription>
      </CardHeader>
      <CardContent>
        <p>Card content goes here. This is where the main body of the card lives.</p>
      </CardContent>
      <CardFooter>
        <Button>Action</Button>
      </CardFooter>
    </Card>
  ),
};

export const Simple: Story = {
  render: () => (
    <Card className="w-[350px]">
      <CardContent className="pt-6">
        <p>A simple card with only content, no header or footer.</p>
      </CardContent>
    </Card>
  ),
};

export const WithoutFooter: Story = {
  render: () => (
    <Card className="w-[350px]">
      <CardHeader>
        <CardTitle>Contact Information</CardTitle>
      </CardHeader>
      <CardContent>
        <dl className="space-y-2 text-sm">
          <div className="flex justify-between">
            <dt className="text-muted-foreground">Email</dt>
            <dd className="font-medium">jane@example.com</dd>
          </div>
          <div className="flex justify-between">
            <dt className="text-muted-foreground">Phone</dt>
            <dd className="font-medium">555-1234</dd>
          </div>
        </dl>
      </CardContent>
    </Card>
  ),
};

export const Dashed: Story = {
  render: () => (
    <Card className="w-[350px] border-dashed">
      <CardContent className="flex flex-col items-center justify-center py-8">
        <h3 className="font-semibold text-muted-foreground mb-2">Coming Soon</h3>
        <p className="text-sm text-muted-foreground">This feature is under development.</p>
      </CardContent>
    </Card>
  ),
};
