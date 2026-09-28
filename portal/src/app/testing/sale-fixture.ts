import { Sale, SaleItem } from '../core/api/api-models';

export function anItem(overrides: Partial<SaleItem> = {}): SaleItem {
  return {
    id: '7f9c2a44-6666-4d1e-8a3b-000000000011',
    productId: '00000000-0000-4000-8000-a00000000001',
    productName: 'Cerveja Pilsen 350ml',
    quantity: 5, unitPrice: 4.5, discountPercentage: 10, discountAmount: 2.25, totalAmount: 20.25,
    isCancelled: false,
    ...overrides,
  };
}

export function aSale(overrides: Partial<Sale> = {}): Sale {
  return {
    id: '7f9c2a44-5555-4d1e-8a3b-000000000010',
    saleNumber: 'S-000123',
    saleDate: '2026-09-24T14:30:00Z',
    customerId: '00000000-0000-4000-8000-c00000000001', customerName: 'Maria Silva',
    branchId: '00000000-0000-4000-8000-b00000000001', branchName: 'Filial Centro',
    totalAmount: 20.25, isCancelled: false,
    createdAt: '2026-09-24T14:31:02Z', updatedAt: null,
    items: [anItem()],
    ...overrides,
  };
}
