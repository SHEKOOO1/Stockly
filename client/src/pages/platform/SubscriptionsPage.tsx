import { CreditCard, Users } from 'lucide-react';
import { useState } from 'react';
import { ChartCard, DemoNotice, Modal, PageHeader, StatCard, StatusBadge } from '@/components/ui';
import { useApp } from '@/context/AppContext';
import { demoPlans } from '@/data/demoData';
import { t } from '@/i18n';
import { formatCurrency, formatNumber } from '@/lib/format';

const cardGridStyle = {
  display: 'grid',
  gridTemplateColumns: 'repeat(auto-fit, minmax(250px, 1fr))',
  gap: '16px',
  marginBlockEnd: '20px',
} as const;

const statGridStyle = {
  display: 'grid',
  gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))',
  gap: '16px',
  marginBlockEnd: '20px',
} as const;

const summaryStyle = {
  display: 'grid',
  gap: '8px',
} as const;

const cardActionsStyle = {
  display: 'flex',
  justifyContent: 'flex-end',
  marginBlockStart: '16px',
} as const;

export function SubscriptionsPage() {
  const { selectedOrganization, showToast } = useApp();
  const [planOverrides, setPlanOverrides] = useState<Record<string, string>>({});
  const [pendingPlanId, setPendingPlanId] = useState<string | null>(null);

  const activePlanId = planOverrides[selectedOrganization.id] ?? selectedOrganization.planId;
  const activePlan = demoPlans.find((plan) => plan.id === activePlanId) ?? demoPlans[0];
  const pendingPlan = demoPlans.find((plan) => plan.id === pendingPlanId);

  function confirmChange() {
    if (!pendingPlan) {
      return;
    }
    setPlanOverrides((current) => ({ ...current, [selectedOrganization.id]: pendingPlan.id }));
    showToast('platform.subscriptions.changeToast', 'info', { plan: pendingPlan.name });
    setPendingPlanId(null);
  }

  return (
    <>
      <PageHeader
        titleKey="platform.subscriptions.title"
        descriptionKey="platform.subscriptions.description"
        eyebrowKey="common.context.platform"
      />
      <DemoNotice compact />

      <ChartCard
        titleKey="platform.subscriptions.currentPlanTitle"
        subtitleKey="platform.subscriptions.currentPlanDescription"
      >
        <div style={statGridStyle}>
          <StatCard
            labelKey="platform.subscriptions.currentOrganization"
            value={selectedOrganization.name}
            icon={Users}
            tone="neutral"
          />
          <StatCard
            labelKey="platform.subscriptions.activePlan"
            value={activePlan.name}
            icon={CreditCard}
            tone="info"
          />
          <StatCard
            labelKey="platform.subscriptions.monthlyPrice"
            value={formatCurrency(activePlan.price, activePlan.currency)}
            icon={CreditCard}
            tone="healthy"
          />
        </div>
        <div style={summaryStyle}>
          <div className="profile-detail">
            <span>{t('platform.subscriptions.userUsage')}</span>
            <strong dir="ltr">
              {formatNumber(selectedOrganization.userCount)} / {formatNumber(activePlan.userLimit)}
            </strong>
          </div>
          <div className="profile-detail">
            <span>{t('platform.subscriptions.warehouseUsage')}</span>
            <strong dir="ltr">
              {formatNumber(selectedOrganization.warehouseCount)} /{' '}
              {formatNumber(activePlan.warehouseLimit)}
            </strong>
          </div>
          <div className="profile-detail">
            <span>{t('common.labels.status')}</span>
            <StatusBadge tone="healthy" labelKey="common.status.active" />
          </div>
        </div>
      </ChartCard>

      <section className="chart-card" style={{ marginBlockEnd: '16px' }}>
        <div className="chart-card__header">
          <div>
            <h2 className="chart-card__title">{t('platform.subscriptions.plansTitle')}</h2>
            <p className="chart-card__subtitle">{t('platform.subscriptions.plansDescription')}</p>
          </div>
        </div>
      </section>

      <div style={cardGridStyle}>
        {demoPlans.map((plan) => {
          const isCurrent = plan.id === activePlan.id;
          return (
            <section className="chart-card" key={plan.id}>
              <div className="chart-card__header">
                <div>
                  <h2 className="chart-card__title">{plan.name}</h2>
                  <p className="chart-card__subtitle" dir="ltr">
                    {plan.code}
                  </p>
                </div>
                {isCurrent ? (
                  <StatusBadge tone="info" labelKey="platform.subscriptions.currentBadge" />
                ) : null}
              </div>
              <div className="chart-card__body">
                <div style={summaryStyle}>
                  <div className="profile-detail">
                    <span>{t('platform.subscriptions.monthlyPrice')}</span>
                    <strong dir="ltr">{formatCurrency(plan.price, plan.currency)}</strong>
                  </div>
                  <div className="profile-detail">
                    <span>{t('platform.subscriptions.userLimit')}</span>
                    <strong dir="ltr">{formatNumber(plan.userLimit)}</strong>
                  </div>
                  <div className="profile-detail">
                    <span>{t('platform.subscriptions.warehouseLimit')}</span>
                    <strong dir="ltr">{formatNumber(plan.warehouseLimit)}</strong>
                  </div>
                  <div className="profile-detail">
                    <span>{t('common.labels.status')}</span>
                    <StatusBadge
                      tone={plan.status === 'active' ? 'healthy' : 'neutral'}
                      labelKey={
                        plan.status === 'active' ? 'common.status.active' : 'common.status.inactive'
                      }
                    />
                  </div>
                </div>
                <div style={cardActionsStyle}>
                  <button
                    type="button"
                    className="button button--primary button--sm"
                    disabled={isCurrent}
                    onClick={() => setPendingPlanId(plan.id)}
                  >
                    {t('platform.subscriptions.changeAction')}
                  </button>
                </div>
              </div>
            </section>
          );
        })}
      </div>

      <Modal
        open={pendingPlan !== undefined}
        onClose={() => setPendingPlanId(null)}
        titleKey="platform.subscriptions.changeTitle"
        descriptionKey="common.form.localPreview"
        footer={
          <>
            <button
              type="button"
              className="button button--quiet"
              onClick={() => setPendingPlanId(null)}
            >
              {t('common.actions.cancel')}
            </button>
            <button type="button" className="button button--primary" onClick={confirmChange}>
              {t('platform.subscriptions.changeConfirm')}
            </button>
          </>
        }
      >
        <p className="confirm-dialog__message">
          {t('platform.subscriptions.changeBody', { plan: pendingPlan?.name ?? '' })}
        </p>
      </Modal>
    </>
  );
}

export default SubscriptionsPage;
