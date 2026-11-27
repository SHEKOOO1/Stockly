import { fireEvent, render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import App from '@/App';

const fetchMock = vi.fn();

function renderApp(initialEntry: string) {
  return render(
    <MemoryRouter initialEntries={[initialEntry]}>
      <App />
    </MemoryRouter>,
  );
}

function setMobileNavigation(mobile: boolean) {
  vi.mocked(window.matchMedia).mockImplementation((query: string) => ({
    matches: mobile && query.includes('max-width: 960px'),
    media: query,
    onchange: null,
    addListener: vi.fn(),
    removeListener: vi.fn(),
    addEventListener: vi.fn(),
    removeEventListener: vi.fn(),
    dispatchEvent: vi.fn(() => false),
  }));
}

function getNavigationToggle() {
  const toggle = document.querySelector<HTMLButtonElement>('[data-nav-toggle]');
  if (!toggle) {
    throw new Error('Navigation toggle not found');
  }
  return toggle;
}

async function unlockDemoSession(userName = 'ريم الحربي') {
  const user = userEvent.setup();
  await user.click(screen.getByRole('button', { name: new RegExp(userName) }));
  for (let index = 0; index < 6; index += 1) {
    await user.click(screen.getByRole('button', { name: `الرقم ${index + 1}` }));
  }
  await user.click(screen.getByRole('button', { name: 'تأكيد الدخول' }));
  await screen.findByRole('button', { name: 'الملف الشخصي' });
}

describe('Stockly frontend shell', () => {
  beforeEach(() => {
    fetchMock.mockClear();
    vi.stubGlobal('fetch', fetchMock);
    setMobileNavigation(false);
  });

  afterEach(() => {
    vi.unstubAllGlobals();
    window.localStorage.clear();
    window.sessionStorage.clear();
  });

  it('redirects the root route to the platform dashboard', async () => {
    renderApp('/');
    await unlockDemoSession();

    expect(await screen.findByRole('heading', { name: 'لوحة المنصة' })).toBeInTheDocument();
    expect(screen.getByText('وضع العرض')).toBeInTheDocument();
    expect(fetchMock).not.toHaveBeenCalled();
  });

  it('renders a deep warehouse route with local demo content', async () => {
    renderApp('/warehouse/stocktaking');
    await unlockDemoSession();

    expect(await screen.findByRole('heading', { name: 'الجرد' })).toBeInTheDocument();
    expect(screen.getByText(/جلسات الجرد والكميات المتوقعة/)).toBeInTheDocument();
    expect(screen.getAllByText('معاينة محلية فقط')).toHaveLength(2);
  });

  it('redirects unknown routes to the platform dashboard', async () => {
    renderApp('/route-that-does-not-exist');
    await unlockDemoSession();

    expect(await screen.findByRole('heading', { name: 'لوحة المنصة' })).toBeInTheDocument();
  });

  it('opens the command palette and navigates locally', async () => {
    const user = userEvent.setup();
    renderApp('/warehouse/products');
    await unlockDemoSession();

    await user.click(screen.getByRole('button', { name: 'فتح البحث العام' }));
    const dialog = await screen.findByRole('dialog', { name: 'البحث العام' });

    await user.click(within(dialog).getByRole('button', { name: /لوحة المنصة/ }));

    expect(await screen.findByRole('heading', { name: 'لوحة المنصة' })).toBeInTheDocument();
    expect(fetchMock).not.toHaveBeenCalled();
  });

  it('closes the command palette with Escape', async () => {
    const user = userEvent.setup();
    renderApp('/platform');
    await unlockDemoSession();

    await user.click(screen.getByRole('button', { name: 'فتح البحث العام' }));
    expect(await screen.findByRole('dialog', { name: 'البحث العام' })).toBeInTheDocument();

    await user.keyboard('{Escape}');

    await waitFor(() => {
      expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
    });
  });

  it('opens and closes the mobile navigation drawer', async () => {
    setMobileNavigation(true);
    const user = userEvent.setup();
    renderApp('/platform');
    await unlockDemoSession();

    const toggle = getNavigationToggle();
    const sidebar = document.getElementById('app-sidebar');
    expect(toggle).toHaveAttribute('aria-label', 'فتح القائمة');
    expect(toggle).toHaveAttribute('aria-expanded', 'false');
    expect(sidebar).toHaveAttribute('hidden');

    await user.click(toggle);

    expect(toggle).toHaveAttribute('aria-label', 'إغلاق القائمة من زر التنقل');
    expect(toggle).toHaveAttribute('aria-expanded', 'true');
    expect(sidebar).toHaveClass('is-open');
    expect(sidebar).not.toHaveAttribute('hidden');
    expect(document.querySelector('[data-sidebar-overlay]')).toHaveClass('is-visible');

    await user.click(toggle);
    await waitFor(() => {
      expect(sidebar).toHaveAttribute('hidden');
    });
    expect(document.querySelector('[data-sidebar-overlay]')).not.toBeInTheDocument();
  });

  it('closes the mobile drawer from the sidebar close button', async () => {
    setMobileNavigation(true);
    const user = userEvent.setup();
    renderApp('/platform');
    await unlockDemoSession();

    await user.click(getNavigationToggle());
    const sidebar = document.getElementById('app-sidebar');
    await user.click(within(sidebar!).getByRole('button', { name: 'إغلاق القائمة من اللوحة' }));

    await waitFor(() => {
      expect(sidebar).toHaveAttribute('hidden');
    });
  });

  it('closes the mobile drawer with Escape', async () => {
    setMobileNavigation(true);
    const user = userEvent.setup();
    renderApp('/platform');
    await unlockDemoSession();

    await user.click(getNavigationToggle());
    await user.keyboard('{Escape}');

    await waitFor(() => {
      expect(document.getElementById('app-sidebar')).toHaveAttribute('hidden');
    });
  });

  it('closes the mobile drawer from the overlay', async () => {
    setMobileNavigation(true);
    const user = userEvent.setup();
    renderApp('/platform');
    await unlockDemoSession();

    await user.click(getNavigationToggle());
    const overlay = document.querySelector('[data-sidebar-overlay]');
    expect(overlay).toHaveAttribute('aria-label', 'إغلاق القائمة بالنقر خارجها');
    fireEvent.mouseDown(overlay!);

    await waitFor(() => {
      expect(document.getElementById('app-sidebar')).toHaveAttribute('hidden');
    });
  });

  it('closes the mobile drawer after following a navigation link', async () => {
    setMobileNavigation(true);
    const user = userEvent.setup();
    renderApp('/platform');
    await unlockDemoSession();

    await user.click(getNavigationToggle());
    const sidebar = document.getElementById('app-sidebar');
    await user.click(within(sidebar!).getByRole('link', { name: 'المخزن' }));

    await waitFor(() => {
      expect(sidebar).toHaveAttribute('hidden');
    });
    expect(await screen.findByRole('heading', { name: 'لوحة المخزن' })).toBeInTheDocument();
    expect(fetchMock).not.toHaveBeenCalled();
  });

  it('collapses and expands the desktop navigation rail', async () => {
    const user = userEvent.setup();
    renderApp('/platform');
    await unlockDemoSession();

    const collapseButton = screen.getByRole('button', { name: 'طي القائمة' });
    await user.click(collapseButton);

    expect(document.getElementById('app-sidebar')).toHaveClass('is-collapsed');
    expect(document.querySelector('.app-shell')).toHaveClass('is-sidebar-collapsed');

    await user.click(screen.getByRole('button', { name: 'توسيع القائمة' }));
    await waitFor(() => {
      expect(document.getElementById('app-sidebar')).not.toHaveClass('is-collapsed');
    });
  });
});

describe('shared terminal demo gate', () => {
  beforeEach(() => {
    fetchMock.mockClear();
    vi.stubGlobal('fetch', fetchMock);
    setMobileNavigation(false);
  });

  afterEach(() => {
    vi.unstubAllGlobals();
    window.localStorage.clear();
    window.sessionStorage.clear();
  });

  it('blocks app routes until a demo PIN session is started', async () => {
    renderApp('/warehouse/products');

    expect(await screen.findByRole('heading', { name: 'تحديد المستخدم' })).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'الملف الشخصي' })).not.toBeInTheDocument();
    expect(screen.queryByRole('heading', { name: 'المنتجات' })).not.toBeInTheDocument();
    expect(fetchMock).not.toHaveBeenCalled();
  });

  it('rejects a PIN with too few digits without starting a session', async () => {
    const user = userEvent.setup();
    renderApp('/platform');

    await user.click(screen.getByRole('button', { name: /ريم الحربي/ }));
    for (let index = 0; index < 5; index += 1) {
      await user.click(screen.getByRole('button', { name: `الرقم ${index + 1}` }));
    }
    await user.click(screen.getByRole('button', { name: 'تأكيد الدخول' }));

    expect(await screen.findByRole('alert')).toHaveTextContent('أدخل ما بين 6 و12 رقماً للمتابعة.');
    expect(screen.queryByRole('button', { name: 'الملف الشخصي' })).not.toBeInTheDocument();
  });

  it('starts a demo session without storing or displaying the entered PIN', async () => {
    renderApp('/platform');
    await unlockDemoSession();

    expect(await screen.findByRole('heading', { name: 'لوحة المنصة' })).toBeInTheDocument();
    expect(screen.getByText('ريم الحربي')).toBeInTheDocument();
    expect(document.body).not.toHaveTextContent('123456');
    expect(window.localStorage.length).toBe(0);
    expect(window.sessionStorage.length).toBe(0);
  });

  it('does not offer self-registration on the terminal', async () => {
    renderApp('/platform');

    expect(await screen.findByRole('heading', { name: 'تحديد المستخدم' })).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /إنشاء حساب|تسجيل جديد|مستخدم جديد/ })).toBeNull();
  });

  it('clears the visible session when signing out', async () => {
    const user = userEvent.setup();
    renderApp('/platform');
    await unlockDemoSession();

    await user.click(screen.getByRole('button', { name: 'الملف الشخصي' }));
    await user.click(screen.getByRole('button', { name: 'إنهاء الجلسة' }));

    expect(await screen.findByRole('heading', { name: 'تحديد المستخدم' })).toBeInTheDocument();
    expect(screen.getByText(/انتهت جلسة العرض/)).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'الملف الشخصي' })).not.toBeInTheDocument();
  });

  it('clears the previous user context when switching users', async () => {
    const user = userEvent.setup();
    renderApp('/platform');
    await unlockDemoSession();
    await user.click(screen.getByRole('button', { name: 'الملف الشخصي' }));
    await user.click(screen.getByRole('button', { name: 'تبديل المستخدم' }));

    expect(await screen.findByRole('heading', { name: 'تحديد المستخدم' })).toBeInTheDocument();
    expect(screen.getByText(/تم تبديل المستخدم/)).toBeInTheDocument();
    await unlockDemoSession('خالد العتيبي');
    expect(screen.getByText('خالد العتيبي')).toBeInTheDocument();
    expect(screen.queryByText('ريم الحربي')).not.toBeInTheDocument();
  });
});
