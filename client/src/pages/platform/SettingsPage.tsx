import { useState } from 'react';
import { ChartCard, DemoNotice, FormField, PageHeader, StatusBadge } from '@/components/ui';
import { useApp } from '@/context/AppContext';
import { demoPlans } from '@/data/demoData';
import { t } from '@/i18n';

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

type NumberFormat = 'latin' | 'arabic';
type DateFormat = 'short' | 'long';
type Timezone = 'riyadh' | 'dubai';

interface SettingsState {
  platformName: string;
  supportEmail: string;
  defaultPlanId: string;
  timezone: Timezone;
  numberFormat: NumberFormat;
  dateFormat: DateFormat;
  emailAlerts: boolean;
  lowStockAlerts: boolean;
  weeklySummary: boolean;
}

const initialSettings: SettingsState = {
  platformName: t('common.appName'),
  supportEmail: 'support@example.invalid',
  defaultPlanId: demoPlans[0]?.id ?? '',
  timezone: 'riyadh',
  numberFormat: 'latin',
  dateFormat: 'short',
  emailAlerts: true,
  lowStockAlerts: true,
  weeklySummary: false,
};

export function PlatformSettingsPage() {
  const { showToast } = useApp();
  const [settings, setSettings] = useState<SettingsState>(initialSettings);
  const [errors, setErrors] = useState({ platformName: false, supportEmail: false });
  const [saved, setSaved] = useState(false);

  function update<K extends keyof SettingsState>(field: K, value: SettingsState[K]) {
    setSettings((current) => ({ ...current, [field]: value }));
    setSaved(false);
  }

  function savePreview() {
    const nameValid = settings.platformName.trim().length > 1;
    const emailValid = /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(settings.supportEmail.trim());
    setErrors({ platformName: !nameValid, supportEmail: !emailValid });
    if (!nameValid || !emailValid) {
      return;
    }
    setSaved(true);
    showToast('platform.settings.saveToast', 'info');
  }

  const defaultPlan = demoPlans.find((plan) => plan.id === settings.defaultPlanId);

  return (
    <>
      <PageHeader
        titleKey="platform.settings.title"
        descriptionKey="platform.settings.description"
        eyebrowKey="common.context.platform"
        actions={
          <button type="button" className="button button--primary button--sm" onClick={savePreview}>
            {t('platform.settings.savePreview')}
          </button>
        }
      />
      <DemoNotice compact />

      <div style={groupGridStyle}>
        <ChartCard
          titleKey="platform.settings.generalTitle"
          subtitleKey="platform.settings.generalDescription"
        >
          <div style={formGridStyle}>
            <FormField
              labelKey="platform.settings.platformNameLabel"
              hintKey="platform.settings.platformNameHint"
              required
              htmlFor="settings-platform-name"
              errorKey={errors.platformName ? 'platform.settings.errorName' : undefined}
            >
              <input
                id="settings-platform-name"
                value={settings.platformName}
                onChange={(event) => {
                  update('platformName', event.target.value);
                  setErrors((current) => ({ ...current, platformName: false }));
                }}
              />
            </FormField>
            <FormField
              labelKey="platform.settings.supportEmailLabel"
              hintKey="platform.settings.supportEmailHint"
              required
              htmlFor="settings-support-email"
              errorKey={errors.supportEmail ? 'platform.settings.errorEmail' : undefined}
            >
              <input
                id="settings-support-email"
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
              labelKey="platform.settings.defaultPlanLabel"
              hintKey="platform.settings.defaultPlanHint"
              htmlFor="settings-default-plan"
            >
              <select
                id="settings-default-plan"
                value={settings.defaultPlanId}
                onChange={(event) => update('defaultPlanId', event.target.value)}
              >
                {demoPlans.map((plan) => (
                  <option key={plan.id} value={plan.id}>
                    {plan.name}
                  </option>
                ))}
              </select>
            </FormField>
          </div>
        </ChartCard>

        <ChartCard
          titleKey="platform.settings.localeTitle"
          subtitleKey="platform.settings.localeDescription"
        >
          <div style={formGridStyle}>
            <FormField labelKey="platform.settings.timezoneLabel" htmlFor="settings-timezone">
              <select
                id="settings-timezone"
                value={settings.timezone}
                onChange={(event) => update('timezone', event.target.value as Timezone)}
              >
                <option value="riyadh">{t('platform.settings.zoneRiyadh')}</option>
                <option value="dubai">{t('platform.settings.zoneDubai')}</option>
              </select>
            </FormField>
            <FormField
              labelKey="platform.settings.numberFormatLabel"
              hintKey="platform.settings.numberFormatHint"
              htmlFor="settings-number-format"
            >
              <select
                id="settings-number-format"
                value={settings.numberFormat}
                onChange={(event) => update('numberFormat', event.target.value as NumberFormat)}
              >
                <option value="latin">{t('platform.settings.optionLatinDigits')}</option>
                <option value="arabic">{t('platform.settings.optionArabicDigits')}</option>
              </select>
            </FormField>
            <FormField
              labelKey="platform.settings.dateFormatLabel"
              hintKey="platform.settings.dateFormatHint"
              htmlFor="settings-date-format"
            >
              <select
                id="settings-date-format"
                value={settings.dateFormat}
                onChange={(event) => update('dateFormat', event.target.value as DateFormat)}
              >
                <option value="short">{t('platform.settings.optionShortDate')}</option>
                <option value="long">{t('platform.settings.optionLongDate')}</option>
              </select>
            </FormField>
          </div>
        </ChartCard>

        <ChartCard
          titleKey="platform.settings.alertsTitle"
          subtitleKey="platform.settings.alertsDescription"
        >
          <div style={formGridStyle}>
            <div>
              <label style={toggleStyle} htmlFor="settings-email-alerts">
                <input
                  id="settings-email-alerts"
                  type="checkbox"
                  checked={settings.emailAlerts}
                  onChange={(event) => update('emailAlerts', event.target.checked)}
                />
                <span className="form-field__label">{t('platform.settings.emailAlertsLabel')}</span>
              </label>
              <span className="form-field__hint">{t('platform.settings.emailAlertsHint')}</span>
            </div>
            <div>
              <label style={toggleStyle} htmlFor="settings-low-stock">
                <input
                  id="settings-low-stock"
                  type="checkbox"
                  checked={settings.lowStockAlerts}
                  onChange={(event) => update('lowStockAlerts', event.target.checked)}
                />
                <span className="form-field__label">
                  {t('platform.settings.lowStockAlertsLabel')}
                </span>
              </label>
              <span className="form-field__hint">{t('platform.settings.lowStockAlertsHint')}</span>
            </div>
            <div>
              <label style={toggleStyle} htmlFor="settings-weekly-summary">
                <input
                  id="settings-weekly-summary"
                  type="checkbox"
                  checked={settings.weeklySummary}
                  onChange={(event) => update('weeklySummary', event.target.checked)}
                />
                <span className="form-field__label">
                  {t('platform.settings.weeklySummaryLabel')}
                </span>
              </label>
              <span className="form-field__hint">{t('platform.settings.weeklySummaryHint')}</span>
            </div>
          </div>
        </ChartCard>
      </div>

      {saved ? (
        <ChartCard
          titleKey="platform.settings.previewSummaryTitle"
          subtitleKey="platform.settings.previewSummaryDescription"
        >
          <div style={summaryStyle}>
            <div className="profile-detail">
              <span>{t('platform.settings.platformNameLabel')}</span>
              <strong>{settings.platformName}</strong>
            </div>
            <div className="profile-detail">
              <span>{t('platform.settings.supportEmailLabel')}</span>
              <strong dir="ltr">{settings.supportEmail}</strong>
            </div>
            <div className="profile-detail">
              <span>{t('platform.settings.defaultPlanLabel')}</span>
              <strong>{defaultPlan?.name ?? settings.defaultPlanId}</strong>
            </div>
            <div className="profile-detail">
              <span>{t('platform.settings.timezoneLabel')}</span>
              <strong>
                {t(
                  settings.timezone === 'riyadh'
                    ? 'platform.settings.zoneRiyadh'
                    : 'platform.settings.zoneDubai',
                )}
              </strong>
            </div>
            <div className="profile-detail">
              <span>{t('common.labels.status')}</span>
              <StatusBadge tone="info" labelKey="common.demo.previewOnly" />
            </div>
          </div>
        </ChartCard>
      ) : null}
    </>
  );
}

export default PlatformSettingsPage;
