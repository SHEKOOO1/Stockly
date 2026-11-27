import { fireEvent, render, screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import App from '@/App';

const fetchMock = vi.fn();

function renderApp(initialEntry: string) {
  return render(
    <MemoryRouter initialEntries={[initialEntry]}>
      <App initialDemoSessionUserId="user-1" />
    </MemoryRouter>,
  );
}

async function submitWithConfirmation(user: ReturnType<typeof userEvent.setup>) {
  await user.click(await screen.findByRole('button', { name: 'تأكيد' }));
}

describe('inventory operations', () => {
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

  it('propagates a receipt into stock balances and product movement history', async () => {
    const user = userEvent.setup();
    renderApp('/warehouse/incoming');

    await user.click(await screen.findByRole('button', { name: 'حركة وارد جديدة' }));
    const formDialog = await screen.findByRole('dialog', {
      name: 'إضافة حركة وارد للمعاينة',
    });
    fireEvent.change(within(formDialog).getByLabelText(/الكمية/), { target: { value: '10' } });
    fireEvent.change(within(formDialog).getByLabelText(/الملاحظات/), {
      target: { value: 'اختبار استلام' },
    });
    await user.click(within(formDialog).getByRole('button', { name: 'حفظ المعاينة' }));
    const confirmDialog = await screen.findByRole('dialog', { name: 'تأكيد العملية' });
    await user.click(within(confirmDialog).getByRole('button', { name: 'تأكيد' }));

    expect(
      await screen.findByText('تم تسجيل العملية وتحديث الرصيد في المعاينة المحلية فقط.'),
    ).toBeInTheDocument();

    await user.click(screen.getByRole('link', { name: 'نظرة المخزون' }));
    const stockRow = (await screen.findByText('أرز بسمتي')).closest('tr');
    expect(within(stockRow!).getByText('330')).toBeInTheDocument();

    await user.click(screen.getByRole('link', { name: 'المنتجات' }));
    const productRow = (await screen.findByText('أرز بسمتي')).closest('tr');
    await user.click(within(productRow!).getByRole('button', { name: 'إجراءات الصف' }));
    await user.click(within(screen.getByRole('menu')).getByRole('menuitem', { name: 'عرض' }));
    await user.click(await screen.findByRole('tab', { name: 'الحركات' }));

    expect(await screen.findByText(/اختبار استلام/)).toBeInTheDocument();
    expect(fetchMock).not.toHaveBeenCalled();
  });

  it('blocks issuing more than the available shared balance', async () => {
    const user = userEvent.setup();
    renderApp('/warehouse/outgoing');

    await user.click(await screen.findByRole('button', { name: 'حركة صادر جديدة' }));
    const formDialog = await screen.findByRole('dialog', {
      name: 'إضافة حركة صادر للمعاينة',
    });
    await user.selectOptions(within(formDialog).getByLabelText(/المنتج/), 'prod-4');
    fireEvent.change(within(formDialog).getByLabelText(/الجهة المستلمة/), {
      target: { value: 'مستشفى' },
    });
    await user.click(within(formDialog).getByRole('button', { name: 'حفظ المعاينة' }));
    await submitWithConfirmation(user);

    expect(await screen.findByText('الكمية المتاحة غير كافية لإتمام العملية.')).toBeInTheDocument();
    expect(screen.getByRole('dialog', { name: 'إضافة حركة صادر للمعاينة' })).toBeInTheDocument();
    expect(fetchMock).not.toHaveBeenCalled();
  });

  it('hides archived products from new operation forms', async () => {
    const user = userEvent.setup();
    renderApp('/warehouse/products');

    const activeRow = (await screen.findByText('أرز بسمتي')).closest('tr');
    await user.click(within(activeRow!).getByRole('button', { name: 'إجراءات الصف' }));
    await user.click(within(screen.getByRole('menu')).getByRole('menuitem', { name: 'أرشفة' }));
    const archiveDialog = await screen.findByRole('dialog', { name: 'أرشفة المنتج' });
    await user.click(within(archiveDialog).getByRole('button', { name: 'أرشفة' }));

    await user.click(screen.getByRole('link', { name: 'الوارد' }));
    await user.click(await screen.findByRole('button', { name: 'حركة وارد جديدة' }));
    const formDialog = await screen.findByRole('dialog', {
      name: 'إضافة حركة وارد للمعاينة',
    });

    expect(within(formDialog).queryByRole('option', { name: 'أرز بسمتي' })).not.toBeInTheDocument();
    expect(fetchMock).not.toHaveBeenCalled();
  });

  it('records a stock count, creates a session and updates the shared balance', async () => {
    const user = userEvent.setup();
    renderApp('/warehouse/stocktaking');

    await user.click(await screen.findByRole('button', { name: 'جرد صنف جديد' }));
    const formDialog = await screen.findByRole('dialog', {
      name: 'تسجيل جرد صنف للمعاينة',
    });
    fireEvent.change(within(formDialog).getByLabelText(/الكمية المحصودة/), {
      target: { value: '318' },
    });
    await user.click(within(formDialog).getByRole('button', { name: 'حفظ المعاينة' }));
    await submitWithConfirmation(user);

    expect(
      await screen.findByText('تم تسجيل الجرد وتحديث الرصيد في المعاينة المحلية فقط.'),
    ).toBeInTheDocument();
    expect(await screen.findByText('CNT-LOCAL-001')).toBeInTheDocument();

    await user.click(screen.getByRole('link', { name: 'نظرة المخزون' }));
    const stockRow = (await screen.findByText('أرز بسمتي')).closest('tr');
    expect(within(stockRow!).getByText('318')).toBeInTheDocument();
    expect(fetchMock).not.toHaveBeenCalled();
  });
});
