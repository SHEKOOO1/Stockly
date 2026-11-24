import { useCallback, useEffect, useState } from 'react';
import { t } from '@/i18n';

export type DemoLoaderStatus = 'loading' | 'ready' | 'error';

export interface DemoLoaderOptions {
  delayMs?: number;
  fail?: boolean;
}

export interface DemoLoaderResult<T> {
  data: readonly T[];
  status: DemoLoaderStatus;
  error: string | null;
  reload: () => void;
}

export function useDemoLoader<T>(
  source: readonly T[],
  options: DemoLoaderOptions = {},
): DemoLoaderResult<T> {
  const { delayMs = 220, fail = false } = options;
  const [data, setData] = useState<readonly T[]>([]);
  const [status, setStatus] = useState<DemoLoaderStatus>('loading');
  const [error, setError] = useState<string | null>(null);
  const [attempt, setAttempt] = useState(0);

  const reload = useCallback(() => {
    setAttempt((current) => current + 1);
  }, []);

  useEffect(() => {
    setStatus('loading');
    setError(null);
    setData([]);

    const timer = setTimeout(() => {
      if (fail) {
        setError(t('common.states.errorDescription'));
        setStatus('error');
        return;
      }
      setData(source);
      setStatus('ready');
    }, delayMs);

    return () => clearTimeout(timer);
  }, [source, delayMs, fail, attempt]);

  return { data, status, error, reload };
}
