import { fireEvent, render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import App from '@/App';

const fetchMock = vi.fn();

function renderApp(initialEntry: string, userId = 'user-1') {
  return render(
    <MemoryRouter initialEntries={[initialEntry]}>
      <App initialDemoSessionUserId={userId} />
    </MemoryRouter>,
  );
}

async function openQuickFor(
  user: ReturnType<typeof userEvent.setup>,
  product: RegExp,
  action: string,
) {
  const row = (await screen.findByText(product)).closest('tr');
  await user.click(within(row!).getByRole('button', { name: action }));
  return screen.findByRole('dialog');
}

describe('warehouse operations and quick quantity', () => {
  beforeEach(() => {
    fetchMock.mockClear();
    vi.stubGlobal('fetch', fetchMock);
    vi.mocked(window.matchMedia).mockImplementation((query: string) => ({
      matches: false,
      media: query,
      onchange: null,
      addListener: vi.fn(),
      removeListener: vi.fn(),
      addEventListener: vi.fn(),
      removeEventListener: vi.fn(),
      dispatchEvent: vi.fn(() => false),
    }));
  });

  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it('gives operators the daily screen and hides management tabs', async () => {
    renderApp('/', 'user-4');

    expect(await screen.findByRole('heading', { name: 'عمليات المخزن' })).toBeInTheDocument();
    const sidebar = document.getElementById('app-sidebar')!;
    expect(within(sidebar).getByRole('link', { name: 'العمليات اليومية' })).toBeInTheDocument();
    expect(within(sidebar).queryByRole('link', { name: 'المنتجات' })).not.toBeInTheDocument();
    expect(fetchMock).not.toHaveBeenCalled();
  });

  it('redirects operators away from manager-only routes', async () => {
    renderApp('/warehouse/products', 'user-4');

    expect(await screen.findByRole('heading', { name: 'عمليات المخزن' })).toBeInTheDocument();
    expect(screen.queryByRole('heading', { name: 'المنتجات' })).not.toBeInTheDocument();
    expect(fetchMock).not.toHaveBeenCalled();
  });

  it('keeps warehouse management tabs available to warehouse managers', async () => {
    renderApp('/warehouse/products', 'user-2');

    expect(await screen.findByRole('heading', { name: 'المنتجات' })).toBeInTheDocument();
    const sidebar = document.getElementById('app-sidebar')!;
    expect(within(sidebar).getByRole('link', { name: 'العمليات اليومية' })).toBeInTheDocument();
    expect(within(sidebar).getByRole('link', { name: 'المنتجات' })).toBeInTheDocument();
    expect(fetchMock).not.toHaveBeenCalled();
  });

  it('adds quantity and propagates the new shared balance', async () => {
    const user = userEvent.setup();
    renderApp('/warehouse/operations');

    const dialog = await openQuickFor(user, /أرز بسمتي/, 'إضافة كمية');
    expect(
      within(dialog).getByRole('heading', { name: 'إضافة كمية إلى أرز بسمتي فاخر' }),
    ).toBeInTheDocument();
    fireEvent.change(within(dialog).getByLabelText(/^الكمية/), { target: { value: '10' } });
    fireEvent.change(within(dialog).getByLabelText(/السبب/), {
      target: { value: 'توريد جديد' },
    });
    await user.click(within(dialog).getByRole('button', { name: 'تأكيد الإضافة' }));

    expect(
      await screen.findByText('تم تسجيل العملية وتحديث الرصيد في المعاينة المحلية فقط.'),
    ).toBeInTheDocument();

    await user.click(screen.getByRole('link', { name: 'نظرة المخزون' }));
    const stockRow = (await screen.findByText('أرز بسمتي')).closest('tr');
    expect(within(stockRow!).getByText('330')).toBeInTheDocument();
    expect(fetchMock).not.toHaveBeenCalled();
  });

  it('blocks issuing more than the available balance', async () => {
    const user = userEvent.setup();
    renderApp('/warehouse/operations');

    const dialog = await openQuickFor(user, /أرز بسمتي/, 'صرف كمية');
    fireEvent.change(within(dialog).getByLabelText(/^الكمية/), { target: { value: '9999' } });
    await user.click(within(dialog).getByRole('button', { name: 'تأكيد الصرف' }));

    expect(await screen.findByText('الكمية المتاحة غير كافية لإتمام العملية.')).toBeInTheDocument();
    expect(screen.getByRole('dialog')).toBeInTheDocument();
    expect(fetchMock).not.toHaveBeenCalled();
  });

  it('dedupes, resolves and reopens low-stock notifications', async () => {
    const user = userEvent.setup();
    renderApp('/warehouse/operations');

    const issueDialog = await openQuickFor(user, /أرز بسمتي/, 'صرف كمية');
    fireEvent.change(within(issueDialog).getByLabelText(/^الكمية/), { target: { value: '250' } });
    await user.click(within(issueDialog).getByRole('button', { name: 'تأكيد الصرف' }));

    await user.click(screen.getByRole('button', { name: 'الإشعارات' }));
    expect(await screen.findAllByText(/مخزون منخفض: أرز بسمتي/)).toHaveLength(1);
    await user.click(screen.getByRole('button', { name: 'الإشعارات' }));

    const secondIssue = await openQuickFor(user, /أرز بسمتي/, 'صرف كمية');
    await user.click(within(secondIssue).getByRole('button', { name: 'تأكيد الصرف' }));
    await user.click(screen.getByRole('button', { name: 'الإشعارات' }));
    expect(screen.getAllByText(/مخزون منخفض: أرز بسمتي/)).toHaveLength(1);
    await user.click(screen.getByRole('button', { name: 'الإشعارات' }));

    const addDialog = await openQuickFor(user, /أرز بسمتي/, 'إضافة كمية');
    fireEvent.change(within(addDialog).getByLabelText(/^الكمية/), { target: { value: '100' } });
    await user.click(within(addDialog).getByRole('button', { name: 'تأكيد الإضافة' }));
    await user.click(screen.getByRole('button', { name: 'الإشعارات' }));
    await waitFor(() => {
      expect(screen.queryByText(/مخزون منخفض: أرز بسمتي/)).not.toBeInTheDocument();
    });
    await user.click(screen.getByRole('button', { name: 'الإشعارات' }));

    const reopenIssue = await openQuickFor(user, /أرز بسمتي/, 'صرف كمية');
    fireEvent.change(within(reopenIssue).getByLabelText(/^الكمية/), { target: { value: '100' } });
    await user.click(within(reopenIssue).getByRole('button', { name: 'تأكيد الصرف' }));
    await user.click(screen.getByRole('button', { name: 'الإشعارات' }));
    expect(await screen.findByText(/مخزون منخفض: أرز بسمتي/)).toBeInTheDocument();
    expect(fetchMock).not.toHaveBeenCalled();
  });

  it('does not show role-targeted low-stock notifications to operators', async () => {
    const user = userEvent.setup();
    renderApp('/warehouse/operations', 'user-4');

    const dialog = await openQuickFor(user, /طقم مناشف ضيافة/, 'صرف كمية');
    fireEvent.change(within(dialog).getByLabelText(/^الكمية/), { target: { value: '60' } });
    await user.click(within(dialog).getByRole('button', { name: 'تأكيد الصرف' }));

    await user.click(screen.getByRole('button', { name: 'الإشعارات' }));
    expect(screen.queryByText(/مخزون منخفض: طقم مناشف ضيافة/)).not.toBeInTheDocument();
    expect(fetchMock).not.toHaveBeenCalled();
  });

  it('adds an item with an initial balance and minimum threshold', async () => {
    const user = userEvent.setup();
    renderApp('/warehouse/operations');

    await user.click(screen.getByRole('button', { name: 'إضافة صنف' }));
    const dialog = await screen.findByRole('dialog', { name: 'إضافة صنف للمعاينة' });
    fireEvent.change(within(dialog).getByLabelText(/اسم المنتج/), {
      target: { value: 'صنف اختبار سريع' },
    });
    fireEvent.change(within(dialog).getByLabelText(/رمز المنتج/), {
      target: { value: 'TEST-ITEM-900' },
    });
    fireEvent.change(within(dialog).getByLabelText(/وحدة القياس الأساسية/), {
      target: { value: 'unit-1' },
    });
    fireEvent.change(within(dialog).getByLabelText(/الحد الأدنى للكمية/), {
      target: { value: '5' },
    });
    fireEvent.change(within(dialog).getByLabelText(/الكمية الابتدائية/), {
      target: { value: '3' },
    });
    await user.click(within(dialog).getByRole('button', { name: 'حفظ المعاينة' }));

    expect(await screen.findByText('صنف اختبار سريع')).toBeInTheDocument();
    const row = screen.getByText('صنف اختبار سريع').closest('tr')!;
    expect(within(row).getByText('3')).toBeInTheDocument();
    expect(within(row).getByText('5')).toBeInTheDocument();
    expect(fetchMock).not.toHaveBeenCalled();
  });

  it('rejects duplicate item names and SKUs in the same warehouse', async () => {
    const user = userEvent.setup();
    renderApp('/warehouse/operations');

    await user.click(screen.getByRole('button', { name: 'إضافة صنف' }));
    const dialog = await screen.findByRole('dialog', { name: 'إضافة صنف للمعاينة' });
    fireEvent.change(within(dialog).getByLabelText(/اسم المنتج/), {
      target: { value: 'أرز بسمتي' },
    });
    fireEvent.change(within(dialog).getByLabelText(/رمز المنتج/), {
      target: { value: 'GRC-RICE-001' },
    });
    fireEvent.change(within(dialog).getByLabelText(/وحدة القياس الأساسية/), {
      target: { value: 'unit-1' },
    });
    await user.click(within(dialog).getByRole('button', { name: 'حفظ المعاينة' }));

    expect(
      await screen.findByText('اسم الصنف أو رمزه مستخدم بالفعل في هذا المخزن.'),
    ).toBeInTheDocument();
    expect(fetchMock).not.toHaveBeenCalled();
  });
});
