import { useEffect, useMemo, useRef, useState } from 'react';
import { ArrowRight, Check, Search, ShieldCheck, Users, WifiOff } from 'lucide-react';
import { PIN_MAX_LENGTH, PIN_MIN_LENGTH, useDemoSession } from '@/context/DemoSessionContext';
import { demoOrganizations, demoWarehouses, demoUsers } from '@/data/demoData';
import { t } from '@/i18n';
import type { DemoUser } from '@/types/domain';

function useOnlineStatus() {
  const [online, setOnline] = useState(() => navigator.onLine);

  useEffect(() => {
    const update = () => setOnline(navigator.onLine);
    window.addEventListener('online', update);
    window.addEventListener('offline', update);
    return () => {
      window.removeEventListener('online', update);
      window.removeEventListener('offline', update);
    };
  }, []);

  return online;
}

function UserStage() {
  const { selectTerminalUser } = useDemoSession();
  const [query, setQuery] = useState('');
  const activeUsers = useMemo(() => demoUsers.filter((user) => user.status === 'active'), []);
  const filteredUsers = useMemo(() => {
    const normalized = query.trim().toLocaleLowerCase();
    if (!normalized) {
      return activeUsers;
    }
    return activeUsers.filter((user) =>
      [user.name, user.role, user.email].some((value) =>
        value.toLocaleLowerCase().includes(normalized),
      ),
    );
  }, [activeUsers, query]);

  return (
    <div className="terminal-stage">
      <div className="terminal-stage__heading">
        <span className="terminal-stage__icon" aria-hidden="true">
          <Users size={22} />
        </span>
        <div>
          <h2>{t('common.terminal.title')}</h2>
          <p>{t('common.terminal.subtitle')}</p>
        </div>
      </div>
      <label className="terminal-search">
        <span className="terminal-search__icon" aria-hidden="true">
          <Search size={18} />
        </span>
        <span className="sr-only">{t('common.terminal.search')}</span>
        <input
          type="search"
          value={query}
          placeholder={t('common.terminal.search')}
          onChange={(event) => setQuery(event.target.value)}
        />
      </label>
      <div className="terminal-stage__meta" role="status">
        <span>{t('common.terminal.availableUsers', { count: filteredUsers.length })}</span>
        <span className="terminal-pill">{t('common.terminal.activeOnly')}</span>
      </div>
      {filteredUsers.length === 0 ? (
        <div className="terminal-empty" role="status">
          {t('common.states.emptyDescription')}
        </div>
      ) : (
        <div className="terminal-user-grid">
          {filteredUsers.map((user) => {
            const organizationNames = user.organizationIds
              .map((id) => demoOrganizations.find((organization) => organization.id === id)?.name)
              .filter(Boolean)
              .join('، ');
            return (
              <button
                type="button"
                className="terminal-user-card"
                key={user.id}
                aria-label={`${t('common.terminal.chooseUser')} ${user.name}`}
                onClick={() => selectTerminalUser(user.id)}
              >
                <span className="terminal-user-card__avatar" dir="ltr" aria-hidden="true">
                  {user.name.slice(0, 1)}
                </span>
                <span className="terminal-user-card__body">
                  <strong>{user.name}</strong>
                  <span>{user.role}</span>
                  <small>{organizationNames}</small>
                </span>
                <span className="terminal-user-card__arrow" aria-hidden="true">
                  <ArrowRight size={18} />
                </span>
              </button>
            );
          })}
        </div>
      )}
    </div>
  );
}

