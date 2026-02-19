import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { BrowserRouter } from 'react-router-dom';
import { QueryClientProvider } from '@tanstack/react-query';
import { MsalProvider } from '@azure/msal-react';
import { Toaster } from 'react-hot-toast';
import type { AuthenticationResult } from '@azure/msal-browser';
import { queryClient } from './lib/queryClient';
import { msalInstance, apiScopes } from './lib/msalConfig';
import { useAuthStore } from './store/authStore';
import type { UserInfo, UserRole } from './types';
import App from './App';
import './index.css';

async function bootstrap() {
  // Step 1: Initialize MSAL (required before any API call)
  await msalInstance.initialize();

  // Step 2: In MSAL Browser v5, handleRedirectPromise() MUST be called explicitly
  // after initialize() to process the auth code returned by loginRedirect.
  // This is the key step — without it the auth code is discarded and accounts stay empty.
  let redirectResult: AuthenticationResult | null = null;
  try {
    redirectResult = await msalInstance.handleRedirectPromise();
    console.log('[MSAL] handleRedirectPromise result:', redirectResult?.account?.username ?? 'null');
  } catch (e) {
    console.error('[MSAL] handleRedirectPromise threw:', e);
    sessionStorage.setItem('auth_error', 'Authentication failed. Please try again.');
  }

  if (redirectResult) {
    // Returning from loginRedirect with a fresh token — call /auth/me directly
    try {
      const response = await fetch('/api/v1/auth/me', {
        headers: { Authorization: `Bearer ${redirectResult.accessToken}` },
      });
      if (response.ok) {
        const data = await response.json();
        useAuthStore.getState().setUser({
          userId: data.userId,
          email: data.email,
          displayName: data.displayName,
          role: data.role as UserRole,
          tenantId: data.tenantId,
        } satisfies UserInfo);
      } else {
        await msalInstance.clearCache();
        sessionStorage.setItem('auth_error', response.status === 403
          ? 'Your account does not have an assigned role. Contact your administrator.'
          : `Server error ${response.status}. Please try again.`);
      }
    } catch {
      sessionStorage.setItem('auth_error', 'Could not reach the server. Please try again.');
    }
  } else {
    // Not a redirect return — check for a persisted session (returning user)
    const accounts = msalInstance.getAllAccounts();
    if (accounts.length > 0 && !useAuthStore.getState().user) {
      try {
        const tokenResult = await msalInstance.acquireTokenSilent({
          scopes: apiScopes,
          account: accounts[0],
        });
        const response = await fetch('/api/v1/auth/me', {
          headers: { Authorization: `Bearer ${tokenResult.accessToken}` },
        });
        if (response.ok) {
          const data = await response.json();
          useAuthStore.getState().setUser({
            userId: data.userId,
            email: data.email,
            displayName: data.displayName,
            role: data.role as UserRole,
            tenantId: data.tenantId,
          } satisfies UserInfo);
        }
      } catch {
        // Silently fail — user will see login page
      }
    }
  }

  createRoot(document.getElementById('root')!).render(
    <StrictMode>
      <MsalProvider instance={msalInstance}>
        <QueryClientProvider client={queryClient}>
          <BrowserRouter>
            <App />
            <Toaster position="top-right" toastOptions={{ duration: 4000 }} />
          </BrowserRouter>
        </QueryClientProvider>
      </MsalProvider>
    </StrictMode>
  );
}

bootstrap();
