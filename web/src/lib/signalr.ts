import {
  HubConnection,
  HubConnectionBuilder,
  HubConnectionState,
  LogLevel,
} from "@microsoft/signalr";

const SIGNALR_URL =
  process.env.NEXT_PUBLIC_SIGNALR_URL ?? "http://localhost:5001/realtime-hub";

let connection: HubConnection | null = null;

/**
 * Gets or creates the singleton SignalR connection.
 *
 * When called with an accessToken, creates a new connection if none exists.
 * When called without a token, returns the existing connection (for subscription hooks).
 * Throws if called without a token and no connection exists.
 */
export function getSignalRConnection(accessToken?: string): HubConnection {
  // Return existing active connection
  if (connection && connection.state !== HubConnectionState.Disconnected) {
    return connection;
  }

  // Cannot create without token
  if (!accessToken) {
    if (connection) return connection;
    throw new Error("Cannot create SignalR connection without access token");
  }

  connection = new HubConnectionBuilder()
    .withUrl(SIGNALR_URL, {
      accessTokenFactory: () => accessToken,
    })
    .withAutomaticReconnect({
      nextRetryDelayInMilliseconds: (retryContext) => {
        // Exponential backoff: 1s, 2s, 4s, 8s, 16s, 30s max
        return Math.min(
          Math.pow(2, retryContext.previousRetryCount) * 1000,
          30_000
        );
      },
    })
    .configureLogging(
      process.env.NODE_ENV === "development" ? LogLevel.Debug : LogLevel.Warning
    )
    .build();

  return connection;
}

/**
 * Disconnects and clears the singleton connection.
 */
export async function disconnectSignalR(): Promise<void> {
  if (connection) {
    const conn = connection;
    connection = null;
    await conn.stop();
  }
}

export { HubConnectionState };
