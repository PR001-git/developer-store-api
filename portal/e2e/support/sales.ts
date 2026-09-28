import type { CreateSaleRequest, SaleItemRequest } from '../../src/app/core/api/api-models';
import { BRANCHES, PRODUCTS, type ProductEntry } from '../../src/app/core/catalog/demo-catalog';

export interface Party {
  readonly id: string;
  readonly name: string;
}

/** A customer id no other test uses, so filtering by it isolates a test's sales in a shared database. */
export function uniqueCustomer(name = 'Cliente'): Party {
  return { id: crypto.randomUUID(), name: `${name} ${Math.random().toString(36).slice(2, 8)}` };
}

export function line(product: ProductEntry, quantity: number, unitPrice = product.unitPrice): SaleItemRequest {
  return { productId: product.id, productName: product.name, quantity, unitPrice };
}

export function saleFor(customer: Party, overrides: Partial<CreateSaleRequest> = {}): CreateSaleRequest {
  return {
    saleDate: '2026-09-24T11:30:00-03:00',
    customerId: customer.id,
    customerName: customer.name,
    branchId: BRANCHES[0].id,
    branchName: BRANCHES[0].name,
    items: [line(PRODUCTS[0], 5)],
    ...overrides,
  };
}
