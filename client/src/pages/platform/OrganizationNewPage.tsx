import { ArrowLeft } from 'lucide-react';
import { useState } from 'react';
import { Link } from 'react-router-dom';
import { ChartCard, DemoNotice, FormField, PageHeader, StatusBadge } from '@/components/ui';
import { useApp } from '@/context/AppContext';
import { demoPlans } from '@/data/demoData';
import { t } from '@/i18n';
import { formatDate, formatNumber } from '@/lib/format';
import type { EntityStatus } from '@/types/domain';

const formGridStyle = {
  display: 'grid',
  gridTemplateColumns: 'repeat(auto-fit, minmax(240px, 1fr))',
  gap: '16px',
} as const;

const summaryStyle = {
  display: 'grid',
  gap: '8px',
} as const;

interface OrganizationForm {
  name: string;
  code: string;
  planId: string;
  warehouseCount: string;
  userCount: string;
  status: EntityStatus;
}

interface FormErrors {
  name: boolean;
  code: boolean;
  planId: boolean;
  warehouseCount: boolean;
  userCount: boolean;
}

const emptyErrors: FormErrors = {
  name: false,
  code: false,
  planId: false,
  warehouseCount: false,
  userCount: false,
};

const initialForm: OrganizationForm = {
  name: '',
  code: '',
  planId: demoPlans[0]?.id ?? '',
  warehouseCount: '1',
  userCount: '5',
  status: 'active',
};

