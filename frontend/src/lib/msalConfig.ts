import { PublicClientApplication, Configuration, LogLevel } from '@azure/msal-browser';

const tenantId = 'db99ad1d-fe2f-4b20-82d0-ae098b4f1975';
const clientId = '0233818e-8530-4fba-a15a-beb2443031f5';

const msalConfiguration: Configuration = {
  auth: {
    clientId,
    authority: `https://login.microsoftonline.com/${tenantId}`,
    redirectUri: window.location.origin,
    postLogoutRedirectUri: window.location.origin,
  },
  cache: {
    cacheLocation: 'localStorage',
  },
  system: {
    loggerOptions: {
      logLevel: LogLevel.Warning,
      loggerCallback: (level, message) => {
        if (level === LogLevel.Error) console.error(message);
      },
    },
  },
};

// Scopes requested when acquiring an access token for the backend API
export const apiScopes = ['api://0233818e-8530-4fba-a15a-beb2443031f5/access_as_user'];

// Singleton MSAL instance — shared across api/client.ts, signalr.ts, and React hooks
export const msalInstance = new PublicClientApplication(msalConfiguration);
