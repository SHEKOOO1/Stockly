import { ChevronLeft } from 'lucide-react';
import { Link, useLocation } from 'react-router-dom';
import { t } from '@/i18n';
import { getBreadcrumbTrail } from '@/navigation';

export function Breadcrumbs() {
  const { pathname } = useLocation();
  const trail = getBreadcrumbTrail(pathname);

  return (
    <nav className="breadcrumbs" aria-label={t('common.nav.title')}>
      <ol className="breadcrumbs__list">
        {trail.map((item, index) => {
          const isLast = index === trail.length - 1;
          return (
            <li className="breadcrumbs__item" key={item.id}>
              {index > 0 ? (
                <ChevronLeft className="breadcrumbs__separator" size={13} aria-hidden="true" />
              ) : null}
              {item.to && !isLast ? (
                <Link to={item.to} className="breadcrumbs__link">
                  {t(item.labelKey)}
                </Link>
              ) : (
                <span
                  className={isLast ? 'breadcrumbs__current' : 'breadcrumbs__label'}
                  aria-current={isLast ? 'page' : undefined}
                >
                  {t(item.labelKey)}
                </span>
              )}
            </li>
          );
        })}
      </ol>
    </nav>
  );
}
