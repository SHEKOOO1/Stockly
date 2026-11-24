import { ArrowLeft, Warehouse } from 'lucide-react';
import { useState } from 'react';
import { Link } from 'react-router-dom';
import {
  ChartCard,
  DemoNotice,
  ErrorState,
  FormField,
  LoadingState,
  PageHeader,
  StatCard,
  StatusBadge,
} from '@/components/ui';
import { useApp } from '@/context/AppContext';
import { t, type TranslationKey } from '@/i18n';
import { formatCurrency, formatNumber } from '@/lib/format';
import { useDemoLoader } from '@/lib/useDemoLoader';
import type { EntityStatus, StatusTone } from '@/types/domain';

const formGridStyle = {
  display: 'grid',
  gridTemplateColumns: 'repeat(auto-fit, minmax(240px, 1fr))',
  gap: '16px',
} as const;

const summaryStyle = {
  display: 'grid',
  gap: '8px',
} as const;

const statStyle = {
  marginBlockEnd: '20px',
} as const;

const statusOptions: readonly EntityStatus[] = ['active', 'inactive'];

const statusLabels: Record<EntityStatus, TranslationKey> = {
  active: 'common.status.active',
  inactive: 'common.status.inactive',
  draft: 'common.status.draft',
  pending: 'common.status.pending',
  posted: 'common.status.posted',
  confirmed: 'common.status.confirmed',
};

const statusTones: Record<EntityStatus, StatusTone> = {
  active: 'healthy',
  inactive: 'neutral',
  draft: 'neutral',
  pending: 'warning',
  posted: 'info',
  confirmed: 'healthy',
};

interface WarehouseForm {
  name: string;
  code: string;
  status: EntityStatus;
  productCount: string;
  stockValue: string;
}

interface FormErrors {
  name: boolean;
  code: boolean;
  productCount: boolean;
  stockValue: boolean;
}

const initialForm: WarehouseForm = {
  name: '',
  code: '',
  status: 'active',
  productCount: '0',
  stockValue: '0',
};

const emptyErrors: FormErrors = {
  name: false,
  code: false,
  productCount: false,
  stockValue: false,
};

