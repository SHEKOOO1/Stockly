import { createContext, useCallback, useContext, useMemo, useState, type ReactNode } from 'react';
import { useApp } from '@/context/AppContext';
import { demoUsers } from '@/data/demoData';
import type { DemoUser } from '@/types/domain';

export const PIN_MIN_LENGTH = 6;
export const PIN_MAX_LENGTH = 12;

export type DemoSessionNotice = 'signedOut' | 'switched' | null;

export interface DemoSession {
  user: DemoUser;
  startedAt: string;
}

interface DemoSessionContextValue {
  session: DemoSession | null;
  selectedUser: DemoUser | null;
  notice: DemoSessionNotice;
  selectTerminalUser: (userId: string) => void;
  cancelUserSelection: () => void;
  completePinEntry: (digitCount: number) => boolean;
  switchUser: () => void;
  signOut: () => void;
}

const DemoSessionContext = createContext<DemoSessionContextValue | null>(null);

function findActiveUser(userId: string): DemoUser | undefined {
  return demoUsers.find((user) => user.id === userId && user.status === 'active');
}

export interface DemoSessionProviderProps {
  children: ReactNode;
  initialUserId?: string;
}

export function DemoSessionProvider({ children, initialUserId }: DemoSessionProviderProps) {
  const { resetForDemoSession } = useApp();
  const [session, setSession] = useState<DemoSession | null>(() => {
    const user = initialUserId ? findActiveUser(initialUserId) : undefined;
    return user ? { user, startedAt: new Date().toISOString() } : null;
  });
  const [selectedUser, setSelectedUser] = useState<DemoUser | null>(null);
  const [notice, setNotice] = useState<DemoSessionNotice>(null);

  const selectTerminalUser = useCallback(
    (userId: string) => {
      const user = findActiveUser(userId);
      if (!user) {
        return;
      }
      resetForDemoSession();
      setSelectedUser(user);
      setNotice(null);
    },
    [resetForDemoSession],
  );

  const cancelUserSelection = useCallback(() => {
    setSelectedUser(null);
    setNotice(null);
  }, []);

  const completePinEntry = useCallback(
    (digitCount: number) => {
      if (!selectedUser || digitCount < PIN_MIN_LENGTH || digitCount > PIN_MAX_LENGTH) {
        return false;
      }
      setSession({ user: selectedUser, startedAt: new Date().toISOString() });
      setSelectedUser(null);
      setNotice(null);
      return true;
    },
    [selectedUser],
  );

  const switchUser = useCallback(() => {
    resetForDemoSession();
    setSession(null);
    setSelectedUser(null);
    setNotice('switched');
  }, [resetForDemoSession]);

  const signOut = useCallback(() => {
    resetForDemoSession();
    setSession(null);
    setSelectedUser(null);
    setNotice('signedOut');
  }, [resetForDemoSession]);

  const value = useMemo<DemoSessionContextValue>(
    () => ({
      session,
      selectedUser,
      notice,
      selectTerminalUser,
      cancelUserSelection,
      completePinEntry,
      switchUser,
      signOut,
    }),
    [
      session,
      selectedUser,
      notice,
      selectTerminalUser,
      cancelUserSelection,
      completePinEntry,
      switchUser,
      signOut,
    ],
  );

  return <DemoSessionContext.Provider value={value}>{children}</DemoSessionContext.Provider>;
}

export function useDemoSession(): DemoSessionContextValue {
  const value = useContext(DemoSessionContext);
  if (!value) {
    throw new Error('useDemoSession must be used within DemoSessionProvider');
  }
  return value;
}
