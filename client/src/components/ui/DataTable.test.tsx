import { render, screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it } from 'vitest';
import { DataTable, type DataTableColumn } from '@/components/ui/DataTable';

interface Row {
  id: string;
  name: string;
  quantity: number;
}

const rows: Row[] = [
  'Juliet',
  'Hotel',
  'Golf',
  'Foxtrot',
  'Echo',
  'Delta',
  'Charlie',
  'Bravo',
  'Alpha',
].map((name, index) => ({ id: `row-${index}`, name, quantity: index + 1 }));

const columns: DataTableColumn<Row>[] = [
  {
    id: 'name',
    headerKey: 'common.labels.name',
    cell: (row) => row.name,
    sortValue: (row) => row.name,
  },
  {
    id: 'quantity',
    headerKey: 'common.labels.quantity',
    cell: (row) => row.quantity,
    sortValue: (row) => row.quantity,
  },
];

function tableRowNames(): string[] {
  return within(screen.getByRole('table'))
    .getAllByRole('row')
    .slice(1)
    .map((row) => within(row).getAllByRole('cell')[0]?.textContent ?? '');
}

describe('DataTable', () => {
  it('sorts rows and moves between pages', async () => {
    const user = userEvent.setup();
    render(<DataTable rows={rows} columns={columns} rowKey={(row) => row.id} pageSize={4} />);

    expect(tableRowNames()).toEqual(['Juliet', 'Hotel', 'Golf', 'Foxtrot']);

    const nameHeader = screen.getByRole('columnheader', { name: 'الاسم' });
    await user.click(within(nameHeader).getByRole('button', { name: 'ترتيب تصاعدي' }));

    expect(tableRowNames()).toEqual(['Alpha', 'Bravo', 'Charlie', 'Delta']);

    await user.click(screen.getByRole('button', { name: 'التالي' }));

    expect(tableRowNames()).toEqual(['Echo', 'Foxtrot', 'Golf', 'Hotel']);
    expect(screen.getByText('5–8')).toBeInTheDocument();
  });
});
