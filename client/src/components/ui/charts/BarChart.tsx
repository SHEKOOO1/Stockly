import { formatNumber } from '@/lib/format';
import { t, type TranslationKey } from '@/i18n';
import type { ChartPoint } from '@/types/domain';

export interface BarChartProps {
  data: readonly ChartPoint[];
  labelKey?: TranslationKey;
  height?: number;
}

export function BarChart({
  data,
  labelKey = 'common.charts.activity',
  height = 180,
}: BarChartProps) {
  const width = 520;
  const paddingX = 18;
  const paddingY = 26;
  const chartBottom = height - paddingY;
  const max = Math.max(1, ...data.map((item) => Math.max(0, item.value)));

  if (data.length === 0) {
    return null;
  }

  const slot = (width - paddingX * 2) / data.length;
  const barWidth = Math.min(46, Math.max(10, slot * 0.55));

  return (
    <svg
      className="bar-chart"
      viewBox={`0 0 ${width} ${height}`}
      role="img"
      aria-label={t(labelKey)}
    >
      <title>{t(labelKey)}</title>
      <line
        x1={paddingX}
        y1={chartBottom}
        x2={width - paddingX}
        y2={chartBottom}
        className="bar-chart__axis"
      />
      {data.map((item, index) => {
        const barHeight = Math.max(3, (Math.max(0, item.value) / max) * (chartBottom - paddingY));
        const center = paddingX + slot * index + slot / 2;
        const top = chartBottom - barHeight;
        return (
          <g key={item.id} className={`bar-chart__group bar-chart__group--${item.tone}`}>
            <rect
              x={center - barWidth / 2}
              y={top}
              width={barWidth}
              height={barHeight}
              rx="4"
              className="bar-chart__bar"
            />
            <text
              x={center}
              y={Math.max(12, top - 6)}
              textAnchor="middle"
              style={{ direction: 'ltr', unicodeBidi: 'isolate' }}
            >
              {formatNumber(item.value, { maximumFractionDigits: 0 })}
            </text>
            <text x={center} y={height - 7} textAnchor="middle" className="bar-chart__label">
              {item.label}
            </text>
          </g>
        );
      })}
    </svg>
  );
}
