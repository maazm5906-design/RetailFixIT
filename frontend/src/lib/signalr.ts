import * as signalR from '@microsoft/signalr';
import { msalInstance, apiScopes } from './msalConfig';

let connection: signalR.HubConnection | null = null;

async function getAccessToken(): Promise<string> {
  const accounts = msalInstance.getAllAccounts();
  if (accounts.length === 0) return '';
  try {
    const response = await msalInstance.acquireTokenSilent({
      scopes: apiScopes,
      account: accounts[0],
    });
    return response.accessToken;
  } catch {
    return '';
  }
}

export function getSignalRConnection(): signalR.HubConnection {
  if (connection && connection.state !== signalR.HubConnectionState.Disconnected) {
    return connection;
  }

  connection = new signalR.HubConnectionBuilder()
    .withUrl('/hubs/jobs', {
      accessTokenFactory: getAccessToken,
    })
    .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
    .configureLogging(signalR.LogLevel.Warning)
    .build();

  return connection;
}

export function closeSignalRConnection(): void {
  if (connection) {
    connection.stop().catch(console.error);
    connection = null;
  }
}