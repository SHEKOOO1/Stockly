import { fireEvent, render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import App from '@/App';

const fetchMock = vi.fn();

function renderApp(initialEntry = '/warehouse/categories') {
  return render(
    <MemoryRouter initialEntries={[initialEntry]}>
      <App initialDemoSessionUserId="user-1" />
    </MemoryRouter>,
  );
}

describe('Categories page', () => {
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

  it('adds category with validation', async () => {
    const user = userEvent.setup();
    renderApp();
    await user.click(await screen.findByRole('button', { name: 'إضافة تصنيف' }));
    const dialog = await screen.findByRole('dialog', { name: 'إضافة تصنيف جديد' });
    await user.click(within(dialog).getByRole('button', { name: 'حفظ محليًا' }));
    expect(within(dialog).getAllByText('هذا الحقل مطلوب.').length).toBeGreaterThan(0);
    const inputs = within(dialog).getAllByRole('textbox');
    fireEvent.change(inputs[0], { target: { value: 'تصنيف جديد' } });
    fireEvent.change(inputs[1], { target: { value: 'NEWCAT' } });
    await user.click(within(dialog).getByRole('button', { name: 'حفظ محليًا' }));
    await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument());
    expect(await screen.findByText('تصنيف جديد')).toBeInTheDocument();
    expect(fetchMock).not.toHaveBeenCalled();
  });

  it('searches by code and filters archived categories', async () => {
    renderApp();
    const search = await screen.findByPlaceholderText('ابحث باسم التصنيف أو رمزه...');
    fireEvent.change(search, { target: { value: 'DRINK' } });
    expect(await screen.findByText('مشروبات')).toBeInTheDocument();
    expect(screen.queryByText('مواد غذائية')).not.toBeInTheDocument();

    fireEvent.change(screen.getByRole('combobox', { name: 'الحالة' }), {
      target: { value: 'archived' },
    });
    expect(screen.queryByText('مشروبات')).not.toBeInTheDocument();
  });

  it('prevents descendant as parent option', async () => {
    const user = userEvent.setup();
    renderApp();
    await user.click(await screen.findByRole('button', { name: 'إضافة تصنيف' }));
    let dialog = await screen.findByRole('dialog', { name: 'إضافة تصنيف جديد' });
    const inputs = within(dialog).getAllByRole('textbox');
    fireEvent.change(inputs[0], { target: { value: 'تصنيف فرعي' } });
    fireEvent.change(inputs[1], { target: { value: 'CHILD' } });
    await user.selectOptions(within(dialog).getByRole('combobox'), 'cat-1');
    await user.click(within(dialog).getByRole('button', { name: 'حفظ محليًا' }));
    await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument());
    const row = (await screen.findByText('مواد غذائية')).closest('tr');
    await user.click(within(row!).getAllByRole('button', { name: 'تعديل' })[0]);
    dialog = await screen.findByRole('dialog', { name: 'تعديل التصنيف' });
    const parent = within(dialog).getByRole('combobox') as HTMLSelectElement;
    expect(Array.from(parent.options).some((option) => option.value === 'cat-1')).toBe(false);
  });

  it('archives category and hides it from new product form', async () => {
    const user = userEvent.setup();
    renderApp();
    const row = (await screen.findByText('مشروبات')).closest('tr');
    await user.click(within(row!).getByRole('button', { name: 'أرشفة' }));
    const confirm = await screen.findByRole('dialog', { name: 'أرشفة التصنيف' });
    await user.click(within(confirm).getByRole('button', { name: 'أرشفة' }));
    await waitFor(() =>
      expect(screen.queryByRole('dialog', { name: 'أرشفة التصنيف' })).not.toBeInTheDocument(),
    );
    await user.click(screen.getByRole('link', { name: 'المنتجات' }));
    await user.click(await screen.findByRole('button', { name: 'إضافة منتج' }));
    const productDialog = await screen.findByRole('dialog', { name: 'إضافة منتج للمعاينة' });
    const categorySelect = within(productDialog).getAllByRole('combobox')[0];
    expect(
      Array.from(within(categorySelect).queryAllByRole('option')).some((option) =>
        option.textContent?.includes('مشروبات'),
      ),
    ).toBe(false);
    expect(fetchMock).not.toHaveBeenCalled();
  });

  it('restores an archived category back into the product form', async () => {
    const user = userEvent.setup();
    renderApp();
    const row = (await screen.findByText('مشروبات')).closest('tr');
    await user.click(within(row!).getByRole('button', { name: 'أرشفة' }));
    await user.click(
      within(await screen.findByRole('dialog', { name: 'أرشفة التصنيف' })).getByRole('button', {
        name: 'أرشفة',
      }),
    );

    const statusFilter = screen.getByRole('combobox', { name: 'الحالة' });
    await user.selectOptions(statusFilter, 'archived');
    const archivedRow = (await screen.findByText(/مشروبات/)).closest('tr');
    await user.click(within(archivedRow!).getByRole('button', { name: 'استعادة' }));
    await user.selectOptions(statusFilter, 'active');
    expect(await screen.findByText('مشروبات')).toBeInTheDocument();

    await user.click(screen.getByRole('link', { name: 'المنتجات' }));
    await user.click(await screen.findByRole('button', { name: 'إضافة منتج' }));
    const productDialog = await screen.findByRole('dialog', { name: 'إضافة منتج للمعاينة' });
    const categorySelect = within(productDialog).getAllByRole('combobox')[0];
    expect(
      Array.from(within(categorySelect).queryAllByRole('option')).some((option) =>
        option.textContent?.includes('مشروبات'),
      ),
    ).toBe(true);
  });

  it('reflects a category rename in product details', async () => {
    const user = userEvent.setup();
    renderApp();
    const row = (await screen.findByText('مواد غذائية')).closest('tr');
    await user.click(within(row!).getByRole('button', { name: 'تعديل' }));
    const dialog = await screen.findByRole('dialog', { name: 'تعديل التصنيف' });
    const inputs = within(dialog).getAllByRole('textbox');
    fireEvent.change(inputs[0], { target: { value: 'مواد غذائية محدثة' } });
    await user.click(within(dialog).getByRole('button', { name: 'حفظ محليًا' }));
    await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument());

    await user.click(screen.getByRole('link', { name: 'المنتجات' }));
    const productRow = (await screen.findByText('أرز بسمتي')).closest('tr');
    await user.click(within(productRow!).getByRole('button', { name: 'إجراءات الصف' }));
    await user.click(within(screen.getByRole('menu')).getByRole('menuitem', { name: 'عرض' }));

    expect(await screen.findByText('مواد غذائية محدثة')).toBeInTheDocument();
    expect(fetchMock).not.toHaveBeenCalled();
  });

  it('navigates from products to categories through the sidebar', async () => {
    const user = userEvent.setup();
    renderApp('/warehouse/products');
    await user.click(await screen.findByRole('link', { name: 'التصنيفات' }));
    expect(await screen.findByRole('heading', { name: 'التصنيفات' })).toBeInTheDocument();
    expect(fetchMock).not.toHaveBeenCalled();
  });
});
