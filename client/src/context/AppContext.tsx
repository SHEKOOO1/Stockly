import {
  createContext,
  useCallback,
  useContext,
  useMemo,
  useRef,
  useState,
  type ReactNode,
} from 'react';
import { demoNotifications, demoOrganizations, demoWarehouses } from '@/data/demoData';
import type { TranslationKey, TranslationValues } from '@/i18n';
import type {
  AppContextScope,
  DemoNotification,
  Organization,
  StatusTone,
  Warehouse,
} from '@/types/domain';

export interface ToastMessage {
  id: string;
  messageKey: TranslationKey;
  values?: TranslationValues;
  tone: StatusTone;
}

export interface AppContextValue {
  organizations: readonly Organization[];
  warehouses: readonly Warehouse[];
  availableWarehouses: readonly Warehouse[];
  selectedOrganization: Organization;
  selectedWarehouse: Warehouse;
  selectedOrganizationId: string;
  selectedWarehouseId: string;
  selectOrganization: (organizationId: string) => void;
  selectWarehouse: (warehouseId: string) => void;
  activeScope: AppContextScope;
  setActiveScope: (scope: AppContextScope) => void;
  notifications: readonly DemoNotification[];
  unreadNotificationCount: number;
  markNotificationRead: (id: string) => void;
  markAllNotificationsRead: () => void;
  dismissNotification: (id: string) => void;
  toasts: readonly ToastMessage[];
  showToast: (messageKey: TranslationKey, tone?: StatusTone, values?: TranslationValues) => void;
  dismissToast: (id: string) => void;
  mobileNavOpen: boolean;
  setMobileNavOpen: (open: boolean) => void;
  toggleMobileNav: () => void;
  sidebarCollapsed: boolean;
  setSidebarCollapsed: (collapsed: boolean) => void;
  toggleSidebarCollapsed: () => void;
  commandPaletteOpen: boolean;
  setCommandPaletteOpen: (open: boolean) => void;
  toggleCommandPalette: () => void;
  notificationsOpen: boolean;
  setNotificationsOpen: (open: boolean) => void;
  toggleNotifications: () => void;
  profileMenuOpen: boolean;
  setProfileMenuOpen: (open: boolean) => void;
  toggleProfileMenu: () => void;
}

const AppContext = createContext<AppContextValue | null>(null);

