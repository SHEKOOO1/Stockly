import { Bell, RotateCcw, Save, Settings } from 'lucide-react';
import { useState } from 'react';
import {
  ChartCard,
  ConfirmDialog,
  DemoNotice,
  FormField,
  PageHeader,
  StatCard,
  StatusBadge,
} from '@/components/ui';
import { useApp } from '@/context/AppContext';
import { t } from '@/i18n';
import { formatNumber } from '@/lib/format';

const formGridStyle = {
  display: 'grid',
  gridTemplateColumns: 'repeat(auto-fit, minmax(220px, 1fr))',
  gap: '16px',
} as const;

const statGridStyle = {
  display: 'grid',
  gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))',
  gap: '16px',
  marginBlockEnd: '20px',
} as const;

const panelGridStyle = {
  display: 'grid',
  gridTemplateColumns: 'repeat(auto-fit, minmax(340px, 1fr))',
  gap: '16px',
  marginBlockEnd: '20px',
} as const;

const actionRowStyle = {
  display: 'flex',
  flexWrap: 'wrap',
  gap: '8px',
} as const;

interface SettingsFormState {
  displayName: string;
  code: string;
  autoAlerts: boolean;
  expiryWindow: string;
  lowStockThreshold: string;
  emailAlerts: boolean;
  dailyDigest: boolean;
}

function defaultSettings(name: string, code: string): SettingsFormState {
  return {
    displayName: name,
    code,
    autoAlerts: true,
    expiryWindow: '30',
    lowStockThreshold: '20',
    emailAlerts: true,
    dailyDigest: false,
  };
}

