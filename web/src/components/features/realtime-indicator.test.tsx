import { render, screen } from "@testing-library/react";
import { describe, it, expect, beforeEach } from "vitest";
import { useRealtimeStore } from "@/stores/realtime.store";
import { RealtimeIndicator } from "./realtime-indicator";

describe("RealtimeIndicator", () => {
  beforeEach(() => {
    useRealtimeStore.setState({ status: "disconnected" });
  });

  it("shows 'Offline' when disconnected", () => {
    render(<RealtimeIndicator />);
    expect(screen.getByText("Offline")).toBeInTheDocument();
  });

  it("shows 'Live' when connected", () => {
    useRealtimeStore.setState({ status: "connected" });
    render(<RealtimeIndicator />);
    expect(screen.getByText("Live")).toBeInTheDocument();
  });

  it("shows 'Connecting...' when connecting", () => {
    useRealtimeStore.setState({ status: "connecting" });
    render(<RealtimeIndicator />);
    expect(screen.getByText("Connecting...")).toBeInTheDocument();
  });

  it("shows 'Reconnecting...' when reconnecting", () => {
    useRealtimeStore.setState({ status: "reconnecting" });
    render(<RealtimeIndicator />);
    expect(screen.getByText("Reconnecting...")).toBeInTheDocument();
  });

  it("has aria-live for accessibility", () => {
    render(<RealtimeIndicator />);
    const container = screen.getByText("Offline").closest("[aria-live]");
    expect(container).toHaveAttribute("aria-live", "polite");
  });
});