export function AppProvider({ children }: { children: ReactNode }) {
  const [selectedOrganizationId, setSelectedOrganizationId] = useState(
    demoOrganizations[0]?.id ?? '',
  );
  const [selectedWarehouseId, setSelectedWarehouseId] = useState(demoWarehouses[0]?.id ?? '');
  const [activeScope, setActiveScope] = useState<AppContextScope>('platform');
  const [notifications, setNotifications] = useState<DemoNotification[]>([...demoNotifications]);
  const [toasts, setToasts] = useState<ToastMessage[]>([]);
  const [mobileNavOpen, setMobileNavOpen] = useState(false);
  const [sidebarCollapsed, setSidebarCollapsed] = useState(false);
  const [commandPaletteOpen, setCommandPaletteOpen] = useState(false);
  const [notificationsOpen, setNotificationsOpen] = useState(false);
  const [profileMenuOpen, setProfileMenuOpen] = useState(false);
  const toastCounter = useRef(0);

  const showToast = useCallback(
    (messageKey: TranslationKey, tone: StatusTone = 'info', values?: TranslationValues) => {
      toastCounter.current += 1;
      const id = `toast-${toastCounter.current}`;
      setToasts((current) => [...current, { id, messageKey, tone, values }]);
    },
    [],
  );

  const dismissToast = useCallback((id: string) => {
    setToasts((current) => current.filter((toast) => toast.id !== id));
  }, []);

  const selectOrganization = useCallback(
    (organizationId: string) => {
      const organizationExists = demoOrganizations.some((item) => item.id === organizationId);
      if (!organizationExists) {
        return;
      }
      const firstWarehouse = demoWarehouses.find((item) => item.organizationId === organizationId);
      setSelectedOrganizationId(organizationId);
      setSelectedWarehouseId((current) => {
        const currentWarehouse = demoWarehouses.find((item) => item.id === current);
        return currentWarehouse?.organizationId === organizationId
          ? current
          : (firstWarehouse?.id ?? current);
      });
      showToast('common.context.switchOrganization', 'info');
    },
    [showToast],
  );

  const selectWarehouse = useCallback(
    (warehouseId: string) => {
      const warehouse = demoWarehouses.find((item) => item.id === warehouseId);
      if (!warehouse || warehouse.organizationId !== selectedOrganizationId) {
        return;
      }
      setSelectedWarehouseId(warehouseId);
      showToast('common.context.switchWarehouse', 'info');
    },
    [selectedOrganizationId, showToast],
  );

  const availableWarehouses = useMemo(
    () => demoWarehouses.filter((item) => item.organizationId === selectedOrganizationId),
    [selectedOrganizationId],
  );

  const selectedOrganization = useMemo(
    () =>
      demoOrganizations.find((item) => item.id === selectedOrganizationId) ?? demoOrganizations[0],
    [selectedOrganizationId],
  );

  const selectedWarehouse = useMemo(
    () =>
      availableWarehouses.find((item) => item.id === selectedWarehouseId) ??
      availableWarehouses[0] ??
      demoWarehouses[0],
    [availableWarehouses, selectedWarehouseId],
  );

  const unreadNotificationCount = useMemo(
    () => notifications.filter((item) => !item.read).length,
    [notifications],
  );

  const markNotificationRead = useCallback((id: string) => {
    setNotifications((current) =>
      current.map((item) => (item.id === id ? { ...item, read: true } : item)),
    );
  }, []);

  const markAllNotificationsRead = useCallback(() => {
    setNotifications((current) => current.map((item) => ({ ...item, read: true })));
  }, []);

  const dismissNotification = useCallback((id: string) => {
    setNotifications((current) => current.filter((item) => item.id !== id));
  }, []);

  const toggleMobileNav = useCallback(() => setMobileNavOpen((open) => !open), []);
  const toggleSidebarCollapsed = useCallback(
    () => setSidebarCollapsed((collapsed) => !collapsed),
    [],
  );
  const toggleCommandPalette = useCallback(() => setCommandPaletteOpen((open) => !open), []);
  const toggleNotifications = useCallback(() => setNotificationsOpen((open) => !open), []);
  const toggleProfileMenu = useCallback(() => setProfileMenuOpen((open) => !open), []);

  const value = useMemo<AppContextValue>(
    () => ({
      organizations: demoOrganizations,
      warehouses: demoWarehouses,
      availableWarehouses,
      selectedOrganization,
      selectedWarehouse,
      selectedOrganizationId,
      selectedWarehouseId,
      selectOrganization,
      selectWarehouse,
      activeScope,
      setActiveScope,
      notifications,
      unreadNotificationCount,
      markNotificationRead,
      markAllNotificationsRead,
      dismissNotification,
      toasts,
      showToast,
      dismissToast,
      mobileNavOpen,
      setMobileNavOpen,
      toggleMobileNav,
      sidebarCollapsed,
      setSidebarCollapsed,
      toggleSidebarCollapsed,
      commandPaletteOpen,
      setCommandPaletteOpen,
      toggleCommandPalette,
      notificationsOpen,
      setNotificationsOpen,
      toggleNotifications,
      profileMenuOpen,
      setProfileMenuOpen,
      toggleProfileMenu,
    }),
    [
      availableWarehouses,
      selectedOrganization,
      selectedWarehouse,
      selectedOrganizationId,
      selectedWarehouseId,
      selectOrganization,
      selectWarehouse,
      activeScope,
      notifications,
      unreadNotificationCount,
      markNotificationRead,
      markAllNotificationsRead,
      dismissNotification,
      toasts,
      showToast,
      dismissToast,
      mobileNavOpen,
      toggleMobileNav,
      sidebarCollapsed,
      toggleSidebarCollapsed,
      commandPaletteOpen,
      toggleCommandPalette,
      notificationsOpen,
      toggleNotifications,
      profileMenuOpen,
      toggleProfileMenu,
    ],
  );

  return <AppContext.Provider value={value}>{children}</AppContext.Provider>;
}

export function useApp(): AppContextValue {
  const value = useContext(AppContext);
  if (!value) {
    throw new Error('useApp must be used within AppProvider');
  }
  return value;
}
