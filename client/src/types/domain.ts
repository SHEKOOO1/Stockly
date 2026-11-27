export type DataOrigin = 'demo';

export interface DemoRecord {
  readonly origin: DataOrigin;
}

export type AppContextScope = 'platform' | 'organization' | 'warehouse';

export type StatusTone = 'healthy' | 'warning' | 'critical' | 'neutral' | 'info';

export type EntityStatus = 'active' | 'inactive' | 'draft' | 'pending' | 'posted' | 'confirmed';

export interface Organization extends DemoRecord {
  id: string;
  name: string;
  code: string;
  planId: string;
  status: EntityStatus;
  warehouseCount: number;
  userCount: number;
  createdAt: string;
}

export interface Warehouse extends DemoRecord {
  id: string;
  organizationId: string;
  name: string;
  code: string;
  status: EntityStatus;
  productCount: number;
  stockValue: number;
  lastActivityAt: string;
}

export interface DemoUser extends DemoRecord {
  id: string;
  name: string;
  role: string;
  roleCode: string;
  email: string;
  organizationIds: string[];
  warehouseIds: string[];
  status: EntityStatus;
}

export interface DemoProfile extends DemoRecord {
  id: string;
  name: string;
  roleLabel: string;
  organizationName: string;
  warehouseName: string;
}

export interface Category extends DemoRecord {
  id: string;
  warehouseId: string;
  name: string;
  code: string;
  productCount: number;
  parentId: string | null;
  isActive: boolean;
  isDeleted: boolean;
  updatedAt: string;
}

export type UnitType = 'count' | 'weight' | 'volume' | 'packaging' | 'length';

export interface Unit extends DemoRecord {
  id: string;
  warehouseId: string;
  name: string;
  code: string;
  decimalPlaces: number;
  unitType: UnitType;
  isActive: boolean;
  isDeleted: boolean;
  updatedAt: string;
}

export interface Supplier extends DemoRecord {
  id: string;
  organizationId: string;
  name: string;
  code: string;
  contactName: string;
  phone: string;
  email: string;
  address: string;
  notes: string;
  isActive: boolean;
  isDeleted: boolean;
  updatedAt: string;
}

export type ProductStockStatus = 'healthy' | 'low' | 'out' | 'expiring';

export type BarcodeSymbology = 'EAN13' | 'EAN8' | 'UPC-A' | 'Code128' | 'QR' | 'DataMatrix';

export interface Product extends DemoRecord {
  id: string;
  warehouseId: string;
  sku: string;
  name: string;
  nameAr: string | null;
  description: string | null;
  categoryId: string | null;
  baseUnitId: string;
  purchaseUnitId: string | null;
  isBatchTracked: boolean;
  isExpiryTracked: boolean;
  shelfLifeDays: number | null;
  defaultReasonCodeId: string | null;
  isActive: boolean;
  isDeleted: boolean;
  updatedAt: string;
}

export interface ProductBarcode extends DemoRecord {
  id: string;
  productId: string;
  warehouseId: string | null;
  barcode: string;
  symbology: BarcodeSymbology;
  isPrimary: boolean;
}

export interface ProductWarehouseSetting extends DemoRecord {
  id: string;
  productId: string;
  warehouseId: string;
  minimumQuantity: number;
  maximumQuantity: number | null;
  reorderQuantity: number | null;
  defaultSupplierId: string | null;
  shelfLifeDaysOverride: number | null;
  isPreferred: boolean;
}

export interface StockBalance extends DemoRecord {
  id: string;
  productId: string;
  warehouseId: string;
  onHandQuantity: number;
  incomingQuantity: number;
  lastMovementAtUtc: string;
}

export interface Batch extends DemoRecord {
  id: string;
  productId: string;
  warehouseId: string;
  batchNumber: string;
  expiryDate: string | null;
  productionDate: string | null;
}

export type MovementType = 'STOCK_IN' | 'STOCK_OUT' | 'TRANSFER' | 'ADJUSTMENT' | 'COUNT';

export interface InventoryMovement extends DemoRecord {
  id: string;
  warehouseId: string;
  productId: string;
  type: MovementType;
  quantity: number;
  unitId: string;
  occurredAt: string;
  responsibleUserId: string;
  relatedTransaction: string;
  status: EntityStatus;
  notes: string;
}

export interface DemoAlert extends DemoRecord {
  id: string;
  warehouseId: string;
  productId: string;
  severity: StatusTone;
  title: string;
  message: string;
  detectedAt: string;
  status: EntityStatus;
}

export interface Activity extends DemoRecord {
  id: string;
  scope: AppContextScope;
  organizationId: string | null;
  warehouseId: string | null;
  action: string;
  actor: string;
  occurredAt: string;
  tone: StatusTone;
}

export interface Plan extends DemoRecord {
  id: string;
  name: string;
  code: string;
  price: number;
  currency: string;
  userLimit: number;
  warehouseLimit: number;
  status: EntityStatus;
}

export interface Role extends DemoRecord {
  id: string;
  organizationId: string;
  name: string;
  code: string;
  permissionCount: number;
  status: EntityStatus;
}

export interface StocktakingSession extends DemoRecord {
  id: string;
  warehouseId: string;
  code: string;
  status: EntityStatus;
  startedAt: string;
  expectedLines: number;
  countedLines: number;
  differenceLines: number;
}

export interface DemoNotification extends DemoRecord {
  id: string;
  title: string;
  body: string;
  createdAt: string;
  read: boolean;
  tone: StatusTone;
  audienceRoleCode?: string;
  lowStockKey?: string;
}

export interface ChartPoint {
  id: string;
  label: string;
  value: number;
  tone: StatusTone;
}
