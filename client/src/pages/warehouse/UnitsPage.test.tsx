import { fireEvent, render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import App from '@/App';

const fetchMock = vi.fn();

function renderApp(initialEntry = '/warehouse/units') {
  return render(
    <MemoryRouter initialEntries={[initialEntry]}>
      <App initialDemoSessionUserId="user-1" />
    </MemoryRouter>,
  );
}

describe('Units page', () => {
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

  it('adds a unit with validation', async () => {
    const user = userEvent.setup();
    renderApp();

    await user.click(await screen.findByRole('button', { name: 'إضافة وحدة' }));
    const dialog = await screen.findByRole('dialog', { name: 'إضافة وحدة للمعاينة' });
    await user.click(within(dialog).getByRole('button', { name: 'حفظ محليًا' }));
    expect(within(dialog).getAllByText('هذا الحقل مطلوب.').length).toBeGreaterThan(0);

    const inputs = within(dialog).getAllByRole('textbox');
    fireEvent.change(inputs[0], { target: { value: 'مليمتر مضاف' } });
    fireEvent.change(inputs[1], { target: { value: 'ADDEDMM' } });
    await user.click(within(dialog).getByRole('button', { name: 'حفظ محليًا' }));

    await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument());
    expect(await screen.findByText('مليمتر مضاف')).toBeInTheDocument();
    expect(fetchMock).not.toHaveBeenCalled();
  });

  it('rejects duplicate names and symbols', async () => {
    const user = userEvent.setup();
    renderApp();

    await user.click(await screen.findByRole('button', { name: 'إضافة وحدة' }));
    const dialog = await screen.findByRole('dialog', { name: 'إضافة وحدة للمعاينة' });
    const inputs = within(dialog).getAllByRole('textbox');
    fireEvent.change(inputs[0], { target: { value: ' قطعة ' } });
    fireEvent.change(inputs[1], { target: { value: 'DUP' } });
    await user.click(within(dialog).getByRole('button', { name: 'حفظ محليًا' }));

    expect(
      await within(dialog).findByText('اسم الوحدة أو رمزها مستخدم بالفعل.'),
    ).toBeInTheDocument();
  });

  it('searches by symbol and filters archived units', async () => {
    const user = userEvent.setup();
    renderApp();

    const search = await screen.findByPlaceholderText('ابحث باسم الوحدة أو رمزها...');
    fireEvent.change(search, { target: { value: 'ML' } });
    expect(await screen.findByText('ملليلتر')).toBeInTheDocument();

    const row = (await screen.findByText('ملليلتر')).closest('tr');
    await user.click(within(row!).getByRole('button', { name: 'أرشفة' }));
    const confirm = await screen.findByRole('dialog', { name: 'أرشفة الوحدة' });
    await user.click(within(confirm).getByRole('button', { name: 'أرشفة' }));

    expect(await screen.findByText(/ملليلتر \(مؤرشف\)/)).toBeInTheDocument();
    await user.selectOptions(screen.getByRole('combobox', { name: 'الحالة' }), 'archived');
    expect(screen.queryByText('كيلوغرام')).not.toBeInTheDocument();
  });

  it('blocks archiving a unit used by active products', async () => {
    const user = userEvent.setup();
    renderApp();

    const row = (await screen.findByText('كيلوغرام')).closest('tr');
    await user.click(within(row!).getByRole('button', { name: 'أرشفة' }));

    expect(await screen.findByRole('dialog', { name: 'تعذر أرشفة الوحدة' })).toBeInTheDocument();
    expect(await screen.findByText(/مستخدمة في \d+ منتج نشط/)).toBeInTheDocument();
    expect(within(row!).getByText('نشط')).toBeInTheDocument();
  });

  it('hides archived units from product forms until restored', async () => {
    const user = userEvent.setup();
    renderApp();

    const unitRow = (await screen.findByText('ملليلتر')).closest('tr');
    await user.click(within(unitRow!).getByRole('button', { name: 'أرشفة' }));
    await user.click(
      within(await screen.findByRole('dialog', { name: 'أرشفة الوحدة' })).getByRole('button', {
        name: 'أرشفة',
      }),
    );

    await user.click(screen.getByRole('link', { name: 'المنتجات' }));
    await user.click(await screen.findByRole('button', { name: 'إضافة منتج' }));
    const productDialog = await screen.findByRole('dialog', { name: 'إضافة منتج للمعاينة' });
    const baseUnit = within(productDialog).getByLabelText(/وحدة القياس الأساسية/);
    expect(
      Array.from(baseUnit.querySelectorAll('option')).some((option) =>
        option.textContent?.includes('ملليلتر'),
      ),
    ).toBe(false);

    await user.click(screen.getByRole('button', { name: 'إلغاء' }));
    await user.click(screen.getByRole('link', { name: 'وحدات القياس' }));
    await user.selectOptions(screen.getByRole('combobox', { name: 'الحالة' }), 'archived');
    const archivedRow = (await screen.findByText(/ملليلتر/)).closest('tr');
    await user.click(within(archivedRow!).getByRole('button', { name: 'استعادة' }));

    await user.click(screen.getByRole('link', { name: 'المنتجات' }));
    await user.click(await screen.findByRole('button', { name: 'إضافة منتج' }));
    const restoredDialog = await screen.findByRole('dialog', { name: 'إضافة منتج للمعاينة' });
    const restoredBaseUnit = within(restoredDialog).getByLabelText(/وحدة القياس الأساسية/);
    expect(
      Array.from(restoredBaseUnit.querySelectorAll('option')).some((option) =>
        option.textContent?.includes('ملليلتر (ML)'),
      ),
    ).toBe(true);
    expect(fetchMock).not.toHaveBeenCalled();
  });

  it('reflects a unit rename in product details', async () => {
    const user = userEvent.setup();
    renderApp();

    const row = (await screen.findByText('كيلوغرام')).closest('tr');
    await user.click(within(row!).getByRole('button', { name: 'تعديل' }));
    const dialog = await screen.findByRole('dialog', { name: 'تعديل الوحدة في المعاينة' });
    const inputs = within(dialog).getAllByRole('textbox');
    fireEvent.change(inputs[0], { target: { value: 'كيلوغرام محدث' } });
    await user.click(within(dialog).getByRole('button', { name: 'حفظ محليًا' }));
    await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument());

    await user.click(screen.getByRole('link', { name: 'المنتجات' }));
    const productRow = (await screen.findByText('أرز بسمتي')).closest('tr');
    await user.click(within(productRow!).getByRole('button', { name: 'إجراءات الصف' }));
    await user.click(within(screen.getByRole('menu')).getByRole('menuitem', { name: 'عرض' }));

    expect(await screen.findByText('كيلوغرام محدث (KG)')).toBeInTheDocument();
    expect(fetchMock).not.toHaveBeenCalled();
  });
});
