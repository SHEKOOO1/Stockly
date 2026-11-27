import {
  Activity,
  AlertTriangle,
  ArrowDownToLine,
  ArrowUpFromLine,
  BarChart3,
  Boxes,
  Building2,
  CalendarClock,
  ClipboardCheck,
  ClipboardList,
  CreditCard,
  History,
  LayoutDashboard,
  type LucideIcon,
  Package,
  Ruler,
  Settings,
  Shield,
  Tags,
  Truck,
  User,
  Users,
  Warehouse,
} from 'lucide-react';
import type { TranslationKey } from '@/i18n';
import type { AppContextScope } from '@/types/domain';

export interface NavigationItem {
  path: string;
  scope: AppContextScope;
  labelKey: TranslationKey;
  groupKey: TranslationKey;
  icon: LucideIcon;
  order: number;
}

export interface RouteMeta {
  path: string;
  scope: AppContextScope;
  titleKey: TranslationKey;
  descriptionKey: TranslationKey;
  placeholderKey: TranslationKey;
  parentPath?: string;
  nav?: Omit<NavigationItem, 'path' | 'scope'>;
}

export interface BreadcrumbItem {
  id: string;
  labelKey: TranslationKey;
  to?: string;
}

const overviewGroup: TranslationKey = 'common.nav.groups.overview';
const managementGroup: TranslationKey = 'common.nav.groups.management';
const catalogGroup: TranslationKey = 'common.nav.groups.catalog';
const operationsGroup: TranslationKey = 'common.nav.groups.operations';
const controlGroup: TranslationKey = 'common.nav.groups.control';

