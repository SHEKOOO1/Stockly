import {
  createContext,
  useCallback,
  useContext,
  useMemo,
  useRef,
  useState,
  type ReactNode,
} from 'react';
import { demoDataOrigin, demoSuppliers } from '@/data/demoData';
import { useDemoProductStore } from '@/data/demoProductStore';
import {
  getSupplierProductCount,
  hasSupplierValidationErrors,
  isSupplierSelectable,
  normalizeSupplierCode,
  validateSupplier,
  type SupplierSummary,
  type SupplierValidationErrors,
} from '@/lib/supplier';
import type { Supplier } from '@/types/domain';

export interface SupplierFormValues {
  name: string;
  code: string;
  contactName: string;
  phone: string;
  email: string;
  address: string;
  notes: string;
}

export interface DemoSupplierStoreValue {
  suppliers: readonly Supplier[];
  supplierSummaries: readonly SupplierSummary[];
  addSupplier: (
    organizationId: string,
    values: SupplierFormValues,
  ) => SupplierValidationErrors | null;
  updateSupplier: (
    supplierId: string,
    values: SupplierFormValues,
  ) => SupplierValidationErrors | null;
  archiveSupplier: (supplierId: string) => void;
  restoreSupplier: (supplierId: string) => void;
  getSupplierById: (supplierId: string | null | undefined) => Supplier | undefined;
  getSupplierSummaries: (organizationId: string) => readonly SupplierSummary[];
  getSelectableSuppliers: (
    organizationId: string,
    currentSupplierIds?: readonly (string | null | undefined)[],
  ) => readonly Supplier[];
  getUsageCount: (supplierId: string) => number;
}

const DemoSupplierStoreContext = createContext<DemoSupplierStoreValue | null>(null);

function createSupplier(
  id: string,
  organizationId: string,
  values: SupplierFormValues,
  now: string,
): Supplier {
  return {
    origin: demoDataOrigin,
    id,
    organizationId,
    name: values.name.trim(),
    code: normalizeSupplierCode(values.code),
    contactName: values.contactName.trim(),
    phone: values.phone.trim(),
    email: values.email.trim(),
    address: values.address.trim(),
    notes: values.notes.trim(),
    isActive: true,
    isDeleted: false,
    updatedAt: now,
  };
}

export function DemoSupplierStoreProvider({ children }: { children: ReactNode }) {
  const { products, settings } = useDemoProductStore();
  const [suppliers, setSuppliers] = useState<readonly Supplier[]>(() => demoSuppliers);
  const counter = useRef(0);

  const makeId = useCallback((prefix: string) => {
    counter.current += 1;
    return `${prefix}-local-${counter.current}`;
  }, []);

  const supplierSummaries = useMemo<SupplierSummary[]>(
    () =>
      suppliers.map((supplier) => ({
        supplier,
        usageCount: getSupplierProductCount(products, settings, supplier.id),
      })),
    [suppliers, products, settings],
  );

  const getSupplierById = useCallback(
    (supplierId: string | null | undefined) =>
      supplierId ? suppliers.find((supplier) => supplier.id === supplierId) : undefined,
    [suppliers],
  );

  const getSupplierSummaries = useCallback(
    (organizationId: string) =>
      supplierSummaries.filter((row) => row.supplier.organizationId === organizationId),
    [supplierSummaries],
  );

  const getUsageCount = useCallback(
    (supplierId: string) => getSupplierProductCount(products, settings, supplierId),
    [products, settings],
  );

  const getSelectableSuppliers = useCallback(
    (organizationId: string, currentSupplierIds: readonly (string | null | undefined)[] = []) => {
      const retained = new Set(currentSupplierIds.filter((id): id is string => Boolean(id)));
      return suppliers.filter(
        (supplier) =>
          supplier.organizationId === organizationId &&
          (isSupplierSelectable(supplier) || retained.has(supplier.id)),
      );
    },
    [suppliers],
  );

  const addSupplier = useCallback(
    (organizationId: string, values: SupplierFormValues): SupplierValidationErrors | null => {
      const errors = validateSupplier(values, suppliers, organizationId);
      if (hasSupplierValidationErrors(errors)) {
        return errors;
      }
      setSuppliers((current) => [
        ...current,
        createSupplier(makeId('sup'), organizationId, values, new Date().toISOString()),
      ]);
      return null;
    },
    [suppliers, makeId],
  );

  const updateSupplier = useCallback(
    (supplierId: string, values: SupplierFormValues): SupplierValidationErrors | null => {
      const existing = suppliers.find((supplier) => supplier.id === supplierId);
      if (!existing) {
        return { name: 'required' };
      }
      const errors = validateSupplier(
        { ...values, editingId: supplierId },
        suppliers,
        existing.organizationId,
      );
      if (hasSupplierValidationErrors(errors)) {
        return errors;
      }
      setSuppliers((current) =>
        current.map((supplier) =>
          supplier.id === supplierId
            ? {
                ...supplier,
                name: values.name.trim(),
                code: normalizeSupplierCode(values.code),
                contactName: values.contactName.trim(),
                phone: values.phone.trim(),
                email: values.email.trim(),
                address: values.address.trim(),
                notes: values.notes.trim(),
                updatedAt: new Date().toISOString(),
              }
            : supplier,
        ),
      );
      return null;
    },
    [suppliers],
  );

  const archiveSupplier = useCallback((supplierId: string) => {
    setSuppliers((current) =>
      current.map((supplier) =>
        supplier.id === supplierId
          ? {
              ...supplier,
              isActive: false,
              isDeleted: true,
              updatedAt: new Date().toISOString(),
            }
          : supplier,
      ),
    );
  }, []);

  const restoreSupplier = useCallback((supplierId: string) => {
    setSuppliers((current) =>
      current.map((supplier) =>
        supplier.id === supplierId
          ? {
              ...supplier,
              isActive: true,
              isDeleted: false,
              updatedAt: new Date().toISOString(),
            }
          : supplier,
      ),
    );
  }, []);

  const value = useMemo<DemoSupplierStoreValue>(
    () => ({
      suppliers,
      supplierSummaries,
      addSupplier,
      updateSupplier,
      archiveSupplier,
      restoreSupplier,
      getSupplierById,
      getSupplierSummaries,
      getSelectableSuppliers,
      getUsageCount,
    }),
    [
      suppliers,
      supplierSummaries,
      addSupplier,
      updateSupplier,
      archiveSupplier,
      restoreSupplier,
      getSupplierById,
      getSupplierSummaries,
      getSelectableSuppliers,
      getUsageCount,
    ],
  );

  return (
    <DemoSupplierStoreContext.Provider value={value}>{children}</DemoSupplierStoreContext.Provider>
  );
}

export function useDemoSupplierStore(): DemoSupplierStoreValue {
  const value = useContext(DemoSupplierStoreContext);
  if (!value) {
    throw new Error('useDemoSupplierStore must be used within DemoSupplierStoreProvider');
  }
  return value;
}
