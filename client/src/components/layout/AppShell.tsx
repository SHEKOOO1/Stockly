import { useEffect, type ReactNode } from 'react';
import { useLocation } from 'react-router-dom';
import { useApp } from '@/context/AppContext';
import { t } from '@/i18n';
import { getContextForPath } from '@/navigation';
import { CommandPalette } from './CommandPalette';
import { Sidebar } from './Sidebar';
import { Topbar } from './Topbar';
import { ToastRegion } from '@/components/ui/ToastRegion';

export interface AppShellProps {
  children: ReactNode;
}

export function AppShell({ children }: AppShellProps) {
  const { pathname } = useLocation();
  const {
    setActiveScope,
    setMobileNavOpen,
    setNotificationsOpen,
    setProfileMenuOpen,
    sidebarCollapsed,
  } = useApp();

  useEffect(() => {
    setActiveScope(getContextForPath(pathname));
    setMobileNavOpen(false);
    setNotificationsOpen(false);
    setProfileMenuOpen(false);
  }, [pathname, setActiveScope, setMobileNavOpen, setNotificationsOpen, setProfileMenuOpen]);

  return (
    <div className={`app-shell ${sidebarCollapsed ? 'is-sidebar-collapsed' : ''}`.trim()}>
      <a className="skip-link" href="#main-content">
        {t('common.nav.title')}
      </a>
      <Sidebar />
      <div className="app-shell__main">
        <Topbar />
        <main id="main-content" className="app-shell__content" tabIndex={-1}>
          {children}
        </main>
        <footer className="app-shell__footer">
          <span>{t('common.brandName')}</span>
          <span>{t('common.demo.source')}</span>
        </footer>
      </div>
      <CommandPalette />
      <ToastRegion />
    </div>
  );
}
