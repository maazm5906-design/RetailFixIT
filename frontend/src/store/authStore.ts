import { create } from 'zustand';
import { persist } from 'zustand/middleware';
import type { UserInfo } from '../types';

// MSAL manages token acquisition and refresh.
// This store only holds the resolved user info after login.
interface AuthState {
  user: UserInfo | null;
  setUser: (user: UserInfo | null) => void;
  logout: () => void;
}

export const useAuthStore = create<AuthState>()(
  persist(
    (set) => ({
      user: null,
      setUser: (user) => set({ user }),
      logout: () => set({ user: null }),
    }),
    {
      name: 'retailfixit-auth',
      partialize: (state) => ({ user: state.user }),
    }
  )
);
