import { ChevronLeft, Info } from 'lucide-react';
import { Link } from 'react-router-dom';
import { useApp } from '@/context/AppContext';
import { t } from '@/i18n';
import { contextLabelKey, contextRootPath } from '@/navigation';
import type { AppContextScope } from '@/types/domain';
import { ContextSelectors } from './ContextSelectors';

const scopeOrder: AppContextScope[] = ['platform', 'organization', 'warehouse'];

export function ContextRail() {
  const { activeScope } = useApp();

  return (
    <div className="context-rail">
      <div className="context-rail__levels">
        {scopeOrder.map((scope, index) => (
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
