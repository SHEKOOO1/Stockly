import { Info } from 'lucide-react';
import { t, type TranslationKey } from '@/i18n';

export interface DemoNoticeProps {
  messageKey?: TranslationKey;
  compact?: boolean;
  className?: string;
}

export function DemoNotice({
  messageKey = 'common.demo.notice',
  compact = false,
  className = '',
}: DemoNoticeProps) {
  return (
    <aside className={`demo-notice ${compact ? 'demo-notice--compact' : ''} ${className}`.trim()}>
      <span className="demo-notice__icon" aria-hidden="true">
        <Info size={16} />
      </span>
      <div className="demo-notice__body">
        <strong className="demo-notice__title">{t('common.demo.title')}</strong>
        <p className="demo-notice__text">{t(messageKey)}</p>
      </div>
    </aside>
  );
}
