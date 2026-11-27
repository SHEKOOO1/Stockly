import { Bell, CheckCheck } from 'lucide-react';
import { useEffect, useRef } from 'react';
import { useApp } from '@/context/AppContext';
import { useDemoSession } from '@/context/DemoSessionContext';
import { t } from '@/i18n';
import { formatDateTime } from '@/lib/format';
import { EmptyState } from '@/components/ui/EmptyState';

export function NotificationsMenu() {
  const {
    notifications,
    notificationsOpen,
    toggleNotifications,
    setNotificationsOpen,
    markNotificationRead,
    markAllNotificationsRead,
  } = useApp();
  const { session } = useDemoSession();
  const containerRef = useRef<HTMLDivElement>(null);
  const visibleNotifications = notifications.filter(
    (notification) =>
      !notification.audienceRoleCode || notification.audienceRoleCode === session?.user.roleCode,
  );
  const visibleUnreadCount = visibleNotifications.filter((item) => !item.read).length;

  useEffect(() => {
    if (!notificationsOpen) {
      return;
    }
    function handlePointerDown(event: MouseEvent) {
      if (containerRef.current && !containerRef.current.contains(event.target as Node)) {
        setNotificationsOpen(false);
      }
    }
    document.addEventListener('mousedown', handlePointerDown);
    return () => document.removeEventListener('mousedown', handlePointerDown);
  }, [notificationsOpen, setNotificationsOpen]);

  return (
    <div className="topbar-menu" ref={containerRef}>
      <button
        type="button"
        className="icon-button icon-button--badge"
        onClick={toggleNotifications}
        aria-label={t('common.notifications.title')}
        aria-expanded={notificationsOpen}
      >
        <Bell size={18} aria-hidden="true" />
        {visibleUnreadCount > 0 ? (
          <span className="icon-button__count" dir="ltr">
            {visibleUnreadCount}
          </span>
        ) : null}
      </button>
      {notificationsOpen ? (
        <div className="menu-panel menu-panel--notifications">
          <div className="menu-panel__header">
            <strong>{t('common.notifications.title')}</strong>
            <button
              type="button"
              className="button button--quiet button--xs"
              onClick={markAllNotificationsRead}
              disabled={visibleUnreadCount === 0}
            >
              <CheckCheck size={14} aria-hidden="true" />
              {t('common.notifications.markAllRead')}
            </button>
          </div>
          <div className="menu-panel__body">
            {visibleNotifications.length === 0 ? (
              <EmptyState />
            ) : (
              visibleNotifications.map((notification) => (
                <button
                  key={notification.id}
                  type="button"
                  className={`notification-item ${notification.read ? '' : 'is-unread'}`.trim()}
                  onClick={() => markNotificationRead(notification.id)}
                >
                  <span
                    className={`notification-item__dot notification-item__dot--${notification.tone}`}
                  />
                  <span className="notification-item__content">
                    <strong>{notification.title}</strong>
                    <span>{notification.body}</span>
                    <time dir="ltr" dateTime={notification.createdAt}>
                      {formatDateTime(notification.createdAt)}
                    </time>
                  </span>
                </button>
              ))
            )}
          </div>
          <div className="menu-panel__footer">{t('common.notifications.demoTip')}</div>
        </div>
      ) : null}
    </div>
  );
}