export function CreateWarehousePage() {
  const { availableWarehouses, showToast } = useApp();
  const loader = useDemoLoader(availableWarehouses, { delayMs: 180 });
  const [form, setForm] = useState<WarehouseForm>(initialForm);
  const [errors, setErrors] = useState<FormErrors>(emptyErrors);
  const [preview, setPreview] = useState<WarehouseForm | null>(null);

  function submitPreview() {
    const productCount = Number(form.productCount);
    const stockValue = Number(form.stockValue);
    const nextErrors: FormErrors = {
      name: form.name.trim().length < 3,
      code: !/^[A-Za-z0-9]{3,12}$/.test(form.code.trim()),
      productCount: !Number.isInteger(productCount) || productCount < 0,
      stockValue: !Number.isFinite(stockValue) || stockValue < 0,
    };
    setErrors(nextErrors);
    if (Object.values(nextErrors).some(Boolean)) {
      return;
    }
    setPreview({
      ...form,
      name: form.name.trim(),
      code: form.code.trim().toUpperCase(),
    });
    showToast('organization.warehousesNew.toastPreview', 'info');
  }

  function resetForm() {
    setForm(initialForm);
    setErrors(emptyErrors);
    setPreview(null);
  }

  return (
    <>
      <PageHeader
        titleKey="organization.warehousesNew.title"
        descriptionKey="organization.warehousesNew.description"
        eyebrowKey="common.context.organization"
        actions={
          <Link className="button button--quiet button--sm" to="/organization/warehouses">
            <ArrowLeft size={14} aria-hidden="true" />
            {t('organization.warehousesNew.backToWarehouses')}
          </Link>
        }
      />
      <DemoNotice compact />

      {loader.status === 'error' ? (
        <ErrorState onRetry={loader.reload} />
      ) : loader.status === 'loading' ? (
        <LoadingState />
      ) : (
        <>
          <div style={statStyle}>
            <StatCard
              labelKey="organization.warehousesNew.existingCount"
              value={formatNumber(loader.data.length)}
              hintKey="organization.warehousesNew.existingHint"
              icon={Warehouse}
              tone="info"
            />
          </div>

          <ChartCard
            titleKey="organization.warehousesNew.formTitle"
            subtitleKey="organization.warehousesNew.formDescription"
          >
            <div style={formGridStyle}>
              <FormField
                labelKey="organization.warehousesNew.nameLabel"
                hintKey="organization.warehousesNew.nameHint"
                required
                htmlFor="warehouse-new-name"
                errorKey={errors.name ? 'organization.warehousesNew.errorName' : undefined}
              >
                <input
                  id="warehouse-new-name"
                  value={form.name}
                  onChange={(event) => {
                    setForm((current) => ({ ...current, name: event.target.value }));
                    setErrors((current) => ({ ...current, name: false }));
                  }}
                />
              </FormField>
              <FormField
                labelKey="organization.warehousesNew.codeLabel"
                hintKey="organization.warehousesNew.codeHint"
                required
                htmlFor="warehouse-new-code"
                errorKey={errors.code ? 'organization.warehousesNew.errorCode' : undefined}
              >
                <input
                  id="warehouse-new-code"
                  dir="ltr"
                  value={form.code}
                  onChange={(event) => {
                    setForm((current) => ({ ...current, code: event.target.value }));
                    setErrors((current) => ({ ...current, code: false }));
                  }}
                />
              </FormField>
              <FormField
                labelKey="organization.warehousesNew.statusLabel"
                htmlFor="warehouse-new-status"
              >
                <select
                  id="warehouse-new-status"
                  value={form.status}
                  onChange={(event) =>
                    setForm((current) => ({
                      ...current,
                      status: event.target.value as EntityStatus,
                    }))
                  }
                >
                  {statusOptions.map((status) => (
                    <option key={status} value={status}>
                      {t(statusLabels[status])}
                    </option>
                  ))}
                </select>
              </FormField>
              <FormField
                labelKey="organization.warehousesNew.productCountLabel"
                required
                htmlFor="warehouse-new-products"
                errorKey={
                  errors.productCount ? 'organization.warehousesNew.errorProductCount' : undefined
                }
              >
                <input
                  id="warehouse-new-products"
                  type="number"
                  min="0"
                  dir="ltr"
                  value={form.productCount}
                  onChange={(event) => {
                    setForm((current) => ({ ...current, productCount: event.target.value }));
                    setErrors((current) => ({ ...current, productCount: false }));
                  }}
                />
              </FormField>
              <FormField
                labelKey="organization.warehousesNew.stockValueLabel"
                required
                htmlFor="warehouse-new-stock-value"
                errorKey={
                  errors.stockValue ? 'organization.warehousesNew.errorStockValue' : undefined
                }
              >
                <input
                  id="warehouse-new-stock-value"
                  type="number"
                  min="0"
                  step="0.01"
                  dir="ltr"
                  value={form.stockValue}
                  onChange={(event) => {
                    setForm((current) => ({ ...current, stockValue: event.target.value }));
                    setErrors((current) => ({ ...current, stockValue: false }));
                  }}
                />
              </FormField>
            </div>
            <div style={{ display: 'flex', gap: '8px', marginBlockStart: '20px' }}>
              <button type="button" className="button button--primary" onClick={submitPreview}>
                {t('organization.warehousesNew.submit')}
              </button>
              <button type="button" className="button button--quiet" onClick={resetForm}>
                {t('common.actions.reset')}
              </button>
            </div>
          </ChartCard>

          {preview ? (
            <div style={{ marginBlockStart: '20px' }}>
              <ChartCard
                titleKey="organization.warehousesNew.previewTitle"
                subtitleKey="organization.warehousesNew.previewDescription"
              >
                <div style={summaryStyle}>
                  <div className="profile-detail">
                    <span>{t('common.labels.name')}</span>
                    <strong>{preview.name}</strong>
                  </div>
                  <div className="profile-detail">
                    <span>{t('common.labels.code')}</span>
                    <strong dir="ltr">{preview.code}</strong>
                  </div>
                  <div className="profile-detail">
                    <span>{t('common.labels.status')}</span>
                    <StatusBadge
                      tone={statusTones[preview.status]}
                      labelKey={statusLabels[preview.status]}
                    />
                  </div>
                  <div className="profile-detail">
                    <span>{t('organization.warehousesNew.productCountLabel')}</span>
                    <strong dir="ltr">{formatNumber(Number(preview.productCount))}</strong>
                  </div>
                  <div className="profile-detail">
                    <span>{t('organization.warehousesNew.stockValueLabel')}</span>
                    <strong dir="ltr">{formatCurrency(Number(preview.stockValue))}</strong>
                  </div>
                </div>
              </ChartCard>
            </div>
          ) : null}
        </>
      )}
    </>
  );
}

export default CreateWarehousePage;
