import { Menu, Search } from 'lucide-react';
import { useApp } from '@/context/AppContext';
import { t, type TranslationKey } from '@/i18n';
import { useMediaQuery } from '@/lib/useMediaQuery';
import { Breadcrumbs } from './Breadcrumbs';
import { ContextRail } from './ContextRail';
import { NotificationsMenu } from './NotificationsMenu';
import { ProfileMenu } from './ProfileMenu';

export function Topbar() {
  const {
    mobileNavOpen,
    sidebarCollapsed,
    toggleMobileNav,
    toggleSidebarCollapsed,
    toggleCommandPalette,
  } = useApp();
  const isMobile = useMediaQuery('(max-width: 960px)');
  const toggleNavigation = isMobile ? toggleMobileNav : toggleSidebarCollapsed;
  const navigationExpanded = isMobile ? mobileNavOpen : !sidebarCollapsed;
  const toggleLabelKey: TranslationKey = isMobile
    ? mobileNavOpen
      ? 'common.actions.closeNavToggle'
      : 'common.actions.openNav'
    : sidebarCollapsed
      ? 'common.actions.expandNav'
      : 'common.actions.collapseNav';

  return (
    <header className="topbar">
      <div className="topbar__row">
        <div className="topbar__start">
          <button
            type="button"
            data-nav-toggle
            className="icon-button topbar__menu-toggle"
            onClick={toggleNavigation}
            aria-label={t(toggleLabelKey)}
            aria-controls="app-sidebar"
            aria-expanded={navigationExpanded}
          >
            <Menu size={20} aria-hidden="true" />
          </button>
          <Breadcrumbs />
        </div>
        <div className="topbar__actions">
          <button
            type="button"
            className="topbar-search"
            onClick={toggleCommandPalette}
            aria-label={t('common.actions.openSearch')}
          >
            <Search size={16} aria-hidden="true" />
            <span>{t('common.search.placeholder')}</span>
          </button>
          <NotificationsMenu />
          <ProfileMenu />
        </div>
      </div>
      <ContextRail />
    </header>
  );
}
