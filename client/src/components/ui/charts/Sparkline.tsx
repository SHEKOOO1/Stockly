import { useId } from 'react';
import { t, type TranslationKey } from '@/i18n';
import type { StatusTone } from '@/types/domain';

export interface SparklineProps {
  values: readonly number[];
  tone?: StatusTone;
  labelKey?: TranslationKey;
}

export function Sparkline({
  values,
  tone = 'info',
  labelKey = 'common.charts.trend',
}: SparklineProps) {
  const rawId = useId();
  const gradientId = `spark-${rawId.replace(/:/g, '')}`;
  const numbers = values.filter((value) => Number.isFinite(value));
  const width = 120;
  const height = 40;
  const padding = 3;

  if (numbers.length === 0) {
    return null;
  }

  const min = Math.min(...numbers);
  const max = Math.max(...numbers);
  const span = max - min || 1;
  const denominator = numbers.length === 1 ? 1 : numbers.length - 1;
  const points = numbers.map((value, index) => ({
    x: padding + (index / denominator) * (width - padding * 2),
    y: height - padding - ((value - min) / span) * (height - padding * 2),
  }));
  const line = points.map((point) => `${point.x.toFixed(1)},${point.y.toFixed(1)}`).join(' ');
  const area = `${line} ${width - padding},${height - padding} ${padding},${height - padding}`;
  const last = points[points.length - 1];

  return (
    <svg
      className={`sparkline sparkline--${tone}`}
      viewBox={`0 0 ${width} ${height}`}
      preserveAspectRatio="none"
      role="img"
      aria-label={t(labelKey)}
    >
      <title>{t(labelKey)}</title>
      <defs>
        <linearGradient id={gradientId} x1="0" y1="0" x2="0" y2="1">
          <stop offset="0%" stopColor="currentColor" stopOpacity="0.24" />
          <stop offset="100%" stopColor="currentColor" stopOpacity="0" />
        </linearGradient>
      </defs>
      <polygon points={area} fill={`url(#${gradientId})`} />
      <polyline points={line} fill="none" stroke="currentColor" strokeWidth="2" />
      <circle cx={last.x} cy={last.y} r="2.5" fill="currentColor" />
    </svg>
  );
}
