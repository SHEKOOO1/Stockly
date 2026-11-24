import type { TranslationKey } from '@/i18n';
import type { ProductStockStatus, StatusTone } from '@/types/domain';

export const stockStatusLabelKeys: Record<ProductStockStatus, TranslationKey> = {
  healthy: 'common.status.healthy',
  low: 'common.status.low',
  out: 'common.status.out',
  expiring: 'common.status.expiring',
};

export const stockStatusTones: Record<ProductStockStatus, StatusTone> = {
  healthy: 'healthy',
  low: 'warning',
  out: 'critical',
  expiring: 'warning',
};
