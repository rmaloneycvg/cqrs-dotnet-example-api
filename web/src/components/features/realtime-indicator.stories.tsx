import type { Meta, StoryObj } from "@storybook/react";
import { useRealtimeStore } from "@/stores/realtime.store";
import { RealtimeIndicator } from "./realtime-indicator";

type ConnectionStatus = "connected" | "connecting" | "reconnecting" | "disconnected";

const meta = {
  title: "Features/RealtimeIndicator",
  component: RealtimeIndicator,
  tags: ["autodocs"],
} satisfies Meta<typeof RealtimeIndicator>;

export default meta;
type Story = StoryObj<typeof meta>;

function withStatus(status: ConnectionStatus) {
  return (Story: () => React.JSX.Element) => {
    useRealtimeStore.setState({ status });
    return <Story />;
  };
}

export const Connected: Story = {
  decorators: [withStatus("connected")],
};

export const Connecting: Story = {
  decorators: [withStatus("connecting")],
};

export const Reconnecting: Story = {
  decorators: [withStatus("reconnecting")],
};

export const Disconnected: Story = {
  decorators: [withStatus("disconnected")],
};