export function WarehouseSettingsPage() {
  const { selectedWarehouse, showToast } = useApp();
  const [form, setForm] = useState<SettingsFormState>(() =>
    defaultSettings(selectedWarehouse.name, selectedWarehouse.code),
  );
  const [nameError, setNameError] = useState(false);
  const [codeError, setCodeError] = useState(false);
  const [expiryError, setExpiryError] = useState(false);
  const [thresholdError, setThresholdError] = useState(false);
  const [resetting, setResetting] = useState(false);

  function savePreview() {
    const nameValid = form.displayName.trim().length >= 3;
    const codeValid = form.code.trim().length >= 2;
    const expiryWindow = Number(form.expiryWindow);
    const threshold = Number(form.lowStockThreshold);
    const expiryValid =
      form.expiryWindow.trim() !== '' && Number.isInteger(expiryWindow) && expiryWindow > 0;
    const thresholdValid =
      form.lowStockThreshold.trim() !== '' && Number.isInteger(threshold) && threshold >= 0;
    setNameError(!nameValid);
    setCodeError(!codeValid);
    setExpiryError(!expiryValid);
    setThresholdError(!thresholdValid);
    if (!nameValid || !codeValid || !expiryValid || !thresholdValid) {
      return;
    }
    showToast('warehouse.shared.updateToast', 'info');
  }

  function confirmReset() {
    setForm(defaultSettings(selectedWarehouse.name, selectedWarehouse.code));
    setNameError(false);
    setCodeError(false);
    setExpiryError(false);
    setThresholdError(false);
    showToast('warehouse.shared.statusToast', 'info');
    setResetting(false);
  }

  return (
    <>
      <PageHeader
        titleKey="warehouse.settings.title"
        descriptionKey="warehouse.settings.description"
        eyebrowKey="common.context.warehouse"
        actions={
          <>
            <span className="status-badge status-badge--info">{selectedWarehouse.name}</span>
            <span className="demo-badge">{t('common.demo.previewOnly')}</span>
          </>
        }
      />
      <DemoNotice compact messageKey="warehouse.shared.localNotice" />

      <div style={statGridStyle}>
        <StatCard
          labelKey="warehouse.settings.generalTitle"
          value={form.displayName}
          icon={Settings}
          tone="info"
        />
        <StatCard
          labelKey="warehouse.settings.previewSummary"
          value={form.code}
          icon={Bell}
          tone="healthy"
        />
      </div>

      <div style={panelGridStyle}>
        <ChartCard
          titleKey="warehouse.settings.generalSection"
          subtitleKey="warehouse.settings.generalTitle"
        >
          <div style={formGridStyle}>
            <FormField
              labelKey="warehouse.settings.displayNameField"
              required
              errorKey={nameError ? 'common.form.fieldRequired' : undefined}
            >
              <input
                value={form.displayName}
                onChange={(event) => {
                  setForm((current) => ({ ...current, displayName: event.target.value }));
                  setNameError(false);
                }}
              />
            </FormField>
            <FormField
              labelKey="warehouse.settings.codeField"
              required
              errorKey={codeError ? 'common.form.fieldRequired' : undefined}
            >
              <input
                dir="ltr"
                value={form.code}
                onChange={(event) => {
                  setForm((current) => ({ ...current, code: event.target.value }));
                  setCodeError(false);
                }}
              />
            </FormField>
          </div>
        </ChartCard>

        <ChartCard
          titleKey="warehouse.settings.operationsSection"
          subtitleKey="warehouse.settings.operationsTitle"
        >
          <div style={formGridStyle}>
            <FormField
              labelKey="warehouse.settings.autoAlertsField"
              hintKey="warehouse.settings.autoAlertsHint"
            >
              <label>
                <input
                  type="checkbox"
                  checked={form.autoAlerts}
                  onChange={(event) =>
                    setForm((current) => ({ ...current, autoAlerts: event.target.checked }))
                  }
                />
              </label>
            </FormField>
            <FormField
              labelKey="warehouse.settings.expiryWindowField"
              required
              errorKey={expiryError ? 'common.form.positiveNumber' : undefined}
            >
              <input
                type="number"
                min="1"
                dir="ltr"
                value={form.expiryWindow}
                onChange={(event) => {
                  setForm((current) => ({ ...current, expiryWindow: event.target.value }));
                  setExpiryError(false);
                }}
              />
            </FormField>
            <FormField
              labelKey="warehouse.settings.lowStockThresholdField"
              required
              errorKey={thresholdError ? 'common.form.invalidNumber' : undefined}
            >
              <input
                type="number"
                min="0"
                dir="ltr"
                value={form.lowStockThreshold}
                onChange={(event) => {
                  setForm((current) => ({ ...current, lowStockThreshold: event.target.value }));
                  setThresholdError(false);
                }}
              />
            </FormField>
          </div>
        </ChartCard>

        <ChartCard
          titleKey="warehouse.settings.notificationsSection"
          subtitleKey="warehouse.settings.notificationsTitle"
        >
          <div style={formGridStyle}>
            <FormField labelKey="warehouse.settings.emailAlertsField">
              <label>
                <input
                  type="checkbox"
                  checked={form.emailAlerts}
                  onChange={(event) =>
                    setForm((current) => ({ ...current, emailAlerts: event.target.checked }))
                  }
                />
              </label>
            </FormField>
            <FormField labelKey="warehouse.settings.dailyDigestField">
              <label>
                <input
                  type="checkbox"
                  checked={form.dailyDigest}
                  onChange={(event) =>
                    setForm((current) => ({ ...current, dailyDigest: event.target.checked }))
                  }
                />
              </label>
            </FormField>
          </div>
        </ChartCard>
      </div>

      <ChartCard
        titleKey="warehouse.settings.previewSummary"
        subtitleKey="common.form.localPreview"
      >
        <div style={statGridStyle}>
          <div>
            <strong>{t('warehouse.settings.warehouseCodeLabel')}</strong>
            <p dir="ltr">{form.code}</p>
          </div>
          <div>
            <strong>{t('warehouse.settings.alertsEnabledLabel')}</strong>
            <p>
              <StatusBadge
                tone={form.autoAlerts ? 'healthy' : 'neutral'}
                labelKey={form.autoAlerts ? 'common.status.active' : 'common.status.inactive'}
              />
            </p>
          </div>
          <div>
            <strong>{t('warehouse.settings.expiryWindowLabel')}</strong>
            <p dir="ltr">{formatNumber(form.expiryWindow)}</p>
          </div>
        </div>
        <div style={actionRowStyle}>
          <button type="button" className="button button--primary button--sm" onClick={savePreview}>
            <Save size={14} aria-hidden="true" />
            {t('warehouse.shared.savePreview')}
          </button>
          <button
            type="button"
            className="button button--quiet button--sm"
            onClick={() => setResetting(true)}
          >
            <RotateCcw size={14} aria-hidden="true" />
            {t('warehouse.settings.resetAction')}
          </button>
        </div>
      </ChartCard>

      <ConfirmDialog
        open={resetting}
        onClose={() => setResetting(false)}
        onConfirm={confirmReset}
        titleKey="warehouse.settings.confirmResetTitle"
        messageKey="warehouse.settings.confirmResetMessage"
      />
    </>
  );
}

export default WarehouseSettingsPage;
