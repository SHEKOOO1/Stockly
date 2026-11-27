import { useEffect, useMemo, useRef } from 'react';
import { Link } from 'react-router-dom';
import { useApp } from '@/context/AppContext';
import { useDemoSession } from '@/context/DemoSessionContext';
import { demoOrganizations, demoWarehouses } from '@/data/demoData';
import { t } from '@/i18n';

export function ProfileMenu() {
  const { profileMenuOpen, toggleProfileMenu, setProfileMenuOpen } = useApp();
  const { session, switchUser, signOut } = useDemoSession();
  const containerRef = useRef<HTMLDivElement>(null);

  const organizationNames = useMemo(
    () =>
      session?.user.organizationIds
        .map((id) => demoOrganizations.find((organization) => organization.id === id)?.name)
        .filter(Boolean)
        .join('، ') ?? '',
    [session],
  );
  const warehouseNames = useMemo(
    () =>
      session?.user.warehouseIds
        .map((id) => demoWarehouses.find((warehouse) => warehouse.id === id)?.name)
        .filter(Boolean)
        .join('، ') ?? '',
    [session],
  );

  useEffect(() => {
    if (!profileMenuOpen) {
      return;
    }
    function handlePointerDown(event: MouseEvent) {
      if (containerRef.current && !containerRef.current.contains(event.target as Node)) {
        setProfileMenuOpen(false);
      }
    }
    document.addEventListener('mousedown', handlePointerDown);
    return () => document.removeEventListener('mousedown', handlePointerDown);
  }, [profileMenuOpen, setProfileMenuOpen]);

  if (!session) {
    return null;
  }

  return (
    <div className="topbar-menu" ref={containerRef}>
      <button
        type="button"
        className="profile-trigger"
        onClick={toggleProfileMenu}
        aria-label={t('common.profile.title')}
        aria-expanded={profileMenuOpen}
      >
        <span className="profile-trigger__avatar" dir="ltr" aria-hidden="true">
          {session.user.name.slice(0, 1)}
        </span>
        <span className="profile-trigger__name">{session.user.name}</span>
      </button>
      {profileMenuOpen ? (
        <div className="menu-panel menu-panel--profile">
          <div className="menu-panel__header">
            <strong>{t('common.profile.title')}</strong>
          </div>
          <div className="menu-panel__body">
            <div className="profile-summary">
              <span className="profile-summary__avatar" dir="ltr" aria-hidden="true">
                {session.user.name.slice(0, 1)}
              </span>
              <div>
                <strong>{session.user.name}</strong>
                <span>
                  {t('common.profile.role')}: {session.user.role}
                </span>
              </div>
            </div>
            <div className="profile-detail">
              <span>{t('common.context.organization')}</span>
              <strong>{organizationNames}</strong>
            </div>
            <div className="profile-detail">
              <span>{t('common.context.warehouse')}</span>
              <strong>{warehouseNames}</strong>
            </div>
            <div className="profile-detail">
              <span>{t('common.profile.currentSession')}</span>
              <strong>{t('common.demo.previewOnly')}</strong>
            </div>
            <Link
              to="/organization/profile"
              className="menu-link"
              onClick={() => setProfileMenuOpen(false)}
            >
              {t('organization.profile.title')}
            </Link>
            <button type="button" className="menu-link menu-link--button" onClick={switchUser}>
              {t('common.profile.switchUser')}
            </button>
            <button type="button" className="menu-link menu-link--button" onClick={signOut}>
              {t('common.profile.signOut')}
            </button>
          </div>
          <div className="menu-panel__footer">{t('common.profile.demoIdentity')}</div>
        </div>
      ) : null}
    </div>
  );
}

export default ProfileMenu;
