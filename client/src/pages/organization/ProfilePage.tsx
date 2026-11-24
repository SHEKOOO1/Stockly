import { useEffect, useState } from 'react';
import {
  ChartCard,
  DemoNotice,
  ErrorState,
  FormField,
  LoadingState,
  PageHeader,
  StatusBadge,
  Tabs,
  type TabItem,
} from '@/components/ui';
import { useApp } from '@/context/AppContext';
import { demoPlans } from '@/data/demoData';
import { t, type TranslationKey } from '@/i18n';
import { formatCurrency, formatDate, formatNumber } from '@/lib/format';
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

interface ProfileForm {
  name: string;
  code: string;
  planId: string;
  status: EntityStatus;
}

interface FormErrors {
  name: boolean;
  code: boolean;
  planId: boolean;
}

const emptyErrors: FormErrors = {
  name: false,
  code: false,
  planId: false,
};

export function OrganizationProfilePage() {
  const { selectedOrganization, selectedWarehouse, showToast } = useApp();
  const planLoader = useDemoLoader(demoPlans, { delayMs: 180 });
  const [form, setForm] = useState<ProfileForm>({
    name: selectedOrganization.name,
    code: selectedOrganization.code,
    planId: selectedOrganization.planId,
    status: selectedOrganization.status,
  });
  const [errors, setErrors] = useState<FormErrors>(emptyErrors);
  const [preview, setPreview] = useState<ProfileForm | null>(null);

  useEffect(() => {
    setForm({
      name: selectedOrganization.name,
      code: selectedOrganization.code,
      planId: selectedOrganization.planId,
      status: selectedOrganization.status,
    });
    setErrors(emptyErrors);
    setPreview(null);
  }, [
    selectedOrganization.id,
    selectedOrganization.name,
    selectedOrganization.code,
    selectedOrganization.planId,
    selectedOrganization.status,
  ]);

  function submitPreview() {
    const nextErrors: FormErrors = {
      name: form.name.trim().length < 3,
      code: !/^[A-Za-z0-9]{3,12}$/.test(form.code.trim()),
      planId: !demoPlans.some((plan) => plan.id === form.planId),
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
    showToast('organization.profile.toastPreview', 'info');
  }

  const selectedPlan = demoPlans.find((plan) => plan.id === selectedOrganization.planId);
  const previewPlan = preview ? demoPlans.find((plan) => plan.id === preview.planId) : undefined;

  const profileTab: TabItem = {
    id: 'profile',
    labelKey: 'organization.profile.profileTab',
    content: (
      <>
        <ChartCard
          titleKey="organization.profile.formTitle"
          subtitleKey="organization.profile.formDescription"
        >
          <div style={formGridStyle}>
            <FormField
              labelKey="organization.profile.nameLabel"
              hintKey="organization.profile.nameHint"
              required
              htmlFor="organization-profile-name"
              errorKey={errors.name ? 'organization.profile.errorName' : undefined}
            >
              <input
                id="organization-profile-name"
                value={form.name}
                onChange={(event) => {
                  setForm((current) => ({ ...current, name: event.target.value }));
                  setErrors((current) => ({ ...current, name: false }));
                }}
              />
            </FormField>
            <FormField
              labelKey="organization.profile.codeLabel"
              hintKey="organization.profile.codeHint"
              required
              htmlFor="organization-profile-code"
              errorKey={errors.code ? 'organization.profile.errorCode' : undefined}
            >
              <input
                id="organization-profile-code"
                dir="ltr"
                value={form.code}
                onChange={(event) => {
                  setForm((current) => ({ ...current, code: event.target.value }));
                  setErrors((current) => ({ ...current, code: false }));
                }}
              />
            </FormField>
            <FormField
              labelKey="organization.profile.planLabel"
              hintKey="organization.profile.planHint"
              required
              htmlFor="organization-profile-plan"
              errorKey={errors.planId ? 'organization.profile.errorPlan' : undefined}
            >
              <select
                id="organization-profile-plan"
                value={form.planId}
                onChange={(event) => {
                  setForm((current) => ({ ...current, planId: event.target.value }));
                  setErrors((current) => ({ ...current, planId: false }));
                }}
              >
                {planLoader.data.map((plan) => (
                  <option key={plan.id} value={plan.id}>
                    {plan.name}
                  </option>
                ))}
              </select>
            </FormField>
            <FormField
              labelKey="organization.profile.statusLabel"
              htmlFor="organization-profile-status"
            >
              <select
                id="organization-profile-status"
                value={form.status}
                onChange={(event) =>
                  setForm((current) => ({ ...current, status: event.target.value as EntityStatus }))
                }
              >
                {statusOptions.map((status) => (
                  <option key={status} value={status}>
                    {t(statusLabels[status])}
                  </option>
                ))}
              </select>
            </FormField>
          </div>
          <div style={{ display: 'flex', gap: '8px', marginBlockStart: '20px' }}>
            <button type="button" className="button button--primary" onClick={submitPreview}>
              {t('organization.profile.savePreview')}
            </button>
            <button
              type="button"
              className="button button--quiet"
              onClick={() => {
                setForm({
                  name: selectedOrganization.name,
                  code: selectedOrganization.code,
                  planId: selectedOrganization.planId,
                  status: selectedOrganization.status,
                });
                setErrors(emptyErrors);
                setPreview(null);
              }}
            >
              {t('common.actions.reset')}
            </button>
          </div>
        </ChartCard>

        {preview ? (
          <ChartCard
            titleKey="organization.profile.previewTitle"
            subtitleKey="organization.profile.previewDescription"
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
                <span>{t('organization.profile.planLabel')}</span>
                <strong>{previewPlan?.name ?? preview.planId}</strong>
              </div>
              <div className="profile-detail">
                <span>{t('common.labels.status')}</span>
                <StatusBadge
                  tone={statusTones[preview.status]}
                  labelKey={statusLabels[preview.status]}
                />
              </div>
            </div>
          </ChartCard>
        ) : null}
      </>
    ),
  };

  const contextTab: TabItem = {
    id: 'context',
    labelKey: 'organization.profile.contextTab',
    content: (
      <ChartCard
        titleKey="organization.profile.contextTitle"
        subtitleKey="organization.profile.contextDescription"
      >
        <div style={summaryStyle}>
          <div className="profile-detail">
            <span>{t('organization.profile.selectedOrganizationLabel')}</span>
            <strong>{selectedOrganization.name}</strong>
          </div>
          <div className="profile-detail">
            <span>{t('organization.profile.selectedWarehouseLabel')}</span>
            <strong>{selectedWarehouse.name}</strong>
          </div>
          <div className="profile-detail">
            <span>{t('organization.profile.planNameLabel')}</span>
            <strong>{selectedPlan?.name ?? selectedOrganization.planId}</strong>
          </div>
          <div className="profile-detail">
            <span>{t('organization.profile.planPriceLabel')}</span>
            <strong dir="ltr">
              {selectedPlan ? formatCurrency(selectedPlan.price, selectedPlan.currency) : '—'}
            </strong>
          </div>
          <div className="profile-detail">
            <span>{t('organization.profile.userLimitLabel')}</span>
            <strong dir="ltr">{selectedPlan ? formatNumber(selectedPlan.userLimit) : '—'}</strong>
          </div>
          <div className="profile-detail">
            <span>{t('organization.profile.warehouseLimitLabel')}</span>
            <strong dir="ltr">
              {selectedPlan ? formatNumber(selectedPlan.warehouseLimit) : '—'}
            </strong>
          </div>
          <div className="profile-detail">
            <span>{t('organization.profile.createdLabel')}</span>
            <strong dir="ltr">{formatDate(selectedOrganization.createdAt)}</strong>
          </div>
          <div className="profile-detail">
            <span>{t('common.labels.status')}</span>
            <StatusBadge
              tone={statusTones[selectedOrganization.status]}
              labelKey={statusLabels[selectedOrganization.status]}
            />
          </div>
        </div>
      </ChartCard>
    ),
  };

  const tabs: readonly TabItem[] = [profileTab, contextTab];

  return (
    <>
      <PageHeader
        titleKey="organization.profile.title"
        descriptionKey="organization.profile.description"
        eyebrowKey="common.context.organization"
      />
      <DemoNotice compact />

      {planLoader.status === 'error' ? (
        <ErrorState onRetry={planLoader.reload} />
      ) : planLoader.status === 'loading' ? (
        <LoadingState />
      ) : (
        <Tabs items={tabs} defaultValue="profile" ariaLabelKey="organization.profile.title" />
      )}
    </>
  );
}

export default OrganizationProfilePage;