function PinStage({ user }: { user: DemoUser }) {
  const { cancelUserSelection, completePinEntry } = useDemoSession();
  const [digitCount, setDigitCount] = useState(0);
  const [invalid, setInvalid] = useState(false);
  const [checking, setChecking] = useState(false);
  const timerRef = useRef<number | null>(null);

  useEffect(
    () => () => {
      if (timerRef.current !== null) {
        window.clearTimeout(timerRef.current);
      }
    },
    [],
  );

  function pressDigit() {
    if (checking) {
      return;
    }
    setDigitCount((current) => Math.min(PIN_MAX_LENGTH, current + 1));
    setInvalid(false);
  }

  function removeDigit() {
    if (checking) {
      return;
    }
    setDigitCount((current) => Math.max(0, current - 1));
    setInvalid(false);
  }

  function clearEntry() {
    if (checking) {
      return;
    }
    setDigitCount(0);
    setInvalid(false);
  }

  function goBack() {
    if (timerRef.current !== null) {
      window.clearTimeout(timerRef.current);
      timerRef.current = null;
    }
    setChecking(false);
    cancelUserSelection();
  }

  function submitEntry() {
    if (checking) {
      return;
    }
    if (digitCount < PIN_MIN_LENGTH) {
      setInvalid(true);
      return;
    }
    setInvalid(false);
    setChecking(true);
    timerRef.current = window.setTimeout(() => {
      const accepted = completePinEntry(digitCount);
      setChecking(false);
      timerRef.current = null;
      if (!accepted) {
        setInvalid(true);
      }
    }, 180);
  }

  const warehouseNames = user.warehouseIds
    .map((id) => demoWarehouses.find((warehouse) => warehouse.id === id)?.name)
    .filter(Boolean)
    .join('، ');

  return (
    <div className="terminal-stage">
      <div className="terminal-stage__heading">
        <span className="terminal-stage__icon" aria-hidden="true">
          <ShieldCheck size={22} />
        </span>
        <div>
          <h2>{t('common.terminal.pinTitle')}</h2>
          <p>
            {t('common.terminal.pinSubtitle', {
              min: PIN_MIN_LENGTH,
              max: PIN_MAX_LENGTH,
            })}
          </p>
        </div>
      </div>
      <div className="terminal-selected-user">
        <span className="terminal-selected-user__avatar" dir="ltr" aria-hidden="true">
          {user.name.slice(0, 1)}
        </span>
        <div>
          <strong>{user.name}</strong>
          <span>{user.role}</span>
          <small>{warehouseNames}</small>
        </div>
        <button type="button" className="terminal-text-button" onClick={goBack}>
          {t('common.terminal.changeUser')}
        </button>
      </div>
      <div className="pin-area">
        <div
          className="pin-mask"
          role="img"
          aria-label={t('common.terminal.pinFilled', { count: digitCount })}
          dir="ltr"
        >
          {Array.from({ length: PIN_MAX_LENGTH }, (_, index) => (
            <span key={index} className={index < digitCount ? 'is-filled' : ''} />
          ))}
        </div>
        <span className="sr-only">{t('common.terminal.pinField')}</span>
        {invalid ? (
          <p className="terminal-error" role="alert">
            {t('common.terminal.pinInvalid')}
          </p>
        ) : null}
        {checking ? (
          <p className="terminal-status" role="status">
            {t('common.terminal.pinChecking')}
          </p>
        ) : null}
        <div className="pin-keypad" aria-label={t('common.terminal.pinField')}>
          {[1, 2, 3, 4, 5, 6, 7, 8, 9].map((digit) => (
            <button
              type="button"
              className="pin-key"
              key={digit}
              aria-label={t('common.terminal.digit', { digit })}
              onClick={pressDigit}
            >
              {digit}
            </button>
          ))}
          <button type="button" className="pin-key pin-key--action" onClick={clearEntry}>
            {t('common.terminal.clear')}
          </button>
          <button
            type="button"
            className="pin-key"
            aria-label={t('common.terminal.digit', { digit: 0 })}
            onClick={pressDigit}
          >
            0
          </button>
          <button
            type="button"
            className="pin-key pin-key--action"
            aria-label={t('common.terminal.backspace')}
            onClick={removeDigit}
          >
            ⌫
          </button>
        </div>
        <button
          type="button"
          className="button button--primary terminal-submit"
          onClick={submitEntry}
          disabled={checking}
        >
          <Check size={18} aria-hidden="true" />
          {t('common.terminal.submit')}
        </button>
        <p className="terminal-hint">{t('common.terminal.pinNotice')}</p>
      </div>
    </div>
  );
}

export function TerminalGate() {
  const { selectedUser, notice } = useDemoSession();
  const online = useOnlineStatus();
  const noticeKey =
    notice === 'signedOut'
      ? 'common.terminal.sessionEnded'
      : notice === 'switched'
        ? 'common.terminal.switched'
        : null;

  return (
    <main className="terminal-gate" id="main-content">
      <div className="terminal-gate__background" aria-hidden="true" />
      <section className="terminal-gate__panel" aria-labelledby="terminal-brand-title">
        <header className="terminal-gate__header">
          <span className="terminal-gate__brand-mark" aria-hidden="true">
            S
          </span>
          <div>
            <h1 id="terminal-brand-title">{t('common.appName')}</h1>
            <span>{t('common.tagline')}</span>
          </div>
          <span className="terminal-pill">{t('common.demo.badge')}</span>
        </header>
        {noticeKey ? (
          <div className="terminal-notice" role="status">
            {t(noticeKey)}
          </div>
        ) : null}
        {!online ? (
          <div className="terminal-notice terminal-notice--offline" role="status">
            <WifiOff size={16} aria-hidden="true" />
            {t('common.terminal.offline')}
          </div>
        ) : null}
        {selectedUser ? <PinStage key={selectedUser.id} user={selectedUser} /> : <UserStage />}
        <footer className="terminal-gate__footer">{t('common.demo.notice')}</footer>
      </section>
    </main>
  );
}

export default TerminalGate;
