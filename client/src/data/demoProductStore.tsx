import {
  createContext,
  useCallback,
  useContext,
  useMemo,
  useRef,
  useState,
  type ReactNode,
} from 'react';
import { demoDataOrigin, demoProductDataset } from '@/data/demoData';
import {
  getProductSummary as buildProductSummary,
  type ProductDataset,
  type ProductSummary,
} from '@/lib/product';
import type {
  BarcodeSymbology,
  Batch,
  Product,
  ProductBarcode,
  ProductWarehouseSetting,
  StockBalance,
} from '@/types/domain';

export interface ProductFormValues {
  name: string;
  nameAr: string;
  sku: string;
  description: string;
  categoryId: string;
  baseUnitId: string;
  purchaseUnitId: string;
  isBatchTracked: boolean;
  isExpiryTracked: boolean;
  shelfLifeDays: number | null;
  defaultReasonCodeId: string;
  isActive: boolean;
  minimumQuantity: number;
  maximumQuantity: number | null;
  reorderQuantity: number | null;
  defaultSupplierId: string;
  barcode: string;
  barcodeSymbology: BarcodeSymbology;
}

interface DemoProductState {
  products: Product[];
  barcodes: ProductBarcode[];
  settings: ProductWarehouseSetting[];
  balances: StockBalance[];
  batches: Batch[];
}

export interface DemoProductStoreValue {
  products: readonly Product[];
  barcodes: readonly ProductBarcode[];
  settings: readonly ProductWarehouseSetting[];
  balances: readonly StockBalance[];
  batches: readonly Batch[];
  productSummaries: readonly ProductSummary[];
  addProduct: (warehouseId: string, values: ProductFormValues) => void;
  updateProduct: (productId: string, values: ProductFormValues) => void;
  archiveProduct: (productId: string) => void;
  restoreProduct: (productId: string) => void;
  getProductSummary: (product: Product, now?: Date) => ProductSummary;
}

const DemoProductStoreContext = createContext<DemoProductStoreValue | null>(null);

function emptyToNull(value: string): string | null {
  const trimmed = value.trim();
  return trimmed === '' ? null : trimmed;
}

function optionalNumber(value: number | null): number | null {
  return value === null || Number.isNaN(value) ? null : value;
}

