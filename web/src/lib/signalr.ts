import {
  HubConnection,
  HubConnectionBuilder,
  HubConnectionState,
  LogLevel,
} from "@microsoft/signalr";

const SIGNALR_URL =
  process.env.NEXT_PUBLIC_SIGNALR_URL ?? "http://localhost:5001/realtime-hub";

let connection: HubConnection | null = null;

export function getSignalRConnection(accessToken?: string): HubConnection {
  if (connection && connection.state !== HubConnectionState.Disconnected) {
    return connection;
  }

  const builder = new HubConnectionBuilder()
    .withUrl(SIGNALR_URL, {
      accessTokenFactory: () => accessToken ?? "",
    })
    .withAutomaticReconnect({
      nextRetryDelayInMilliseconds: (retryContext) => {
        // Exponential backoff: 0, 1s, 2s, 4s, 8s, 16s, 30s max
        const delay = Math.min(
          Math.pow(2, retryContext.previousRetryCount) * 1000,
          30000
        );
        return delay;
      },
    })
    .configureLogging(
      process.env.NODE_ENV === "development"
        ? LogLevel.Debug
        : LogLevel.Warning
    );

  connection = builder.build();
  return connection;
}

export function disconnectSignalR(): Promise<void> {
  if (connection) {
    const conn = connection;
    connection = null;
    return conn.stop();
  }
  return Promise.resolve();
}

export { HubConnectionState };
