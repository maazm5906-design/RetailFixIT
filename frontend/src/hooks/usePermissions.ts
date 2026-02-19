import { useAuthStore } from '../store/authStore';
import type { UserRole } from '../types';

const ROLE_PERMISSIONS: Record<UserRole, string[]> = {
  Admin: [
    'create:job',
    'update:job',
    'cancel:job',
    'assign:vendor',
    'request:ai',
    'manage:vendors',
    'view:dashboard',
    'view:auditlogs',
  ],
  Dispatcher: [
    'create:job',
    'update:job',
    'assign:vendor',
    'request:ai',
    'view:dashboard',
  ],
  VendorManager: [
    'manage:vendors',
    'view:dashboard',
  ],
  SupportAgent: [
    'view:dashboard',
  ],
};

export function usePermissions() {
  const user = useAuthStore((s) => s.user);

  const can = (permission: string): boolean => {
    if (!user) return false;
    const perms = ROLE_PERMISSIONS[user.role] ?? [];
    return perms.includes(permission);
  };

  const hasRole = (...roles: UserRole[]): boolean => {
    if (!user) return false;
    return roles.includes(user.role);
  };

  return { can, hasRole, user };
}