export function DemoProductStoreProvider({ children }: { children: ReactNode }) {
  const [state, setState] = useState<DemoProductState>(() => ({
    products: [...demoProductDataset.products],
    barcodes: [...demoProductDataset.barcodes],
    settings: [...demoProductDataset.settings],
    balances: [...demoProductDataset.balances],
    batches: [...demoProductDataset.batches],
  }));
  const counter = useRef(0);

  const makeId = useCallback((prefix: string) => {
    counter.current += 1;
    return `${prefix}-local-${counter.current}`;
  }, []);

  const dataset = useMemo<ProductDataset>(
    () => ({
      products: state.products,
      barcodes: state.barcodes,
      settings: state.settings,
      balances: state.balances,
      batches: state.batches,
    }),
    [state],
  );

  const productSummaries = useMemo(
    () => dataset.products.map((product) => buildProductSummary(dataset, product)),
    [dataset],
  );

  const addProduct = useCallback(
    (warehouseId: string, values: ProductFormValues) => {
      const productId = makeId('prod');
      const product: Product = {
        origin: demoDataOrigin,
        id: productId,
        warehouseId,
        sku: values.sku.trim(),
        name: values.name.trim(),
        nameAr: emptyToNull(values.nameAr),
        description: emptyToNull(values.description),
        categoryId: emptyToNull(values.categoryId),
        baseUnitId: values.baseUnitId,
        purchaseUnitId: emptyToNull(values.purchaseUnitId),
        isBatchTracked: values.isBatchTracked,
        isExpiryTracked: values.isExpiryTracked,
        shelfLifeDays: values.isExpiryTracked ? values.shelfLifeDays : null,
        defaultReasonCodeId: emptyToNull(values.defaultReasonCodeId),
        isActive: values.isActive,
        isDeleted: false,
        updatedAt: new Date().toISOString(),
      };
      const barcodeId = makeId('bar');
      const settingId = makeId('set');
      const balanceId = makeId('bal');
      const nextBarcode = values.barcode.trim()
        ? {
            origin: demoDataOrigin,
            id: barcodeId,
            productId,
            warehouseId,
            barcode: values.barcode.trim(),
            symbology: values.barcodeSymbology,
            isPrimary: true,
          }
        : null;

      setState((current) => ({
        products: [...current.products, product],
        barcodes: nextBarcode ? [...current.barcodes, nextBarcode] : current.barcodes,
        settings: [
          ...current.settings,
          {
            origin: demoDataOrigin,
            id: settingId,
            productId,
            warehouseId,
            minimumQuantity: values.minimumQuantity,
            maximumQuantity: optionalNumber(values.maximumQuantity),
            reorderQuantity: optionalNumber(values.reorderQuantity),
            defaultSupplierId: emptyToNull(values.defaultSupplierId),
            shelfLifeDaysOverride: null,
            isPreferred: true,
          },
        ],
        balances: [
          ...current.balances,
          {
            origin: demoDataOrigin,
            id: balanceId,
            productId,
            warehouseId,
            onHandQuantity: 0,
            incomingQuantity: 0,
            lastMovementAtUtc: new Date().toISOString(),
          },
        ],
        batches: current.batches,
      }));
    },
    [makeId],
  );

  const updateProduct = useCallback(
    (productId: string, values: ProductFormValues) => {
      const barcodeId = makeId('bar');
      const settingId = makeId('set');
      const balanceId = makeId('bal');

      setState((current) => {
        const existing = current.products.find((product) => product.id === productId);
        if (!existing) {
          return current;
        }
        const updated: Product = {
          ...existing,
          sku: values.sku.trim(),
          name: values.name.trim(),
          nameAr: emptyToNull(values.nameAr),
          description: emptyToNull(values.description),
          categoryId: emptyToNull(values.categoryId),
          baseUnitId: values.baseUnitId,
          purchaseUnitId: emptyToNull(values.purchaseUnitId),
          isBatchTracked: values.isBatchTracked,
          isExpiryTracked: values.isExpiryTracked,
          shelfLifeDays: values.isExpiryTracked ? values.shelfLifeDays : null,
          defaultReasonCodeId: emptyToNull(values.defaultReasonCodeId),
          isActive: existing.isDeleted ? false : values.isActive,
          isDeleted: existing.isDeleted,
          updatedAt: new Date().toISOString(),
        };
        const barcodes = current.barcodes.filter(
          (barcode) => !(barcode.productId === productId && barcode.isPrimary),
        );
        if (values.barcode.trim()) {
          barcodes.push({
            origin: demoDataOrigin,
            id: barcodeId,
            productId,
            warehouseId: existing.warehouseId,
            barcode: values.barcode.trim(),
            symbology: values.barcodeSymbology,
            isPrimary: true,
          });
        }
        const settings = current.settings.filter(
          (setting) =>
            !(setting.productId === productId && setting.warehouseId === existing.warehouseId),
        );
        settings.push({
          origin: demoDataOrigin,
          id: settingId,
          productId,
          warehouseId: existing.warehouseId,
          minimumQuantity: values.minimumQuantity,
          maximumQuantity: optionalNumber(values.maximumQuantity),
          reorderQuantity: optionalNumber(values.reorderQuantity),
          defaultSupplierId: emptyToNull(values.defaultSupplierId),
          shelfLifeDaysOverride: null,
          isPreferred: true,
        });
        const balances = [...current.balances];
        if (!balances.some((balance) => balance.productId === productId)) {
          balances.push({
            origin: demoDataOrigin,
            id: balanceId,
            productId,
            warehouseId: existing.warehouseId,
            onHandQuantity: 0,
            incomingQuantity: 0,
            lastMovementAtUtc: new Date().toISOString(),
          });
        }

        return {
          products: current.products.map((product) =>
            product.id === productId ? updated : product,
          ),
          barcodes,
          settings,
          balances,
          batches: current.batches,
        };
      });
    },
    [makeId],
  );

  const archiveProduct = useCallback((productId: string) => {
    setState((current) => ({
      ...current,
      products: current.products.map((product) =>
        product.id === productId
          ? { ...product, isActive: false, isDeleted: true, updatedAt: new Date().toISOString() }
          : product,
      ),
    }));
  }, []);

  const restoreProduct = useCallback((productId: string) => {
    setState((current) => ({
      ...current,
      products: current.products.map((product) =>
        product.id === productId
          ? { ...product, isActive: true, isDeleted: false, updatedAt: new Date().toISOString() }
          : product,
      ),
    }));
  }, []);

  const getProductSummary = useCallback(
    (product: Product, now?: Date): ProductSummary => buildProductSummary(dataset, product, now),
    [dataset],
  );

  const value = useMemo<DemoProductStoreValue>(
    () => ({
      products: state.products,
      barcodes: state.barcodes,
      settings: state.settings,
      balances: state.balances,
      batches: state.batches,
      productSummaries,
      addProduct,
      updateProduct,
      archiveProduct,
      restoreProduct,
      getProductSummary,
    }),
    [
      state,
      productSummaries,
      addProduct,
      updateProduct,
      archiveProduct,
      restoreProduct,
      getProductSummary,
    ],
  );

  return (
    <DemoProductStoreContext.Provider value={value}>{children}</DemoProductStoreContext.Provider>
  );
}

export function useDemoProductStore(): DemoProductStoreValue {
  const value = useContext(DemoProductStoreContext);
  if (!value) {
    throw new Error('useDemoProductStore must be used within DemoProductStoreProvider');
  }
  return value;
}
