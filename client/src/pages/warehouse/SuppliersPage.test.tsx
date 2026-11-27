import { fireEvent, render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import App from '@/App';

const fetchMock = vi.fn();

function renderApp(initialEntry = '/warehouse/suppliers') {
  return render(
    <MemoryRouter initialEntries={[initialEntry]}>
      <App initialDemoSessionUserId="user-1" />
    </MemoryRouter>,
  );
}

describe('Suppliers page', () => {
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

  it('adds a supplier with validation', async () => {
    const user = userEvent.setup();
    renderApp();

    await user.click(await screen.findByRole('button', { name: 'إضافة مورد' }));
    const dialog = await screen.findByRole('dialog', { name: 'إضافة مورد للمعاينة' });
    await user.click(within(dialog).getByRole('button', { name: 'حفظ محليًا' }));
    expect(within(dialog).getAllByText('هذا الحقل مطلوب.').length).toBeGreaterThan(0);

    const inputs = within(dialog).getAllByRole('textbox');
    fireEvent.change(inputs[0], { target: { value: 'مورد اختباري' } });
    fireEvent.change(inputs[1], { target: { value: 'TEST-SUP' } });
    fireEvent.change(inputs[2], { target: { value: 'مسؤول الاختبار' } });
    await user.click(within(dialog).getByRole('button', { name: 'حفظ محليًا' }));

    await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument());
    expect(await screen.findByText('مورد اختباري')).toBeInTheDocument();
    expect(fetchMock).not.toHaveBeenCalled();
  });

  it('rejects duplicate values and invalid contact formats', async () => {
    const user = userEvent.setup();
    renderApp();

    await user.click(await screen.findByRole('button', { name: 'إضافة مورد' }));
    const dialog = await screen.findByRole('dialog', { name: 'إضافة مورد للمعاينة' });
    const inputs = within(dialog).getAllByRole('textbox');
    fireEvent.change(inputs[0], { target: { value: 'شركة الواحة للتجارة' } });
    fireEvent.change(inputs[1], { target: { value: 'DUP-SUP' } });
    fireEvent.change(inputs[2], { target: { value: 'مسؤول' } });
    fireEvent.change(inputs[3], { target: { value: 'abc' } });
    fireEvent.change(inputs[4], { target: { value: 'not-an-email' } });
    await user.click(within(dialog).getByRole('button', { name: 'حفظ محليًا' }));

    expect(
      await within(dialog).findByText('اسم المورد أو رمزه مستخدم بالفعل.'),
    ).toBeInTheDocument();
    expect(within(dialog).getByText('أدخل رقم هاتف صالحاً.')).toBeInTheDocument();
    expect(within(dialog).getByText('أدخل بريداً إلكترونياً صالحاً.')).toBeInTheDocument();
  });

  it('searches by code and filters archived suppliers', async () => {
    const user = userEvent.setup();
    renderApp();

    const search = await screen.findByPlaceholderText(
      'ابحث باسم المورد أو رمزه أو بيانات الاتصال...',
    );
    fireEvent.change(search, { target: { value: 'SUP-GULF' } });
    expect(await screen.findByText('مؤسسة الخليج للإمداد')).toBeInTheDocument();

    const row = (await screen.findByText('مؤسسة الخليج للإمداد')).closest('tr');
    await user.click(within(row!).getByRole('button', { name: 'أرشفة' }));
    const confirm = await screen.findByRole('dialog', { name: 'أرشفة المورد' });
    await user.click(within(confirm).getByRole('button', { name: 'أرشفة' }));

    expect(await screen.findByText(/مؤسسة الخليج للإمداد \(مؤرشف\)/)).toBeInTheDocument();
    await user.selectOptions(
      screen.getByRole('combobox', { name: 'تصفية حسب الحالة' }),
      'archived',
    );
    expect(screen.queryByText('شركة الواحة للتجارة')).not.toBeInTheDocument();
  });

  it('keeps archived suppliers out of new products but retains existing references', async () => {
    const user = userEvent.setup();
    renderApp();

    const supplierRow = (await screen.findByText('شركة الواحة للتجارة')).closest('tr');
    await user.click(within(supplierRow!).getByRole('button', { name: 'أرشفة' }));
    await user.click(
      within(await screen.findByRole('dialog', { name: 'أرشفة المورد' })).getByRole('button', {
        name: 'أرشفة',
      }),
    );

    await user.click(screen.getByRole('link', { name: 'المنتجات' }));
    await user.click(await screen.findByRole('button', { name: 'إضافة منتج' }));
    const productDialog = await screen.findByRole('dialog', { name: 'إضافة منتج للمعاينة' });
    const supplierSelect = within(productDialog).getByLabelText(/المورد الافتراضي/);
    expect(
      Array.from(supplierSelect.querySelectorAll('option')).some((option) =>
        option.textContent?.includes('شركة الواحة للتجارة'),
      ),
    ).toBe(false);
    await user.click(within(productDialog).getByRole('button', { name: 'إلغاء' }));

    const productRow = (await screen.findByText('أرز بسمتي')).closest('tr');
    await user.click(within(productRow!).getByRole('button', { name: 'إجراءات الصف' }));
    await user.click(within(screen.getByRole('menu')).getByRole('menuitem', { name: 'تعديل' }));
    const editDialog = await screen.findByRole('dialog', { name: 'تعديل المنتج في المعاينة' });
    const editSupplierSelect = within(editDialog).getByLabelText(/المورد الافتراضي/);
    expect(
      Array.from(editSupplierSelect.querySelectorAll('option')).some((option) =>
        option.textContent?.includes('شركة الواحة للتجارة (SUP-WAHA) (مؤرشف)'),
      ),
    ).toBe(true);
    expect(fetchMock).not.toHaveBeenCalled();
  });

  it('propagates supplier edits to product lists and details', async () => {
    const user = userEvent.setup();
    renderApp();

    const row = (await screen.findByText('شركة الواحة للتجارة')).closest('tr');
    await user.click(within(row!).getByRole('button', { name: 'تعديل' }));
    const dialog = await screen.findByRole('dialog', { name: 'تعديل المورد في المعاينة' });
    const inputs = within(dialog).getAllByRole('textbox');
    fireEvent.change(inputs[0], { target: { value: 'شركة الواحة المحدثة' } });
    await user.click(within(dialog).getByRole('button', { name: 'حفظ محليًا' }));
    await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument());

    await user.click(screen.getByRole('link', { name: 'المنتجات' }));
    const productRow = (await screen.findByText('أرز بسمتي')).closest('tr');
    expect(within(productRow!).getByText('شركة الواحة المحدثة')).toBeInTheDocument();

    await user.click(within(productRow!).getByRole('button', { name: 'إجراءات الصف' }));
    await user.click(within(screen.getByRole('menu')).getByRole('menuitem', { name: 'عرض' }));
    await user.click(await screen.findByRole('tab', { name: 'الرصيد' }));
    expect(await screen.findByText('شركة الواحة المحدثة')).toBeInTheDocument();
    expect(fetchMock).not.toHaveBeenCalled();
  });
});
