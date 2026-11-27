import { TerminalGate } from '@/components/terminal/TerminalGate';
import { AppProvider } from '@/context/AppContext';
import { DemoSessionProvider, useDemoSession } from '@/context/DemoSessionContext';
import { AppShell } from '@/components/layout/AppShell';
import { DemoCategoryStoreProvider } from '@/data/demoCategoryStore';
import { DemoInventoryStoreProvider } from '@/data/demoInventoryStore';
import { DemoProductStoreProvider } from '@/data/demoProductStore';
import { DemoSupplierStoreProvider } from '@/data/demoSupplierStore';
import { DemoUnitStoreProvider } from '@/data/demoUnitStore';
import { AppRoutes } from '@/routes/AppRoutes';

function AuthenticatedApp() {
  const { session } = useDemoSession();

  if (!session) {
    return <TerminalGate />;
  }

  return (
    <DemoProductStoreProvider key={session.user.id}>
      <DemoCategoryStoreProvider>
        <DemoUnitStoreProvider>
          <DemoSupplierStoreProvider>
            <DemoInventoryStoreProvider>
              <AppShell>
                <AppRoutes />
              </AppShell>
            </DemoInventoryStoreProvider>
          </DemoSupplierStoreProvider>
        </DemoUnitStoreProvider>
      </DemoCategoryStoreProvider>
    </DemoProductStoreProvider>
  );
}

export interface AppProps {
  initialDemoSessionUserId?: string;
}

export function App({ initialDemoSessionUserId }: AppProps = {}) {
  return (
    <AppProvider>
      <DemoSessionProvider initialUserId={initialDemoSessionUserId}>
        <AuthenticatedApp />
      </DemoSessionProvider>
    </AppProvider>
  );
}

export default App;
