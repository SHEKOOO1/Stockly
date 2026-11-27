import { useEffect, useMemo } from 'react';
import { useApp } from '@/context/AppContext';
import { useDemoSession } from '@/context/DemoSessionContext';
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
  const { session } = useDemoSession();
  const visibleOrganizations = useMemo(
    () =>
      session ? organizations.filter((item) => session.user.organizationIds.includes(item.id)) : [],
    [organizations, session],
  );
  const visibleWarehouses = useMemo(
    () =>
      session
        ? availableWarehouses.filter((item) => session.user.warehouseIds.includes(item.id))
        : [],
    [availableWarehouses, session],
  );

  useEffect(() => {
    if (!session || visibleOrganizations.length === 0 || visibleWarehouses.length === 0) {
      return;
    }
    if (!visibleOrganizations.some((item) => item.id === selectedOrganizationId)) {
      selectOrganization(visibleOrganizations[0].id);
      return;
    }
    if (!visibleWarehouses.some((item) => item.id === selectedWarehouseId)) {
      selectWarehouse(visibleWarehouses[0].id);
    }
  }, [
    session,
    visibleOrganizations,
    visibleWarehouses,
    selectedOrganizationId,
    selectedWarehouseId,
    selectOrganization,
    selectWarehouse,
  ]);

  return (
    <div className="context-selectors">
      <label className="context-selectors__field">
        <span className="sr-only">{t('common.context.switchOrganization')}</span>
        <select
          value={selectedOrganizationId}
          onChange={(event) => selectOrganization(event.target.value)}
          aria-label={t('common.context.switchOrganization')}
        >
          {visibleOrganizations.map((organization) => (
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
          {visibleWarehouses.map((warehouse) => (
            <option key={warehouse.id} value={warehouse.id}>
              {warehouse.name}
            </option>
          ))}
        </select>
      </label>
    </div>
  );
}
