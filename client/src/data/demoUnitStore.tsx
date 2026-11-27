import {
  createContext,
  useCallback,
  useContext,
  useMemo,
  useRef,
  useState,
  type ReactNode,
} from 'react';
import { demoDataOrigin, demoUnits } from '@/data/demoData';
import { useDemoProductStore } from '@/data/demoProductStore';
import {
  canArchiveUnit,
  getUnitUsageCount,
  hasUnitValidationErrors,
  isUnitSelectable,
  normalizeUnitCode,
  validateUnit,
  type UnitSummary,
  type UnitValidationErrors,
} from '@/lib/unit';
import type { Unit, UnitType } from '@/types/domain';

export interface UnitFormValues {
  name: string;
  code: string;
  decimalPlaces: number;
  unitType: UnitType;
}

export interface DemoUnitStoreValue {
  units: readonly Unit[];
  unitSummaries: readonly UnitSummary[];
  addUnit: (warehouseId: string, values: UnitFormValues) => UnitValidationErrors | null;
  updateUnit: (unitId: string, values: UnitFormValues) => UnitValidationErrors | null;
  archiveUnit: (unitId: string) => boolean;
  restoreUnit: (unitId: string) => void;
  getUnitById: (unitId: string | null | undefined) => Unit | undefined;
  getUnitSummaries: (warehouseId: string) => readonly UnitSummary[];
  getSelectableUnits: (currentUnitIds?: readonly (string | null | undefined)[]) => readonly Unit[];
  getUsageCount: (unitId: string) => number;
}

const DemoUnitStoreContext = createContext<DemoUnitStoreValue | null>(null);

function createUnit(id: string, warehouseId: string, values: UnitFormValues, now: string): Unit {
  return {
    origin: demoDataOrigin,
    id,
    warehouseId,
    name: values.name.trim(),
    code: normalizeUnitCode(values.code),
    decimalPlaces: values.decimalPlaces,
    unitType: values.unitType,
    isActive: true,
    isDeleted: false,
    updatedAt: now,
  };
}

export function DemoUnitStoreProvider({ children }: { children: ReactNode }) {
  const { products } = useDemoProductStore();
  const [units, setUnits] = useState<readonly Unit[]>(() => demoUnits);
  const counter = useRef(0);

  const makeId = useCallback((prefix: string) => {
    counter.current += 1;
    return `${prefix}-local-${counter.current}`;
  }, []);

  const unitSummaries = useMemo<UnitSummary[]>(
    () =>
      units.map((unit) => ({
        unit,
        usageCount: getUnitUsageCount(products, unit.id),
      })),
    [units, products],
  );

  const getUnitById = useCallback(
    (unitId: string | null | undefined) =>
      unitId ? units.find((unit) => unit.id === unitId) : undefined,
    [units],
  );

  const getUnitSummaries = useCallback(
    (warehouseId: string) => unitSummaries.filter((row) => row.unit.warehouseId === warehouseId),
    [unitSummaries],
  );

  const getUsageCount = useCallback(
    (unitId: string) => getUnitUsageCount(products, unitId),
    [products],
  );

  const getSelectableUnits = useCallback(
    (currentUnitIds: readonly (string | null | undefined)[] = []) => {
      const retained = new Set(currentUnitIds.filter((id): id is string => Boolean(id)));
      return units.filter((unit) => isUnitSelectable(unit) || retained.has(unit.id));
    },
    [units],
  );

  const addUnit = useCallback(
    (warehouseId: string, values: UnitFormValues): UnitValidationErrors | null => {
      const errors = validateUnit(values, units);
      if (hasUnitValidationErrors(errors)) {
        return errors;
      }
      setUnits((current) => [
        ...current,
        createUnit(makeId('unit'), warehouseId, values, new Date().toISOString()),
      ]);
      return null;
    },
    [units, makeId],
  );

  const updateUnit = useCallback(
    (unitId: string, values: UnitFormValues): UnitValidationErrors | null => {
      const existing = units.find((unit) => unit.id === unitId);
      if (!existing) {
        return { name: 'required' };
      }
      const errors = validateUnit({ ...values, editingId: unitId }, units);
      if (hasUnitValidationErrors(errors)) {
        return errors;
      }
      setUnits((current) =>
        current.map((unit) =>
          unit.id === unitId
            ? {
                ...unit,
                name: values.name.trim(),
                code: normalizeUnitCode(values.code),
                decimalPlaces: values.decimalPlaces,
                unitType: values.unitType,
                updatedAt: new Date().toISOString(),
              }
            : unit,
        ),
      );
      return null;
    },
    [units],
  );

  const archiveUnit = useCallback(
    (unitId: string) => {
      if (!canArchiveUnit(products, unitId)) {
        return false;
      }
      setUnits((current) =>
        current.map((unit) =>
          unit.id === unitId
            ? {
                ...unit,
                isActive: false,
                isDeleted: true,
                updatedAt: new Date().toISOString(),
              }
            : unit,
        ),
      );
      return true;
    },
    [products],
  );

  const restoreUnit = useCallback((unitId: string) => {
    setUnits((current) =>
      current.map((unit) =>
        unit.id === unitId
          ? {
              ...unit,
              isActive: true,
              isDeleted: false,
              updatedAt: new Date().toISOString(),
            }
          : unit,
      ),
    );
  }, []);

  const value = useMemo<DemoUnitStoreValue>(
    () => ({
      units,
      unitSummaries,
      addUnit,
      updateUnit,
      archiveUnit,
      restoreUnit,
      getUnitById,
      getUnitSummaries,
      getSelectableUnits,
      getUsageCount,
    }),
    [
      units,
      unitSummaries,
      addUnit,
      updateUnit,
      archiveUnit,
      restoreUnit,
      getUnitById,
      getUnitSummaries,
      getSelectableUnits,
      getUsageCount,
    ],
  );

  return <DemoUnitStoreContext.Provider value={value}>{children}</DemoUnitStoreContext.Provider>;
}

export function useDemoUnitStore(): DemoUnitStoreValue {
  const value = useContext(DemoUnitStoreContext);
  if (!value) {
    throw new Error('useDemoUnitStore must be used within DemoUnitStoreProvider');
  }
  return value;
}
