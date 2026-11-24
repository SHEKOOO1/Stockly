import { AppShell } from '@/components/layout/AppShell';
import { AppProvider } from '@/context/AppContext';
import { DemoProductStoreProvider } from '@/data/demoProductStore';
import { AppRoutes } from '@/routes/AppRoutes';

export function App() {
  return (
    <AppProvider>
      <DemoProductStoreProvider>
        <AppShell>
          <AppRoutes />
        </AppShell>
      </DemoProductStoreProvider>
    </AppProvider>
  );
}

export default App;
