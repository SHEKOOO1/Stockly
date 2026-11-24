import { useEffect, useRef } from 'react';
import { Link } from 'react-router-dom';
import { useApp } from '@/context/AppContext';
import { demoProfile } from '@/data/demoData';
import { t } from '@/i18n';

export function ProfileMenu() {
  const { profileMenuOpen, toggleProfileMenu, setProfileMenuOpen } = useApp();
  const containerRef = useRef<HTMLDivElement>(null);

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
          {demoProfile.name.slice(0, 1)}
        </span>
        <span className="profile-trigger__name">{demoProfile.name}</span>
      </button>
      {profileMenuOpen ? (
        <div className="menu-panel menu-panel--profile">
          <div className="menu-panel__header">
            <strong>{t('common.profile.title')}</strong>
          </div>
          <div className="menu-panel__body">
            <div className="profile-summary">
              <span className="profile-summary__avatar" dir="ltr" aria-hidden="true">
                {demoProfile.name.slice(0, 1)}
              </span>
              <div>
                <strong>{demoProfile.name}</strong>
                <span>
                  {t('common.profile.role')}: {demoProfile.roleLabel}
                </span>
              </div>
            </div>
            <div className="profile-detail">
              <span>{t('common.context.organization')}</span>
              <strong>{demoProfile.organizationName}</strong>
            </div>
            <div className="profile-detail">
              <span>{t('common.context.warehouse')}</span>
              <strong>{demoProfile.warehouseName}</strong>
            </div>
            <Link
              to="/organization/profile"
              className="menu-link"
              onClick={() => setProfileMenuOpen(false)}
            >
              {t('organization.profile.title')}
            </Link>
          </div>
          <div className="menu-panel__footer">{t('common.profile.demoIdentity')}</div>
        </div>
      ) : null}
    </div>
  );
}
