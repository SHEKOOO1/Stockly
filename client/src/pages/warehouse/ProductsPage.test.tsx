import { fireEvent, render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import App from '@/App';

const fetchMock = vi.fn();

function renderApp(initialEntry = '/warehouse/products') {
  return render(
    <MemoryRouter initialEntries={[initialEntry]}>
      <App initialDemoSessionUserId="user-1" />
    </MemoryRouter>,
  );
}

describe('Products page', () => {
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

  it('filters products by search text', async () => {
    renderApp();

    const search = await screen.findByPlaceholderText(
      'ابحث بالاسم أو الاسم العربي أو رمز المنتج...',
    );
    fireEvent.change(search, { target: { value: 'أرز بسمتي' } });

    expect(await screen.findByText('أرز بسمتي')).toBeInTheDocument();
    expect(screen.queryByText('عدس أحمر')).not.toBeInTheDocument();
  });

  it('validates a new product before saving', async () => {
    const user = userEvent.setup();
    renderApp();

    await user.click(await screen.findByRole('button', { name: 'إضافة منتج' }));
    const dialog = await screen.findByRole('dialog', { name: 'إضافة منتج للمعاينة' });
    await user.click(within(dialog).getByRole('button', { name: 'حفظ المعاينة' }));

    expect(within(dialog).getByText(/أدخل اسماً من حرف واحد/)).toBeInTheDocument();
    expect(within(dialog).getByText(/أدخل رمز منتج/)).toBeInTheDocument();
    expect(within(dialog).getByText('وحدة القياس الأساسية مطلوبة.')).toBeInTheDocument();
    expect(screen.getByRole('dialog')).toBeInTheDocument();
  });

  it('accepts a one-character product name', async () => {
    const user = userEvent.setup();
    renderApp();

    await user.click(await screen.findByRole('button', { name: 'إضافة منتج' }));
    const dialog = await screen.findByRole('dialog', { name: 'إضافة منتج للمعاينة' });
    fireEvent.change(within(dialog).getByLabelText(/اسم المنتج/), { target: { value: 'ن' } });
    fireEvent.change(within(dialog).getByLabelText(/رمز المنتج/), {
      target: { value: 'ONE-SKU-1' },
    });
    await user.selectOptions(within(dialog).getByLabelText(/وحدة القياس الأساسية/), 'unit-1');
    await user.click(within(dialog).getByRole('button', { name: 'حفظ المعاينة' }));

    await waitFor(() => {
      expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
    });
    expect(await screen.findByText('ن')).toBeInTheDocument();
    expect(fetchMock).not.toHaveBeenCalled();
  });

  it('adds a product locally without exposing cost fields', async () => {
    const user = userEvent.setup();
    renderApp();

    await user.click(await screen.findByRole('button', { name: 'إضافة منتج' }));
    const dialog = await screen.findByRole('dialog', { name: 'إضافة منتج للمعاينة' });
    expect(within(dialog).queryByText(/التكلفة/)).not.toBeInTheDocument();

    fireEvent.change(within(dialog).getByLabelText(/اسم المنتج/), {
      target: { value: 'منتج اختباري' },
    });
    fireEvent.change(within(dialog).getByLabelText(/رمز المنتج/), {
      target: { value: 'TEST-SKU-900' },
    });
    await user.selectOptions(within(dialog).getByLabelText(/وحدة القياس الأساسية/), 'unit-1');
    await user.click(within(dialog).getByRole('button', { name: 'حفظ المعاينة' }));

    await waitFor(() => {
      expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
    });
    expect(await screen.findByText('منتج اختباري')).toBeInTheDocument();
    expect(fetchMock).not.toHaveBeenCalled();
  });

  it('scopes supplier options to the selected warehouse organization', async () => {
    const user = userEvent.setup();
    renderApp();

    await user.click(await screen.findByRole('button', { name: 'إضافة منتج' }));
    const dialog = await screen.findByRole('dialog', { name: 'إضافة منتج للمعاينة' });
    const supplier = within(dialog).getByLabelText(/المورد الافتراضي/);

    expect(
      within(supplier).getByRole('option', { name: 'شركة الواحة للتجارة (SUP-WAHA)' }),
    ).toBeInTheDocument();
    expect(
      within(supplier).queryByRole('option', { name: 'دار القرطاسية' }),
    ).not.toBeInTheDocument();
    expect(within(supplier).queryByRole('option', { name: 'ورشة الساحل' })).not.toBeInTheDocument();
  });

  it('archives, restores and blocks editing of archived products', async () => {
    const user = userEvent.setup();
    renderApp();

    const activeRow = (await screen.findByText('أرز بسمتي')).closest('tr');
    await user.click(within(activeRow!).getByRole('button', { name: 'إجراءات الصف' }));
    await user.click(within(screen.getByRole('menu')).getByRole('menuitem', { name: 'أرشفة' }));
    const archiveDialog = await screen.findByRole('dialog', { name: 'أرشفة المنتج' });
    await user.click(within(archiveDialog).getByRole('button', { name: 'أرشفة' }));

    const lifecycle = screen.getByLabelText('حالة دورة الحياة');
    await user.selectOptions(lifecycle, 'archived');
    const archivedRow = (await screen.findByText('أرز بسمتي')).closest('tr');
    await user.click(within(archivedRow!).getByRole('button', { name: 'إجراءات الصف' }));
    const archivedMenu = screen.getByRole('menu');
    expect(within(archivedMenu).getByRole('menuitem', { name: 'تعديل' })).toBeDisabled();
    await user.click(within(archivedMenu).getByRole('menuitem', { name: 'استعادة' }));
    const restoreDialog = await screen.findByRole('dialog', { name: 'استعادة المنتج' });
    await user.click(within(restoreDialog).getByRole('button', { name: 'استعادة' }));

    await waitFor(() => {
      expect(screen.queryByText('أرز بسمتي')).not.toBeInTheDocument();
    });
    await user.selectOptions(lifecycle, 'active');
    expect(await screen.findByText('أرز بسمتي')).toBeInTheDocument();
    expect(fetchMock).not.toHaveBeenCalled();
  });

  it('shows a locally added product on another warehouse page', async () => {
    const user = userEvent.setup();
    renderApp();

    await user.click(await screen.findByRole('button', { name: 'إضافة منتج' }));
    const dialog = await screen.findByRole('dialog', { name: 'إضافة منتج للمعاينة' });
    fireEvent.change(within(dialog).getByLabelText(/اسم المنتج/), {
      target: { value: 'منتج اختباري' },
    });
    fireEvent.change(within(dialog).getByLabelText(/رمز المنتج/), {
      target: { value: 'TEST-SKU-901' },
    });
    await user.selectOptions(within(dialog).getByLabelText(/وحدة القياس الأساسية/), 'unit-1');
    await user.click(within(dialog).getByRole('button', { name: 'حفظ المعاينة' }));
    await waitFor(() => {
      expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
    });

    await user.click(screen.getByRole('link', { name: 'نظرة المخزون' }));

    expect(await screen.findByText('منتج اختباري')).toBeInTheDocument();
    expect(fetchMock).not.toHaveBeenCalled();
  });

  it('reflects a details-page edit in the product details and store', async () => {
    const user = userEvent.setup();
    renderApp('/warehouse/products/prod-1');

    expect(await screen.findByRole('heading', { name: 'أرز بسمتي' })).toBeInTheDocument();
    await user.click(screen.getByRole('button', { name: 'تعديل' }));
    const dialog = await screen.findByRole('dialog', { name: 'تعديل المنتج في المعاينة' });
    fireEvent.change(within(dialog).getByLabelText(/اسم المنتج/), {
      target: { value: 'أرز بسمتي محدث' },
    });
    await user.click(within(dialog).getByRole('button', { name: 'حفظ المعاينة' }));

    expect(await screen.findByRole('heading', { name: 'أرز بسمتي محدث' })).toBeInTheDocument();
    expect(fetchMock).not.toHaveBeenCalled();
  });

  it('blocks editing an archived product from its details page and restores it', async () => {
    const user = userEvent.setup();
    renderApp();

    const activeRow = (await screen.findByText('أرز بسمتي')).closest('tr');
    await user.click(within(activeRow!).getByRole('button', { name: 'إجراءات الصف' }));
    await user.click(within(screen.getByRole('menu')).getByRole('menuitem', { name: 'أرشفة' }));
    const archiveDialog = await screen.findByRole('dialog', { name: 'أرشفة المنتج' });
    await user.click(within(archiveDialog).getByRole('button', { name: 'أرشفة' }));

    const lifecycle = screen.getByLabelText('حالة دورة الحياة');
    await user.selectOptions(lifecycle, 'archived');
    const archivedRow = (await screen.findByText('أرز بسمتي')).closest('tr');
    await user.click(within(archivedRow!).getByRole('button', { name: 'إجراءات الصف' }));
    await user.click(within(screen.getByRole('menu')).getByRole('menuitem', { name: 'عرض' }));

    expect(await screen.findByRole('heading', { name: 'أرز بسمتي' })).toBeInTheDocument();
    const editButton = screen.getByRole('button', { name: 'تعديل' });
    expect(editButton).toBeDisabled();
    await user.click(screen.getByRole('button', { name: 'استعادة' }));
    const restoreDialog = await screen.findByRole('dialog', { name: 'استعادة المنتج' });
    await user.click(within(restoreDialog).getByRole('button', { name: 'استعادة' }));

    await waitFor(() => {
      expect(screen.getByRole('button', { name: 'تعديل' })).not.toBeDisabled();
    });
    expect(fetchMock).not.toHaveBeenCalled();
  });
});
