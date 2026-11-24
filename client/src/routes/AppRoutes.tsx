import { Navigate, Route, Routes } from 'react-router-dom';
import PlatformActivityPage from '@/pages/platform/ActivityPage';
import OrganizationAdministratorsPage from '@/pages/platform/OrganizationAdministratorsPage';
import OrganizationDetailsPage from '@/pages/platform/OrganizationDetailsPage';
import CreateOrganizationPage from '@/pages/platform/OrganizationNewPage';
import OrganizationsPage from '@/pages/platform/OrganizationsPage';
import PlatformDashboardPage from '@/pages/platform/DashboardPage';
import SubscriptionsPage from '@/pages/platform/SubscriptionsPage';
import PlatformSettingsPage from '@/pages/platform/SettingsPage';
import OrganizationActivityPage from '@/pages/organization/ActivityPage';
import OrganizationDashboardPage from '@/pages/organization/DashboardPage';
import OrganizationProfilePage from '@/pages/organization/ProfilePage';
import RolesPermissionsPage from '@/pages/organization/RolesPage';
import OrganizationSettingsPage from '@/pages/organization/SettingsPage';
import OrganizationUsersPage from '@/pages/organization/UsersPage';
import WarehousesPage from '@/pages/organization/WarehousesPage';
import CreateWarehousePage from '@/pages/organization/WarehouseNewPage';
import InventoryAdjustmentsPage from '@/pages/warehouse/AdjustmentsPage';
import LowStockAlertsPage from '@/pages/warehouse/AlertsPage';
import CategoriesPage from '@/pages/warehouse/CategoriesPage';
import WarehouseDashboardPage from '@/pages/warehouse/DashboardPage';
import ExpirationTrackingPage from '@/pages/warehouse/ExpirationPage';
import WarehouseEmployeesPage from '@/pages/warehouse/EmployeesPage';
import IncomingStockPage from '@/pages/warehouse/IncomingPage';
import StockMovementsPage from '@/pages/warehouse/MovementsPage';
import OutgoingStockPage from '@/pages/warehouse/OutgoingPage';
import OverviewPage from '@/pages/warehouse/OverviewPage';
import ProductDetailsPage from '@/pages/warehouse/ProductDetailsPage';
import { ProductsPage } from '@/pages/warehouse/ProductsPage';
import ReportsPage from '@/pages/warehouse/ReportsPage';
import StocktakingPage from '@/pages/warehouse/StocktakingPage';
import WarehouseSettingsPage from '@/pages/warehouse/SettingsPage';
import StockOverviewPage from '@/pages/warehouse/StockPage';
import SuppliersPage from '@/pages/warehouse/SuppliersPage';
import UnitsPage from '@/pages/warehouse/UnitsPage';

export function AppRoutes() {
  return (
    <Routes>
      <Route path="/" element={<Navigate to="/platform" replace />} />
      <Route path="/platform" element={<PlatformDashboardPage />} />
      <Route path="/platform/organizations" element={<OrganizationsPage />} />
      <Route path="/platform/organizations/new" element={<CreateOrganizationPage />} />
      <Route path="/platform/organizations/:id" element={<OrganizationDetailsPage />} />
      <Route
        path="/platform/organizations/:id/administrators"
        element={<OrganizationAdministratorsPage />}
      />
      <Route path="/platform/subscriptions" element={<SubscriptionsPage />} />
      <Route path="/platform/settings" element={<PlatformSettingsPage />} />
      <Route path="/platform/activity" element={<PlatformActivityPage />} />

      <Route path="/organization" element={<OrganizationDashboardPage />} />
      <Route path="/organization/profile" element={<OrganizationProfilePage />} />
      <Route path="/organization/users" element={<OrganizationUsersPage />} />
      <Route path="/organization/roles" element={<RolesPermissionsPage />} />
      <Route path="/organization/warehouses" element={<WarehousesPage />} />
      <Route path="/organization/warehouses/new" element={<CreateWarehousePage />} />
      <Route path="/organization/activity" element={<OrganizationActivityPage />} />
      <Route path="/organization/settings" element={<OrganizationSettingsPage />} />

      <Route path="/warehouse" element={<WarehouseDashboardPage />} />
      <Route path="/warehouse/overview" element={<OverviewPage />} />
      <Route path="/warehouse/products" element={<ProductsPage />} />
      <Route path="/warehouse/products/:id" element={<ProductDetailsPage />} />
      <Route path="/warehouse/categories" element={<CategoriesPage />} />
      <Route path="/warehouse/units" element={<UnitsPage />} />
      <Route path="/warehouse/suppliers" element={<SuppliersPage />} />
      <Route path="/warehouse/stock" element={<StockOverviewPage />} />
      <Route path="/warehouse/incoming" element={<IncomingStockPage />} />
      <Route path="/warehouse/outgoing" element={<OutgoingStockPage />} />
      <Route path="/warehouse/movements" element={<StockMovementsPage />} />
      <Route path="/warehouse/adjustments" element={<InventoryAdjustmentsPage />} />
      <Route path="/warehouse/stocktaking" element={<StocktakingPage />} />
      <Route path="/warehouse/alerts" element={<LowStockAlertsPage />} />
      <Route path="/warehouse/expiration" element={<ExpirationTrackingPage />} />
      <Route path="/warehouse/reports" element={<ReportsPage />} />
      <Route path="/warehouse/employees" element={<WarehouseEmployeesPage />} />
      <Route path="/warehouse/settings" element={<WarehouseSettingsPage />} />

      <Route path="*" element={<Navigate to="/platform" replace />} />
    </Routes>
  );
}

export default AppRoutes;
