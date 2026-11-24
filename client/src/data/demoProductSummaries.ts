import { demoProductDataset } from '@/data/demoData';
import { getProductSummary, type ProductSummary } from '@/lib/product';

export const demoProductSummaries: readonly ProductSummary[] = demoProductDataset.products.map(
  (product) => getProductSummary(demoProductDataset, product),
);
