import { TestBed } from '@angular/core/testing';
import { NonNullableFormBuilder } from '@angular/forms';
import { beforeEach, describe, expect, it } from 'vitest';
import { anItem, aSale } from '../../../testing/sale-fixture';
import { addItem, applyServerErrors, createSaleForm, patchFromSale, SaleForm, toCreateRequest, toUpdateRequest } from './sale-form';

describe('sale form', () => {
  let fb: NonNullableFormBuilder;
  let form: SaleForm;

  const fill = (target: SaleForm) => {
    target.controls.saleDate.setValue('2026-09-24T11:30');
    target.controls.customer.setValue({ id: '00000000-0000-4000-8000-c00000000001', name: ' Maria Silva ' });
    target.controls.branch.setValue({ id: '00000000-0000-4000-8000-b00000000001', name: 'Filial Centro' });
    target.controls.items.at(0).setValue({
      productId: '00000000-0000-4000-8000-a00000000001', productName: 'Cerveja Pilsen 350ml', quantity: 5, unitPrice: 4.5,
    });
  };

  beforeEach(() => {
    fb = TestBed.inject(NonNullableFormBuilder);
    form = createSaleForm(fb);
  });

  it('starts with one empty line and is invalid until filled', () => {
    expect(form.controls.items.length).toBe(1);
    expect(form.valid).toBe(false);

    fill(form);
    expect(form.valid).toBe(true);
  });

  it('builds a create request with the local date and its offset, trimmed names and no blank number', () => {
    fill(form);

    expect(toCreateRequest(form)).toEqual({
      saleDate: '2026-09-24T11:30:00.000-03:00',
      customerId: '00000000-0000-4000-8000-c00000000001', customerName: 'Maria Silva',
      branchId: '00000000-0000-4000-8000-b00000000001', branchName: 'Filial Centro',
      items: [{ productId: '00000000-0000-4000-8000-a00000000001', productName: 'Cerveja Pilsen 350ml', quantity: 5, unitPrice: 4.5 }],
    });
  });

  it('sends a typed sale number, trimmed', () => {
    fill(form);
    form.controls.saleNumber.setValue('  BR-2026-0001 ');

    expect(toCreateRequest(form).saleNumber).toBe('BR-2026-0001');
  });

  it('rejects a repeated product, more than 20 items and a third decimal place', () => {
    fill(form);
    addItem(fb, form, { productId: '00000000-0000-4000-8000-A00000000001', productName: 'Again', quantity: 1, unitPrice: 1 });
    expect(form.controls.items.errors).toEqual({ repeatedProduct: ['00000000-0000-4000-8000-a00000000001'] });

    form.controls.items.at(0).controls.quantity.setValue(21);
    expect(form.controls.items.at(0).controls.quantity.errors).toEqual({ quantityMax: true });

    form.controls.items.at(0).controls.unitPrice.setValue(1.234);
    expect(form.controls.items.at(0).controls.unitPrice.errors).toEqual({ priceDecimals: true });
  });

  it('loads a sale for editing: active lines only, number read-only', () => {
    patchFromSale(fb, form, aSale({
      items: [anItem(), anItem({ id: 'x', productId: '00000000-0000-4000-8000-a00000000002', productName: 'Old', isCancelled: true })],
    }));

    expect(form.controls.saleNumber.disabled).toBe(true);
    expect(form.controls.saleDate.value).toBe('2026-09-24T11:30');
    expect(form.controls.items.length).toBe(1);
    expect(toUpdateRequest(form).items).toEqual([
      { productId: '00000000-0000-4000-8000-a00000000001', productName: 'Cerveja Pilsen 350ml', quantity: 5, unitPrice: 4.5 },
    ]);
  });

  it('puts server errors on their controls and returns the rest', () => {
    fill(form);
    const unmatched = applyServerErrors(form, {
      'items[0].quantity': ["It's not possible to sell above 20 identical items"],
      customerName: ['Too long'],
      '': ['A sale must have at least one item'],
    });

    expect(form.controls.items.at(0).controls.quantity.errors).toEqual({ server: "It's not possible to sell above 20 identical items" });
    expect(form.controls.customer.controls.name.errors).toEqual({ server: 'Too long' });
    expect(unmatched).toEqual(['A sale must have at least one item']);
  });
});