export const routeMeta: readonly RouteMeta[] = [
  {
    path: '/platform',
    scope: 'platform',
    titleKey: 'platform.dashboard.title',
    descriptionKey: 'platform.dashboard.description',
    placeholderKey: 'platform.dashboard.placeholder',
    nav: {
      labelKey: 'platform.dashboard.title',
      groupKey: overviewGroup,
      icon: LayoutDashboard,
      order: 1,
    },
  },
  {
    path: '/platform/organizations',
    scope: 'platform',
    titleKey: 'platform.organizations.title',
    descriptionKey: 'platform.organizations.description',
    placeholderKey: 'platform.organizations.placeholder',
    nav: {
      labelKey: 'platform.organizations.title',
      groupKey: managementGroup,
      icon: Building2,
      order: 2,
    },
  },
  {
    path: '/platform/organizations/new',
    scope: 'platform',
    titleKey: 'platform.organizationsNew.title',
    descriptionKey: 'platform.organizationsNew.description',
    placeholderKey: 'platform.organizationsNew.placeholder',
    parentPath: '/platform/organizations',
  },
  {
    path: '/platform/organizations/:id',
    scope: 'platform',
    titleKey: 'platform.organizationDetails.title',
    descriptionKey: 'platform.organizationDetails.description',
    placeholderKey: 'platform.organizationDetails.placeholder',
    parentPath: '/platform/organizations',
  },
  {
    path: '/platform/organizations/:id/administrators',
    scope: 'platform',
    titleKey: 'platform.organizationAdministrators.title',
    descriptionKey: 'platform.organizationAdministrators.description',
    placeholderKey: 'platform.organizationAdministrators.placeholder',
    parentPath: '/platform/organizations/:id',
  },
  {
    path: '/platform/subscriptions',
    scope: 'platform',
    titleKey: 'platform.subscriptions.title',
    descriptionKey: 'platform.subscriptions.description',
    placeholderKey: 'platform.subscriptions.placeholder',
    nav: {
      labelKey: 'platform.subscriptions.title',
      groupKey: managementGroup,
      icon: CreditCard,
      order: 3,
    },
  },
  {
    path: '/platform/settings',
    scope: 'platform',
    titleKey: 'platform.settings.title',
    descriptionKey: 'platform.settings.description',
    placeholderKey: 'platform.settings.placeholder',
    nav: { labelKey: 'platform.settings.title', groupKey: controlGroup, icon: Settings, order: 4 },
  },
  {
    path: '/platform/activity',
    scope: 'platform',
    titleKey: 'platform.activity.title',
    descriptionKey: 'platform.activity.description',
    placeholderKey: 'platform.activity.placeholder',
    nav: { labelKey: 'platform.activity.title', groupKey: controlGroup, icon: Activity, order: 5 },
  },
  {
    path: '/organization',
    scope: 'organization',
    titleKey: 'organization.dashboard.title',
    descriptionKey: 'organization.dashboard.description',
    placeholderKey: 'organization.dashboard.placeholder',
    nav: {
      labelKey: 'organization.dashboard.title',
      groupKey: overviewGroup,
      icon: LayoutDashboard,
      order: 1,
    },
  },
  {
    path: '/organization/profile',
    scope: 'organization',
    titleKey: 'organization.profile.title',
    descriptionKey: 'organization.profile.description',
    placeholderKey: 'organization.profile.placeholder',
    nav: {
      labelKey: 'organization.profile.title',
      groupKey: managementGroup,
      icon: User,
      order: 2,
    },
  },
  {
    path: '/organization/users',
    scope: 'organization',
    titleKey: 'organization.users.title',
    descriptionKey: 'organization.users.description',
    placeholderKey: 'organization.users.placeholder',
    nav: { labelKey: 'organization.users.title', groupKey: managementGroup, icon: Users, order: 3 },
  },
  {
    path: '/organization/roles',
    scope: 'organization',
    titleKey: 'organization.roles.title',
    descriptionKey: 'organization.roles.description',
    placeholderKey: 'organization.roles.placeholder',
    nav: {
      labelKey: 'organization.roles.title',
      groupKey: managementGroup,
      icon: Shield,
      order: 4,
    },
  },
  {
    path: '/organization/warehouses',
    scope: 'organization',
    titleKey: 'organization.warehouses.title',
    descriptionKey: 'organization.warehouses.description',
    placeholderKey: 'organization.warehouses.placeholder',
    nav: {
      labelKey: 'organization.warehouses.title',
      groupKey: managementGroup,
      icon: Warehouse,
      order: 5,
    },
  },
  {
    path: '/organization/warehouses/new',
    scope: 'organization',
    titleKey: 'organization.warehousesNew.title',
    descriptionKey: 'organization.warehousesNew.description',
    placeholderKey: 'organization.warehousesNew.placeholder',
    parentPath: '/organization/warehouses',
  },
  {
    path: '/organization/activity',
    scope: 'organization',
    titleKey: 'organization.activity.title',
    descriptionKey: 'organization.activity.description',
    placeholderKey: 'organization.activity.placeholder',
    nav: {
      labelKey: 'organization.activity.title',
      groupKey: controlGroup,
      icon: Activity,
      order: 6,
    },
  },
  {
    path: '/organization/settings',
    scope: 'organization',
    titleKey: 'organization.settings.title',
    descriptionKey: 'organization.settings.description',
    placeholderKey: 'organization.settings.placeholder',
    nav: {
      labelKey: 'organization.settings.title',
      groupKey: controlGroup,
      icon: Settings,
      order: 7,
    },
  },
  {
    path: '/warehouse/operations',
    scope: 'warehouse',
    titleKey: 'warehouse.operations.title',
    descriptionKey: 'warehouse.operations.description',
    placeholderKey: 'warehouse.operations.placeholder',
    nav: {
      labelKey: 'warehouse.operations.navLabel',
      groupKey: operationsGroup,
      icon: Package,
      order: 0,
    },
  },
  {
    path: '/warehouse',
    scope: 'warehouse',
    titleKey: 'warehouse.dashboard.title',
    descriptionKey: 'warehouse.dashboard.description',
    placeholderKey: 'warehouse.dashboard.placeholder',
    nav: {
      labelKey: 'warehouse.dashboard.title',
      groupKey: overviewGroup,
      icon: LayoutDashboard,
      order: 1,
    },
  },
  {
    path: '/warehouse/overview',
    scope: 'warehouse',
    titleKey: 'warehouse.overview.title',
    descriptionKey: 'warehouse.overview.description',
    placeholderKey: 'warehouse.overview.placeholder',
    nav: {
      labelKey: 'warehouse.overview.title',
      groupKey: overviewGroup,
      icon: BarChart3,
      order: 2,
    },
  },
  {
    path: '/warehouse/products',
    scope: 'warehouse',
    titleKey: 'warehouse.products.title',
    descriptionKey: 'warehouse.products.description',
    placeholderKey: 'warehouse.products.placeholder',
    nav: { labelKey: 'warehouse.products.title', groupKey: catalogGroup, icon: Package, order: 3 },
  },
  {
    path: '/warehouse/products/:id',
    scope: 'warehouse',
    titleKey: 'warehouse.productDetails.title',
    descriptionKey: 'warehouse.productDetails.description',
    placeholderKey: 'warehouse.productDetails.placeholder',
    parentPath: '/warehouse/products',
  },
  {
    path: '/warehouse/categories',
    scope: 'warehouse',
    titleKey: 'warehouse.categories.title',
    descriptionKey: 'warehouse.categories.description',
    placeholderKey: 'warehouse.categories.placeholder',
    nav: { labelKey: 'warehouse.categories.title', groupKey: catalogGroup, icon: Tags, order: 4 },
  },
  {
    path: '/warehouse/units',
    scope: 'warehouse',
    titleKey: 'warehouse.units.title',
    descriptionKey: 'warehouse.units.description',
    placeholderKey: 'warehouse.units.placeholder',
    nav: { labelKey: 'warehouse.units.title', groupKey: catalogGroup, icon: Ruler, order: 5 },
  },
  {
    path: '/warehouse/suppliers',
    scope: 'warehouse',
    titleKey: 'warehouse.suppliers.title',
    descriptionKey: 'warehouse.suppliers.description',
    placeholderKey: 'warehouse.suppliers.placeholder',
    nav: { labelKey: 'warehouse.suppliers.title', groupKey: catalogGroup, icon: Truck, order: 6 },
  },
  {
    path: '/warehouse/stock',
    scope: 'warehouse',
    titleKey: 'warehouse.stock.title',
    descriptionKey: 'warehouse.stock.description',
    placeholderKey: 'warehouse.stock.placeholder',
    nav: { labelKey: 'warehouse.stock.title', groupKey: operationsGroup, icon: Boxes, order: 7 },
  },
  {
    path: '/warehouse/incoming',
    scope: 'warehouse',
    titleKey: 'warehouse.incoming.title',
    descriptionKey: 'warehouse.incoming.description',
    placeholderKey: 'warehouse.incoming.placeholder',
    nav: {
      labelKey: 'warehouse.incoming.title',
      groupKey: operationsGroup,
      icon: ArrowDownToLine,
      order: 8,
    },
  },
  {
    path: '/warehouse/outgoing',
    scope: 'warehouse',
    titleKey: 'warehouse.outgoing.title',
    descriptionKey: 'warehouse.outgoing.description',
    placeholderKey: 'warehouse.outgoing.placeholder',
    nav: {
      labelKey: 'warehouse.outgoing.title',
      groupKey: operationsGroup,
      icon: ArrowUpFromLine,
      order: 9,
    },
  },
  {
    path: '/warehouse/movements',
    scope: 'warehouse',
    titleKey: 'warehouse.movements.title',
    descriptionKey: 'warehouse.movements.description',
    placeholderKey: 'warehouse.movements.placeholder',
    nav: {
      labelKey: 'warehouse.movements.title',
      groupKey: operationsGroup,
      icon: History,
      order: 10,
    },
  },
  {
    path: '/warehouse/adjustments',
    scope: 'warehouse',
    titleKey: 'warehouse.adjustments.title',
    descriptionKey: 'warehouse.adjustments.description',
    placeholderKey: 'warehouse.adjustments.placeholder',
    nav: {
      labelKey: 'warehouse.adjustments.title',
      groupKey: operationsGroup,
      icon: ClipboardList,
      order: 11,
    },
  },
  {
    path: '/warehouse/stocktaking',
    scope: 'warehouse',
    titleKey: 'warehouse.stocktaking.title',
    descriptionKey: 'warehouse.stocktaking.description',
    placeholderKey: 'warehouse.stocktaking.placeholder',
    nav: {
      labelKey: 'warehouse.stocktaking.title',
      groupKey: controlGroup,
      icon: ClipboardCheck,
      order: 12,
    },
  },
  {
    path: '/warehouse/alerts',
    scope: 'warehouse',
    titleKey: 'warehouse.alerts.title',
    descriptionKey: 'warehouse.alerts.description',
    placeholderKey: 'warehouse.alerts.placeholder',
    nav: {
      labelKey: 'warehouse.alerts.title',
      groupKey: controlGroup,
      icon: AlertTriangle,
      order: 13,
    },
  },
  {
    path: '/warehouse/expiration',
    scope: 'warehouse',
    titleKey: 'warehouse.expiration.title',
    descriptionKey: 'warehouse.expiration.description',
    placeholderKey: 'warehouse.expiration.placeholder',
    nav: {
      labelKey: 'warehouse.expiration.title',
      groupKey: controlGroup,
      icon: CalendarClock,
      order: 14,
    },
  },
  {
    path: '/warehouse/reports',
    scope: 'warehouse',
    titleKey: 'warehouse.reports.title',
    descriptionKey: 'warehouse.reports.description',
    placeholderKey: 'warehouse.reports.placeholder',
    nav: {
      labelKey: 'warehouse.reports.title',
      groupKey: controlGroup,
      icon: BarChart3,
      order: 15,
    },
  },
  {
    path: '/warehouse/employees',
    scope: 'warehouse',
    titleKey: 'warehouse.employees.title',
    descriptionKey: 'warehouse.employees.description',
    placeholderKey: 'warehouse.employees.placeholder',
    nav: {
      labelKey: 'warehouse.employees.title',
      groupKey: managementGroup,
      icon: Users,
      order: 16,
    },
  },
  {
    path: '/warehouse/settings',
    scope: 'warehouse',
    titleKey: 'warehouse.settings.title',
    descriptionKey: 'warehouse.settings.description',
    placeholderKey: 'warehouse.settings.placeholder',
    nav: {
      labelKey: 'warehouse.settings.title',
      groupKey: controlGroup,
      icon: Settings,
      order: 17,
    },
  },
];

