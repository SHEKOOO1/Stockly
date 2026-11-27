import { CornerDownLeft, Search } from 'lucide-react';
import { useMemo, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useApp } from '@/context/AppContext';
import { useDemoSession } from '@/context/DemoSessionContext';
import { t } from '@/i18n';
import { visibleNavigationForUser } from '@/lib/demoAccess';
import type { NavigationItem } from '@/navigation';
import { EmptyState } from '@/components/ui/EmptyState';
import { Modal } from '@/components/ui/Modal';

export function CommandPalette() {
  const { commandPaletteOpen, setCommandPaletteOpen } = useApp();
  const { session } = useDemoSession();
  const [query, setQuery] = useState('');
  const navigate = useNavigate();

  const allItems = useMemo<NavigationItem[]>(() => {
    if (!session) {
      return [];
    }
    return [
      ...visibleNavigationForUser('platform', session.user),
      ...visibleNavigationForUser('organization', session.user),
      ...visibleNavigationForUser('warehouse', session.user),
    ];
  }, [session]);

  const results = useMemo(() => {
    const needle = query.trim();
    if (!needle) {
      return allItems;
    }
    return allItems.filter((item) => t(item.labelKey).includes(needle));
  }, [allItems, query]);

  function goTo(item: NavigationItem) {
    setCommandPaletteOpen(false);
    setQuery('');
    navigate(item.path);
  }

  return (
    <Modal
      open={commandPaletteOpen}
      onClose={() => setCommandPaletteOpen(false)}
      titleKey="common.search.label"
      descriptionKey="common.search.commandHint"
      size="md"
    >
      <div className="command-palette">
        <label className="command-palette__input">
          <Search size={17} aria-hidden="true" />
          <span className="sr-only">{t('common.actions.search')}</span>
          <input
            type="search"
            value={query}
            autoFocus
            placeholder={t('common.search.placeholder')}
            onChange={(event) => setQuery(event.target.value)}
            onKeyDown={(event) => {
              if (event.key === 'Enter' && results[0]) {
                event.preventDefault();
                goTo(results[0]);
              }
            }}
          />
        </label>
        <div className="command-palette__results">
          {results.length === 0 ? (
            <EmptyState descriptionKey="common.search.noResults" />
          ) : (
            results.map((item) => {
              const Icon = item.icon;
              return (
                <button
                  key={`${item.scope}-${item.path}`}
                  type="button"
                  className="command-palette__result"
                  onClick={() => goTo(item)}
                >
                  <span className="command-palette__result-icon" aria-hidden="true">
                    <Icon size={17} />
                  </span>
                  <span className="command-palette__result-text">
                    <strong>{t(item.labelKey)}</strong>
                    <span>{t(item.groupKey)}</span>
                  </span>
                  <CornerDownLeft size={15} aria-hidden="true" />
                </button>
              );
            })
          )}
        </div>
      </div>
    </Modal>
  );
}