export function CreateOrganizationPage() {
  const { showToast } = useApp();
  const [form, setForm] = useState<OrganizationForm>(initialForm);
  const [errors, setErrors] = useState<FormErrors>(emptyErrors);
  const [preview, setPreview] = useState<OrganizationForm | null>(null);

  function submitPreview() {
    const warehouseCount = Number(form.warehouseCount);
    const userCount = Number(form.userCount);
    const nextErrors: FormErrors = {
      name: form.name.trim().length < 3,
      code: !/^[A-Za-z0-9]{3,12}$/.test(form.code.trim()),
      planId: !demoPlans.some((plan) => plan.id === form.planId),
      warehouseCount: !Number.isInteger(warehouseCount) || warehouseCount <= 0,
      userCount: !Number.isInteger(userCount) || userCount <= 0,
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
    showToast('platform.organizationsNew.toastPreview', 'info');
  }

  function resetForm() {
    setForm(initialForm);
    setErrors(emptyErrors);
    setPreview(null);
  }

  const previewPlan = preview ? demoPlans.find((plan) => plan.id === preview.planId) : undefined;

  return (
    <>
      <PageHeader
        titleKey="platform.organizationsNew.title"
        descriptionKey="platform.organizationsNew.description"
        eyebrowKey="common.context.platform"
        actions={
          <Link className="button button--quiet button--sm" to="/platform/organizations">
            <ArrowLeft size={14} aria-hidden="true" />
            {t('platform.organizationDetails.backToOrganizations')}
          </Link>
        }
      />
      <DemoNotice compact />

      <div style={{ marginBlockEnd: '20px' }}>
        <ChartCard
          titleKey="platform.organizationsNew.formTitle"
          subtitleKey="platform.organizationsNew.formDescription"
        >
          <div style={formGridStyle}>
            <FormField
              labelKey="platform.organizationsNew.nameLabel"
              hintKey="platform.organizationsNew.nameHint"
              required
              htmlFor="organization-name"
              errorKey={errors.name ? 'platform.organizationsNew.errorName' : undefined}
            >
              <input
                id="organization-name"
                value={form.name}
                onChange={(event) => {
                  setForm((current) => ({ ...current, name: event.target.value }));
                  setErrors((current) => ({ ...current, name: false }));
                }}
              />
            </FormField>
            <FormField
              labelKey="platform.organizationsNew.codeLabel"
              hintKey="platform.organizationsNew.codeHint"
              required
              htmlFor="organization-code"
              errorKey={errors.code ? 'platform.organizationsNew.errorCode' : undefined}
            >
              <input
                id="organization-code"
                dir="ltr"
                value={form.code}
                onChange={(event) => {
                  setForm((current) => ({ ...current, code: event.target.value }));
                  setErrors((current) => ({ ...current, code: false }));
                }}
              />
            </FormField>
            <FormField
              labelKey="platform.organizationsNew.planLabel"
              hintKey="platform.organizationsNew.planHint"
              required
              htmlFor="organization-plan"
              errorKey={errors.planId ? 'platform.organizationsNew.errorPlan' : undefined}
            >
              <select
                id="organization-plan"
                value={form.planId}
                onChange={(event) => {
                  setForm((current) => ({ ...current, planId: event.target.value }));
                  setErrors((current) => ({ ...current, planId: false }));
                }}
              >
                {demoPlans.map((plan) => (
                  <option key={plan.id} value={plan.id}>
                    {plan.name}
                  </option>
                ))}
              </select>
            </FormField>
            <FormField
              labelKey="platform.organizationsNew.warehousesLabel"
              required
              htmlFor="organization-warehouse-count"
              errorKey={
                errors.warehouseCount ? 'platform.organizationsNew.errorWarehouses' : undefined
              }
            >
              <input
                id="organization-warehouse-count"
                type="number"
                min="1"
                dir="ltr"
                value={form.warehouseCount}
                onChange={(event) => {
                  setForm((current) => ({ ...current, warehouseCount: event.target.value }));
                  setErrors((current) => ({ ...current, warehouseCount: false }));
                }}
              />
            </FormField>
            <FormField
              labelKey="platform.organizationsNew.usersLabel"
              required
              htmlFor="organization-user-count"
              errorKey={errors.userCount ? 'platform.organizationsNew.errorUsers' : undefined}
            >
              <input
                id="organization-user-count"
                type="number"
                min="1"
                dir="ltr"
                value={form.userCount}
                onChange={(event) => {
                  setForm((current) => ({ ...current, userCount: event.target.value }));
                  setErrors((current) => ({ ...current, userCount: false }));
                }}
              />
            </FormField>
            <FormField
              labelKey="platform.organizationsNew.statusLabel"
              htmlFor="organization-status"
            >
              <select
                id="organization-status"
                value={form.status}
                onChange={(event) =>
                  setForm((current) => ({ ...current, status: event.target.value as EntityStatus }))
                }
              >
                <option value="active">{t('common.status.active')}</option>
                <option value="inactive">{t('common.status.inactive')}</option>
              </select>
            </FormField>
          </div>
          <div style={{ display: 'flex', gap: '8px', marginBlockStart: '20px' }}>
            <button type="button" className="button button--primary" onClick={submitPreview}>
              {t('platform.organizationsNew.submit')}
            </button>
            <button type="button" className="button button--quiet" onClick={resetForm}>
              {t('common.actions.reset')}
            </button>
          </div>
        </ChartCard>
      </div>

      {preview ? (
        <ChartCard
          titleKey="platform.organizationsNew.previewTitle"
          subtitleKey="platform.organizationsNew.previewDescription"
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
              <span>{t('platform.dashboard.planColumn')}</span>
              <strong>{previewPlan?.name ?? preview.planId}</strong>
            </div>
            <div className="profile-detail">
              <span>{t('common.labels.status')}</span>
              <StatusBadge
                tone={preview.status === 'active' ? 'healthy' : 'neutral'}
                labelKey={
                  preview.status === 'active' ? 'common.status.active' : 'common.status.inactive'
                }
              />
            </div>
            <div className="profile-detail">
              <span>{t('platform.organizationsNew.warehousesLabel')}</span>
              <strong dir="ltr">{formatNumber(Number(preview.warehouseCount))}</strong>
            </div>
            <div className="profile-detail">
              <span>{t('platform.organizationsNew.usersLabel')}</span>
              <strong dir="ltr">{formatNumber(Number(preview.userCount))}</strong>
            </div>
            <div className="profile-detail">
              <span>{t('common.labels.createdAt')}</span>
              <strong dir="ltr">{formatDate(new Date())}</strong>
            </div>
          </div>
        </ChartCard>
      ) : null}
    </>
  );
}

export default CreateOrganizationPage;
