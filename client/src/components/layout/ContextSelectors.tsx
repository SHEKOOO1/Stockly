import { useApp } from '@/context/AppContext';
import { t } from '@/i18n';

export function ContextSelectors() {
  const {
    organizations,
    availableWarehouses,
    selectedOrganizationId,
    selectedWarehouseId,
    selectOrganization,
    selectWarehouse,
  } = useApp();

  return (
    <div className="context-selectors">
      <label className="context-selectors__field">
        <span className="sr-only">{t('common.context.switchOrganization')}</span>
        <select
          value={selectedOrganizationId}
          onChange={(event) => selectOrganization(event.target.value)}
          aria-label={t('common.context.switchOrganization')}
        >
          {organizations.map((organization) => (
            <option key={organization.id} value={organization.id}>
              {organization.name}
            </option>
          ))}
        </select>
      </label>
      <label className="context-selectors__field">
        <span className="sr-only">{t('common.context.switchWarehouse')}</span>
        <select
          value={selectedWarehouseId}
          onChange={(event) => selectWarehouse(event.target.value)}
          aria-label={t('common.context.switchWarehouse')}
        >
          {availableWarehouses.map((warehouse) => (
            <option key={warehouse.id} value={warehouse.id}>
              {warehouse.name}
            </option>
          ))}
        </select>
      </label>
    </div>
  );
}
