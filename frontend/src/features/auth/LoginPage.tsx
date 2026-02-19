import React, { useEffect, useState } from 'react';
import { Navigate } from 'react-router-dom';
import { useMsal } from '@azure/msal-react';
import toast from 'react-hot-toast';
import { Wrench } from 'lucide-react';
import { apiScopes } from '../../lib/msalConfig';
import { useAuthStore } from '../../store/authStore';

export default function LoginPage() {
  const { instance } = useMsal();
  const user = useAuthStore((s) => s.user);
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    const err = sessionStorage.getItem('auth_error');
    if (err) {
      sessionStorage.removeItem('auth_error');
      toast.error(err);
    }
  }, []);

  if (user) return <Navigate to="/dashboard" replace />;

  const handleLogin = async () => {
    setLoading(true);
    Object.keys(sessionStorage)
      .filter((k) => k.includes('interaction.status') || k.includes('request.params'))
      .forEach((k) => sessionStorage.removeItem(k));
    try {
      await instance.loginRedirect({
        scopes: apiScopes,
        redirectUri: window.location.origin,
        prompt: 'select_account',
      });
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : String(err);
      toast.error(`Auth error: ${msg.substring(0, 120)}`);
      setLoading(false);
    }
  };

  return (
    <div className="min-h-screen bg-gray-950 flex items-center justify-center px-4">
      <div className="w-full max-w-md">
        <div className="text-center mb-8">
          <div className="inline-flex items-center justify-center w-16 h-16 bg-blue-600 rounded-2xl mb-4">
            <Wrench className="w-8 h-8 text-white" />
          </div>
          <h1 className="text-3xl font-bold text-white">RetailFixIT</h1>
          <p className="text-gray-400 mt-2">Field Service Management</p>
        </div>

        <div className="bg-gray-900 rounded-2xl p-8 border border-gray-800 shadow-2xl">
          <h2 className="text-xl font-semibold text-white mb-2">Sign in to your account</h2>
          <p className="text-gray-500 text-sm mb-8">Use your Microsoft Entra ID credentials</p>

          <button
            onClick={handleLogin}
            disabled={loading}
            className="w-full flex items-center justify-center gap-3 py-3 px-4 bg-white hover:bg-gray-100 disabled:opacity-60 disabled:cursor-not-allowed text-gray-900 font-semibold rounded-lg transition focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2 focus:ring-offset-gray-900"
          >
            <svg width="20" height="20" viewBox="0 0 21 21" xmlns="http://www.w3.org/2000/svg">
              <rect x="1" y="1" width="9" height="9" fill="#F25022" />
              <rect x="11" y="1" width="9" height="9" fill="#7FBA00" />
              <rect x="1" y="11" width="9" height="9" fill="#00A4EF" />
              <rect x="11" y="11" width="9" height="9" fill="#FFB900" />
            </svg>
            {loading ? 'Signing in…' : 'Sign in with Microsoft'}
          </button>

          <p className="mt-6 text-center text-xs text-gray-600">
            Secured by Microsoft Entra ID
          </p>
        </div>
      </div>
    </div>
  );
}
