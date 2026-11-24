import { Boxes, X } from 'lucide-react';
import { useEffect, useMemo, useRef } from 'react';
import { NavLink } from 'react-router-dom';
import { useApp } from '@/context/AppContext';
import { t, type TranslationKey } from '@/i18n';
import { useMediaQuery } from '@/lib/useMediaQuery';
import { contextLabelKey, contextRootPath, navigation, type NavigationItem } from '@/navigation';
import type { AppContextScope } from '@/types/domain';

interface NavigationGroup {
  groupKey: TranslationKey;
  items: NavigationItem[];
}

const scopeOrder: AppContextScope[] = ['platform', 'organization', 'warehouse'];

export function Sidebar() {
  const {
    activeScope,
    mobileNavOpen,
    setMobileNavOpen,
    selectedOrganization,
    selectedWarehouse,
    sidebarCollapsed,
  } = useApp();
  const sidebarRef = useRef<HTMLElement>(null);
  const isMobile = useMediaQuery('(max-width: 960px)');
  const drawerHidden = isMobile && !mobileNavOpen;

  const groups = useMemo<NavigationGroup[]>(() => {
    const result: NavigationGroup[] = [];
    for (const item of navigation[activeScope]) {
      const existing = result.find((group) => group.groupKey === item.groupKey);
      if (existing) {
        existing.items.push(item);
      } else {
        result.push({ groupKey: item.groupKey, items: [item] });
      }
    }
    return result;
  }, [activeScope]);

  useEffect(() => {
    if (!mobileNavOpen) {
      return;
    }

    function handleKeyDown(event: KeyboardEvent) {
      if (event.key === 'Escape') {
        setMobileNavOpen(false);
      }
    }

    function handleMouseDown(event: MouseEvent) {
      const target = event.target;
      if (target instanceof Node && sidebarRef.current?.contains(target)) {
        return;
      }
      if (target instanceof Element && target.closest('[data-nav-toggle]')) {
        return;
      }
      setMobileNavOpen(false);
    }

    window.addEventListener('keydown', handleKeyDown);
    document.addEventListener('mousedown', handleMouseDown);
    return () => {
      window.removeEventListener('keydown', handleKeyDown);
      document.removeEventListener('mousedown', handleMouseDown);
    };
  }, [mobileNavOpen, setMobileNavOpen]);

  return (
    <>
      {isMobile && mobileNavOpen ? (
        <button
          type="button"
          data-sidebar-overlay
          className="sidebar-overlay is-visible"
          onClick={() => setMobileNavOpen(false)}
          aria-label={t('common.actions.closeNavOverlay')}
        />
      ) : null}
      <aside
        ref={sidebarRef}
        id="app-sidebar"
        hidden={drawerHidden}
        aria-hidden={drawerHidden}
        className={`sidebar ${mobileNavOpen ? 'is-open' : ''} ${
          sidebarCollapsed ? 'is-collapsed' : ''
        }`.trim()}
        aria-label={t('common.nav.title')}
      >
        <div className="sidebar__header">
          <span className="sidebar__mark" aria-hidden="true">
            <Boxes size={20} />
          </span>
          <div className="sidebar__brand">
            <strong>{t('common.appName')}</strong>
            <span>{t('common.tagline')}</span>
          </div>
          <button
            type="button"
            className="icon-button sidebar__close"
            onClick={() => setMobileNavOpen(false)}
            aria-label={t('common.actions.closeNavDrawer')}
          >
            <X size={18} aria-hidden="true" />
          </button>
        </div>

        <div className="sidebar__contexts" aria-label={t('common.context.current')}>
          {scopeOrder.map((scope) => (
            <NavLink
              key={scope}
              to={contextRootPath(scope)}
              end
              className={({ isActive }) =>
                `sidebar__context ${activeScope === scope || isActive ? 'is-active' : ''}`.trim()
              }
              aria-label={t(contextLabelKey(scope))}
              onClick={() => setMobileNavOpen(false)}
            >
              <span className="sidebar__context-text">{t(contextLabelKey(scope))}</span>
            </NavLink>
          ))}
        </div>

        <div className="sidebar__selection">
          <span className="sidebar__selection-label">{t('common.context.organization')}</span>
          <strong className="sidebar__selection-value">{selectedOrganization.name}</strong>
          <span className="sidebar__selection-label">{t('common.context.warehouse')}</span>
          <strong className="sidebar__selection-value">{selectedWarehouse.name}</strong>
          <span className="sidebar__selection-note">{t('common.demo.source')}</span>
        </div>

        <nav className="sidebar__nav" aria-label={t('common.nav.title')}>
          {groups.map((group) => (
            <section className="sidebar__group" key={group.groupKey}>
              <h2 className="sidebar__group-title">{t(group.groupKey)}</h2>
              <ul className="sidebar__list">
                {group.items.map((item) => {
                  const Icon = item.icon;
                  const isRoot = item.path === contextRootPath(item.scope);
                  const label = t(item.labelKey);
                  return (
                    <li key={item.path}>
                      <NavLink
                        to={item.path}
                        end={isRoot}
                        className={({ isActive }) =>
                          `sidebar__link ${isActive ? 'is-active' : ''}`.trim()
                        }
                        aria-label={label}
                        title={label}
                        onClick={() => setMobileNavOpen(false)}
                      >
                        <Icon size={17} aria-hidden="true" />
                        <span className="sidebar__link-text">{label}</span>
                      </NavLink>
                    </li>
                  );
                })}
              </ul>
            </section>
          ))}
        </nav>
        <div className="sidebar__footer">{t('common.demo.previewOnly')}</div>
      </aside>
    </>
  );
}
