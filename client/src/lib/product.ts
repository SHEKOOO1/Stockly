import type {
  Batch,
  Product,
  ProductBarcode,
  ProductStockStatus,
  ProductWarehouseSetting,
  StockBalance,
} from '@/types/domain';

export interface ProductDataset {
  readonly products: readonly Product[];
  readonly barcodes: readonly ProductBarcode[];
  readonly settings: readonly ProductWarehouseSetting[];
  readonly balances: readonly StockBalance[];
  readonly batches: readonly Batch[];
}

export const expiringWindowDays = 30;

const dayInMilliseconds = 86_400_000;

function daysUntil(value: string, now: Date): number {
  return (new Date(value).getTime() - now.getTime()) / dayInMilliseconds;
}

export function getPrimaryBarcode(
  dataset: ProductDataset,
  product: Product,
): ProductBarcode | undefined {
  return dataset.barcodes.find(
    (barcode) =>
      barcode.productId === product.id &&
      barcode.isPrimary &&
      (barcode.warehouseId === null || barcode.warehouseId === product.warehouseId),
  );
}

export function getWarehouseSetting(
  dataset: ProductDataset,
  product: Product,
): ProductWarehouseSetting | undefined {
  return dataset.settings.find(
    (setting) => setting.productId === product.id && setting.warehouseId === product.warehouseId,
  );
}

export function getStockBalance(
  dataset: ProductDataset,
  product: Product,
): StockBalance | undefined {
  return dataset.balances.find(
    (balance) => balance.productId === product.id && balance.warehouseId === product.warehouseId,
  );
}

export function getProductBatches(dataset: ProductDataset, product: Product): readonly Batch[] {
  return dataset.batches
    .filter((batch) => batch.productId === product.id && batch.warehouseId === product.warehouseId)
    .sort((left, right) => {
      const leftDate = left.expiryDate ?? '9999-12-31';
      const rightDate = right.expiryDate ?? '9999-12-31';
      return leftDate.localeCompare(rightDate);
    });
}

export function getNearestExpiringBatch(
  dataset: ProductDataset,
  product: Product,
  now: Date = new Date(),
): Batch | undefined {
  if (!product.isExpiryTracked) {
    return undefined;
  }
  return getProductBatches(dataset, product).find(
    (batch) => batch.expiryDate !== null && daysUntil(batch.expiryDate, now) <= expiringWindowDays,
  );
}

export function deriveProductStockStatus(
  dataset: ProductDataset,
  product: Product,
  now: Date = new Date(),
): ProductStockStatus {
  const balance = getStockBalance(dataset, product);
  const setting = getWarehouseSetting(dataset, product);
  const onHandQuantity = balance?.onHandQuantity ?? 0;
  const minimumQuantity = setting?.minimumQuantity ?? 0;

  if (onHandQuantity <= 0) {
    return 'out';
  }
  if (onHandQuantity < minimumQuantity) {
    return 'low';
  }
  if (getNearestExpiringBatch(dataset, product, now)) {
    return 'expiring';
  }
  return 'healthy';
}

export interface ProductSummary {
  readonly product: Product;
  readonly barcode: ProductBarcode | undefined;
  readonly setting: ProductWarehouseSetting | undefined;
  readonly balance: StockBalance | undefined;
  readonly batches: readonly Batch[];
  readonly nearestExpiry: Batch | undefined;
  readonly stockStatus: ProductStockStatus;
  readonly onHandQuantity: number;
  readonly incomingQuantity: number;
  readonly minimumQuantity: number;
  readonly maximumQuantity: number | null;
  readonly reorderQuantity: number | null;
  readonly shelfLifeDays: number | null;
}

export function getProductSummary(
  dataset: ProductDataset,
  product: Product,
  now: Date = new Date(),
): ProductSummary {
  const barcode = getPrimaryBarcode(dataset, product);
  const setting = getWarehouseSetting(dataset, product);
  const balance = getStockBalance(dataset, product);
  const batches = getProductBatches(dataset, product);

  return {
    product,
    barcode,
    setting,
    balance,
    batches,
    nearestExpiry: getNearestExpiringBatch(dataset, product, now),
    stockStatus: deriveProductStockStatus(dataset, product, now),
    onHandQuantity: balance?.onHandQuantity ?? 0,
    incomingQuantity: balance?.incomingQuantity ?? 0,
    minimumQuantity: setting?.minimumQuantity ?? 0,
    maximumQuantity: setting?.maximumQuantity ?? null,
    reorderQuantity: setting?.reorderQuantity ?? null,
    shelfLifeDays: setting?.shelfLifeDaysOverride ?? product.shelfLifeDays,
  };
}
