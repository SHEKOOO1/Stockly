import { locale } from '@/i18n';

export type NumericValue = number | string;

function toDate(value: string | number | Date): Date | null {
  const date = value instanceof Date ? value : new Date(value);
  return Number.isNaN(date.getTime()) ? null : date;
}

function toNumber(value: NumericValue): number | null {
  const numeric = typeof value === 'number' ? value : Number(value);
  return Number.isFinite(numeric) ? numeric : null;
}

export function formatNumber(value: NumericValue, options?: Intl.NumberFormatOptions): string {
  const numeric = toNumber(value);
  if (numeric === null) {
    return '—';
  }
  return new Intl.NumberFormat(locale, options).format(numeric);
}

export function formatCurrency(
  value: NumericValue,
  currency = 'SAR',
  options?: Intl.NumberFormatOptions,
): string {
  const numeric = toNumber(value);
  if (numeric === null) {
    return '—';
  }
  return new Intl.NumberFormat(locale, {
    style: 'currency',
    currency,
    maximumFractionDigits: 2,
    ...options,
  }).format(numeric);
}

export function formatDate(
  value: string | number | Date,
  options?: Intl.DateTimeFormatOptions,
): string {
  const date = toDate(value);
  if (!date) {
    return '—';
  }
  return new Intl.DateTimeFormat(locale, {
    day: '2-digit',
    month: 'short',
    year: 'numeric',
    timeZone: 'UTC',
    ...options,
  }).format(date);
}

export function formatDateTime(
  value: string | number | Date,
  options?: Intl.DateTimeFormatOptions,
): string {
  const date = toDate(value);
  if (!date) {
    return '—';
  }
  return new Intl.DateTimeFormat(locale, {
    day: '2-digit',
    month: 'short',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
    hour12: false,
    timeZone: 'UTC',
    ...options,
  }).format(date);
}