function toNavigationItem(route: RouteMeta): NavigationItem | null {
  if (!route.nav) {
    return null;
  }
  return {
    path: route.path,
    scope: route.scope,
    labelKey: route.nav.labelKey,
    groupKey: route.nav.groupKey,
    icon: route.nav.icon,
    order: route.nav.order,
  };
}

function navigationForScope(scope: AppContextScope): NavigationItem[] {
  return routeMeta
    .filter((route) => route.scope === scope)
    .map(toNavigationItem)
    .filter((item): item is NavigationItem => item !== null)
    .sort((a, b) => a.order - b.order);
}

export const navigation: Record<AppContextScope, NavigationItem[]> = {
  platform: navigationForScope('platform'),
  organization: navigationForScope('organization'),
  warehouse: navigationForScope('warehouse'),
};

export function contextRootPath(scope: AppContextScope): string {
  return scope === 'platform'
    ? '/platform'
    : scope === 'organization'
      ? '/organization'
      : '/warehouse';
}

export function contextLabelKey(scope: AppContextScope): TranslationKey {
  return scope === 'platform'
    ? 'common.context.platform'
    : scope === 'organization'
      ? 'common.context.organization'
      : 'common.context.warehouse';
}

export function getContextForPath(pathname: string): AppContextScope {
  const clean = pathname.split(/[?#]/)[0] ?? '';
  if (clean.startsWith('/organization')) {
    return 'organization';
  }
  if (clean.startsWith('/warehouse')) {
    return 'warehouse';
  }
  return 'platform';
}

function pathSegments(path: string): string[] {
  return path.split(/[?#]/)[0]?.split('/').filter(Boolean) ?? [];
}

function matchesRoute(candidate: string, routePath: string): boolean {
  const candidateSegments = pathSegments(candidate);
  const routeSegments = pathSegments(routePath);
  if (candidateSegments.length !== routeSegments.length) {
    return false;
  }
  return routeSegments.every((segment, index) => {
    const actual = candidateSegments[index];
    return segment.startsWith(':') || segment === actual;
  });
}

export function findRouteMeta(pathname: string): RouteMeta | undefined {
  return routeMeta.find((route) => matchesRoute(pathname, route.path));
}

export function resolveRouteMeta(pathname: string): RouteMeta {
  return (
    findRouteMeta(pathname) ?? {
      path: '/platform',
      scope: 'platform',
      titleKey: 'platform.dashboard.title',
      descriptionKey: 'platform.dashboard.description',
      placeholderKey: 'platform.dashboard.placeholder',
    }
  );
}

export function getBreadcrumbTrail(pathname: string): BreadcrumbItem[] {
  const current = resolveRouteMeta(pathname);
  const rootPath = contextRootPath(current.scope);
  const trail: BreadcrumbItem[] = [
    {
      id: 'scope-root',
      labelKey: contextLabelKey(current.scope),
      to: rootPath,
    },
  ];

  if (current.path === rootPath) {
    return trail;
  }

  const ancestors: RouteMeta[] = [];
  let parentPath = current.parentPath;
  const visited = new Set<string>([current.path]);

  while (parentPath && !visited.has(parentPath)) {
    visited.add(parentPath);
    const parent = findRouteMeta(parentPath);
    if (!parent) {
      break;
    }
    if (parent.path !== rootPath) {
      ancestors.unshift(parent);
    }
    parentPath = parent.parentPath;
  }

  for (const ancestor of ancestors) {
    trail.push({
      id: ancestor.path,
      labelKey: ancestor.nav?.labelKey ?? ancestor.titleKey,
      to: ancestor.path.includes(':') ? undefined : ancestor.path,
    });
  }

  trail.push({
    id: current.path,
    labelKey: current.nav?.labelKey ?? current.titleKey,
  });

  return trail;
}
