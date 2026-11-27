import { ChevronLeft, Info } from 'lucide-react';
import { Link } from 'react-router-dom';
import { useApp } from '@/context/AppContext';
import { useDemoSession } from '@/context/DemoSessionContext';
import { t } from '@/i18n';
import { visibleScopesForUser } from '@/lib/demoAccess';
import { contextLabelKey, contextRootPath } from '@/navigation';
import { ContextSelectors } from './ContextSelectors';

export function ContextRail() {
  const { activeScope } = useApp();
  const { session } = useDemoSession();
  const visibleScopes = session ? visibleScopesForUser(session.user) : [];

  return (
    <div className="context-rail">
      <div className="context-rail__levels">
        {visibleScopes.map((scope, index) => (
          <div className="context-rail__segment" key={scope}>
            {index > 0 ? (
              <ChevronLeft className="context-rail__arrow" size={14} aria-hidden="true" />
            ) : null}
            <Link
              to={contextRootPath(scope)}
              className={`context-rail__level ${activeScope === scope ? 'is-active' : ''}`.trim()}
              aria-current={activeScope === scope ? 'page' : undefined}
            >
              {t(contextLabelKey(scope))}
            </Link>
          </div>
        ))}
      </div>
      <ContextSelectors />
      <span className="demo-badge">
        <Info size={13} aria-hidden="true" />
        {t('common.demo.badge')}
      </span>
    </div>
  );
}
