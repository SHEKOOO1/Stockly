import type { DemoUser } from '@/types/domain';
import { findRouteMeta, navigation, type NavigationItem } from '@/navigation';
import type { AppContextScope } from '@/types/domain';

const tenantOwnerRoleCode = 'TenantOwner';
const warehouseManagerRoleCode = 'WarehouseManager';
const storeKeeperRoleCode = 'StoreKeeper';

const restrictedWarehousePaths = ['/warehouse/settings', '/warehouse/employees'];

export function isTenantOwner(user: DemoUser): boolean {
  return user.roleCode === tenantOwnerRoleCode;
}

export function canAccessScope(scope: AppContextScope, user: DemoUser): boolean {
  if (scope === 'warehouse') {
    return true;
  }
  return isTenantOwner(user);
}

export function visibleScopesForUser(user: DemoUser): AppContextScope[] {
  const scopes: AppContextScope[] = ['platform', 'organization', 'warehouse'];
  return scopes.filter((scope) => canAccessScope(scope, user));
}

export function landingPathForUser(user: DemoUser): string {
  return isTenantOwner(user) ? '/platform' : '/warehouse/operations';
}

export function canAccessPath(pathname: string, user: DemoUser): boolean {
  const routePath = findRouteMeta(pathname)?.path ?? pathname;
  const scope =
    findRouteMeta(pathname)?.scope ??
    (routePath.startsWith('/warehouse') ? 'warehouse' : 'platform');
  if (!canAccessScope(scope, user)) {
    return false;
  }
  if (scope !== 'warehouse') {
    return true;
  }
  if (user.roleCode === 'Worker') {
    return routePath === '/warehouse/operations';
  }
  if (user.roleCode === storeKeeperRoleCode) {
    return !restrictedWarehousePaths.includes(routePath);
  }
  return (
    isTenantOwner(user) ||
    user.roleCode === warehouseManagerRoleCode ||
    user.roleCode === storeKeeperRoleCode
  );
}

export function visibleNavigationForUser(scope: AppContextScope, user: DemoUser): NavigationItem[] {
  return navigation[scope].filter((item) => canAccessPath(item.path, user));
}
