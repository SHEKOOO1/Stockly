import { useEffect, useState } from 'react';
import {
  ChartCard,
  DemoNotice,
  ErrorState,
  FormField,
  LoadingState,
  PageHeader,
  StatusBadge,
} from '@/components/ui';
import { useApp } from '@/context/AppContext';
import { t } from '@/i18n';
import { formatNumber } from '@/lib/format';
import { useDemoLoader } from '@/lib/useDemoLoader';

const groupGridStyle = {
  display: 'grid',
  gridTemplateColumns: 'repeat(auto-fit, minmax(320px, 1fr))',
  gap: '16px',
  marginBlockEnd: '20px',
} as const;

const formGridStyle = {
  display: 'grid',
  gap: '16px',
} as const;

const summaryStyle = {
  display: 'grid',
  gap: '8px',
} as const;

const toggleStyle = {
  display: 'flex',
  alignItems: 'center',
  gap: '8px',
} as const;

type Timezone = 'utc' | 'riyadh';
type NumberFormat = 'latin' | 'arabic';

interface OrganizationSettings {
  displayName: string;
  supportEmail: string;
  defaultWarehouseId: string;
  timezone: Timezone;
  numberFormat: NumberFormat;
  lowStock: string;
  expiryWindow: string;
  emailAlerts: boolean;
  lowStockAlerts: boolean;
  weeklySummary: boolean;
}

interface FormErrors {
  displayName: boolean;
  supportEmail: boolean;
  lowStock: boolean;
  expiryWindow: boolean;
}

const emptyErrors: FormErrors = {
  displayName: false,
  supportEmail: false,
  lowStock: false,
  expiryWindow: false,
};

const timezoneOptions: readonly Timezone[] = ['utc', 'riyadh'];
const numberFormatOptions: readonly NumberFormat[] = ['latin', 'arabic'];

const timezoneLabels = {
  utc: 'organization.settings.timezoneUtc',
  riyadh: 'organization.settings.timezoneRiyadh',
} as const;

const numberFormatLabels = {
  latin: 'organization.settings.digitLatin',
  arabic: 'organization.settings.digitArabic',
} as const;

