import { useId, useState, type KeyboardEvent, type ReactNode } from 'react';
import { t, type TranslationKey } from '@/i18n';

export interface TabItem {
  id: string;
  labelKey: TranslationKey;
  content: ReactNode;
  disabled?: boolean;
}

export interface TabsProps {
  items: readonly TabItem[];
  value?: string;
  defaultValue?: string;
  onChange?: (id: string) => void;
  ariaLabelKey?: TranslationKey;
}

export function Tabs({
  items,
  value,
  defaultValue,
  onChange,
  ariaLabelKey = 'common.labels.details',
}: TabsProps) {
  const baseId = useId();
  const [internalValue, setInternalValue] = useState(defaultValue ?? items[0]?.id ?? '');
  const currentValue = value ?? internalValue;
  const activeItem = items.find((item) => item.id === currentValue) ?? items[0];

  function select(id: string) {
    setInternalValue(id);
    onChange?.(id);
  }

  function handleKeyDown(event: KeyboardEvent<HTMLDivElement>) {
    const enabled = items.filter((item) => !item.disabled);
    if (enabled.length === 0 || (event.key !== 'ArrowRight' && event.key !== 'ArrowLeft')) {
      return;
    }
    event.preventDefault();
    const index = enabled.findIndex((item) => item.id === currentValue);
    const step = event.key === 'ArrowRight' ? 1 : -1;
    const next = enabled[(index + step + enabled.length) % enabled.length];
    if (next) {
      select(next.id);
    }
  }

  return (
    <div className="tabs">
      <div
        className="tabs__list"
        role="tablist"
        aria-label={t(ariaLabelKey)}
        onKeyDown={handleKeyDown}
      >
        {items.map((item) => (
          <button
            key={item.id}
            type="button"
            role="tab"
            id={`${baseId}-tab-${item.id}`}
            className={`tabs__tab ${item.id === currentValue ? 'is-active' : ''}`.trim()}
            aria-selected={item.id === currentValue}
            aria-controls={`${baseId}-panel-${item.id}`}
            disabled={item.disabled}
            onClick={() => select(item.id)}
          >
            {t(item.labelKey)}
          </button>
        ))}
      </div>
      {activeItem ? (
        <div
          className="tabs__panel"
          role="tabpanel"
          id={`${baseId}-panel-${activeItem.id}`}
          aria-labelledby={`${baseId}-tab-${activeItem.id}`}
        >
          {activeItem.content}
        </div>
      ) : null}
    </div>
  );
}