export function OrganizationSettingsPage() {
  const { availableWarehouses, selectedOrganization, selectedWarehouse, showToast } = useApp();
  const loader = useDemoLoader(availableWarehouses, { delayMs: 180 });
  const [settings, setSettings] = useState<OrganizationSettings>({
    displayName: selectedOrganization.name,
    supportEmail: 'support@example.invalid',
    defaultWarehouseId: selectedWarehouse.id,
    timezone: 'utc',
    numberFormat: 'latin',
    lowStock: '10',
    expiryWindow: '30',
    emailAlerts: true,
    lowStockAlerts: true,
    weeklySummary: false,
  });
  const [errors, setErrors] = useState<FormErrors>(emptyErrors);

  useEffect(() => {
    setSettings((current) => ({
      ...current,
      displayName: selectedOrganization.name,
      defaultWarehouseId: selectedWarehouse.id,
    }));
    setErrors(emptyErrors);
  }, [selectedOrganization.id, selectedOrganization.name, selectedWarehouse.id]);

  function update<K extends keyof OrganizationSettings>(field: K, value: OrganizationSettings[K]) {
    setSettings((current) => ({ ...current, [field]: value }));
  }

  function savePreview() {
    const lowStockValue = Number(settings.lowStock);
    const expiryWindowValue = Number(settings.expiryWindow);
    const nextErrors: FormErrors = {
      displayName: settings.displayName.trim().length < 3,
      supportEmail: !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(settings.supportEmail.trim()),
      lowStock: !Number.isInteger(lowStockValue) || lowStockValue < 0,
      expiryWindow: !Number.isInteger(expiryWindowValue) || expiryWindowValue <= 0,
    };
    setErrors(nextErrors);
    if (Object.values(nextErrors).some(Boolean)) {
      return;
    }
    showToast('organization.settings.toastPreview', 'info');
  }

  const defaultWarehouse = availableWarehouses.find(
    (warehouse) => warehouse.id === settings.defaultWarehouseId,
  );

  return (
    <>
      <PageHeader
        titleKey="organization.settings.title"
        descriptionKey="organization.settings.description"
        eyebrowKey="common.context.organization"
        actions={
          <button type="button" className="button button--primary button--sm" onClick={savePreview}>
            {t('organization.settings.savePreview')}
          </button>
        }
      />
      <DemoNotice compact />

      {loader.status === 'error' ? (
        <ErrorState onRetry={loader.reload} />
      ) : loader.status === 'loading' ? (
        <LoadingState />
      ) : (
        <>
          <div style={groupGridStyle}>
            <ChartCard
              titleKey="organization.settings.generalTitle"
              subtitleKey="organization.settings.generalDescription"
            >
              <div style={formGridStyle}>
                <FormField
                  labelKey="organization.settings.nameLabel"
                  hintKey="organization.settings.nameHint"
                  required
                  htmlFor="organization-settings-name"
                  errorKey={errors.displayName ? 'organization.settings.errorName' : undefined}
                >
                  <input
                    id="organization-settings-name"
                    value={settings.displayName}
                    onChange={(event) => {
                      update('displayName', event.target.value);
                      setErrors((current) => ({ ...current, displayName: false }));
                    }}
                  />
                </FormField>
                <FormField
                  labelKey="organization.settings.supportEmailLabel"
                  hintKey="organization.settings.supportEmailHint"
                  required
                  htmlFor="organization-settings-email"
                  errorKey={errors.supportEmail ? 'organization.settings.errorEmail' : undefined}
                >
                  <input
                    id="organization-settings-email"
                    type="email"
                    dir="ltr"
                    value={settings.supportEmail}
                    onChange={(event) => {
                      update('supportEmail', event.target.value);
                      setErrors((current) => ({ ...current, supportEmail: false }));
                    }}
                  />
                </FormField>
                <FormField
                  labelKey="organization.settings.warehouseLabel"
                  hintKey="organization.settings.warehouseHint"
                  htmlFor="organization-settings-warehouse"
                >
                  <select
                    id="organization-settings-warehouse"
                    value={settings.defaultWarehouseId}
                    onChange={(event) => update('defaultWarehouseId', event.target.value)}
                  >
                    {loader.data.map((warehouse) => (
                      <option key={warehouse.id} value={warehouse.id}>
                        {warehouse.name}
                      </option>
                    ))}
                  </select>
                </FormField>
                <FormField
                  labelKey="organization.settings.timezoneLabel"
                  hintKey="organization.settings.timezoneHint"
                  htmlFor="organization-settings-timezone"
                >
                  <select
                    id="organization-settings-timezone"
                    value={settings.timezone}
                    onChange={(event) => update('timezone', event.target.value as Timezone)}
                  >
                    {timezoneOptions.map((timezone) => (
                      <option key={timezone} value={timezone}>
                        {t(timezoneLabels[timezone])}
                      </option>
                    ))}
                  </select>
                </FormField>
                <FormField
                  labelKey="organization.settings.digitFormatLabel"
                  hintKey="organization.settings.digitFormatHint"
                  htmlFor="organization-settings-digits"
                >
                  <select
                    id="organization-settings-digits"
                    value={settings.numberFormat}
                    onChange={(event) => update('numberFormat', event.target.value as NumberFormat)}
                  >
                    {numberFormatOptions.map((format) => (
                      <option key={format} value={format}>
                        {t(numberFormatLabels[format])}
                      </option>
                    ))}
                  </select>
                </FormField>
              </div>
            </ChartCard>

            <ChartCard
              titleKey="organization.settings.operationsTitle"
              subtitleKey="organization.settings.operationsDescription"
            >
              <div style={formGridStyle}>
                <FormField
                  labelKey="organization.settings.lowStockLabel"
                  hintKey="organization.settings.lowStockHint"
                  required
                  htmlFor="organization-settings-low-stock"
                  errorKey={errors.lowStock ? 'organization.settings.errorLowStock' : undefined}
                >
                  <input
                    id="organization-settings-low-stock"
                    type="number"
                    min="0"
                    dir="ltr"
                    value={settings.lowStock}
                    onChange={(event) => {
                      update('lowStock', event.target.value);
                      setErrors((current) => ({ ...current, lowStock: false }));
                    }}
                  />
                </FormField>
                <FormField
                  labelKey="organization.settings.expiryWindowLabel"
                  hintKey="organization.settings.expiryWindowHint"
                  required
                  htmlFor="organization-settings-expiry-window"
                  errorKey={
                    errors.expiryWindow ? 'organization.settings.errorExpiryWindow' : undefined
                  }
                >
                  <input
                    id="organization-settings-expiry-window"
                    type="number"
                    min="1"
                    dir="ltr"
                    value={settings.expiryWindow}
                    onChange={(event) => {
                      update('expiryWindow', event.target.value);
                      setErrors((current) => ({ ...current, expiryWindow: false }));
                    }}
                  />
                </FormField>
              </div>
            </ChartCard>

            <ChartCard
              titleKey="organization.settings.alertsTitle"
              subtitleKey="organization.settings.alertsDescription"
            >
              <div style={formGridStyle}>
                <div>
                  <label style={toggleStyle} htmlFor="organization-settings-email-alerts">
                    <input
                      id="organization-settings-email-alerts"
                      type="checkbox"
                      checked={settings.emailAlerts}
                      onChange={(event) => update('emailAlerts', event.target.checked)}
                    />
                    <span className="form-field__label">
                      {t('organization.settings.emailAlertsLabel')}
                    </span>
                  </label>
                  <span className="form-field__hint">
                    {t('organization.settings.emailAlertsHint')}
                  </span>
                </div>
                <div>
                  <label style={toggleStyle} htmlFor="organization-settings-low-stock-alerts">
                    <input
                      id="organization-settings-low-stock-alerts"
                      type="checkbox"
                      checked={settings.lowStockAlerts}
                      onChange={(event) => update('lowStockAlerts', event.target.checked)}
                    />
                    <span className="form-field__label">
                      {t('organization.settings.lowStockAlertsLabel')}
                    </span>
                  </label>
                  <span className="form-field__hint">
                    {t('organization.settings.lowStockAlertsHint')}
                  </span>
                </div>
                <div>
                  <label style={toggleStyle} htmlFor="organization-settings-weekly-summary">
                    <input
                      id="organization-settings-weekly-summary"
                      type="checkbox"
                      checked={settings.weeklySummary}
                      onChange={(event) => update('weeklySummary', event.target.checked)}
                    />
                    <span className="form-field__label">
                      {t('organization.settings.weeklySummaryLabel')}
                    </span>
                  </label>
                  <span className="form-field__hint">
                    {t('organization.settings.weeklySummaryHint')}
                  </span>
                </div>
              </div>
            </ChartCard>
          </div>

          <ChartCard
            titleKey="organization.settings.previewTitle"
            subtitleKey="organization.settings.previewDescription"
          >
            <div style={summaryStyle}>
              <div className="profile-detail">
                <span>{t('organization.settings.nameLabel')}</span>
                <strong>{settings.displayName}</strong>
              </div>
              <div className="profile-detail">
                <span>{t('organization.settings.supportEmailLabel')}</span>
                <strong dir="ltr">{settings.supportEmail}</strong>
              </div>
              <div className="profile-detail">
                <span>{t('organization.settings.warehouseLabel')}</span>
                <strong>{defaultWarehouse?.name ?? settings.defaultWarehouseId}</strong>
              </div>
              <div className="profile-detail">
                <span>{t('organization.settings.timezoneLabel')}</span>
                <strong>{t(timezoneLabels[settings.timezone])}</strong>
              </div>
              <div className="profile-detail">
                <span>{t('organization.settings.digitFormatLabel')}</span>
                <strong>{t(numberFormatLabels[settings.numberFormat])}</strong>
              </div>
              <div className="profile-detail">
                <span>{t('organization.settings.lowStockLabel')}</span>
                <strong dir="ltr">{formatNumber(Number(settings.lowStock))}</strong>
              </div>
              <div className="profile-detail">
                <span>{t('organization.settings.expiryWindowLabel')}</span>
                <strong dir="ltr">{formatNumber(Number(settings.expiryWindow))}</strong>
              </div>
              <div className="profile-detail">
                <span>{t('organization.settings.emailAlertsLabel')}</span>
                <strong>
                  {settings.emailAlerts ? t('common.status.active') : t('common.status.inactive')}
                </strong>
              </div>
              <div className="profile-detail">
                <span>{t('organization.settings.lowStockAlertsLabel')}</span>
                <strong>
                  {settings.lowStockAlerts
                    ? t('common.status.active')
                    : t('common.status.inactive')}
                </strong>
              </div>
              <div className="profile-detail">
                <span>{t('organization.settings.weeklySummaryLabel')}</span>
                <strong>
                  {settings.weeklySummary ? t('common.status.active') : t('common.status.inactive')}
                </strong>
              </div>
              <div className="profile-detail">
                <span>{t('common.labels.status')}</span>
                <StatusBadge tone="info" labelKey="common.demo.previewOnly" />
              </div>
            </div>
          </ChartCard>
        </>
      )}
    </>
  );
}

export default OrganizationSettingsPage;
