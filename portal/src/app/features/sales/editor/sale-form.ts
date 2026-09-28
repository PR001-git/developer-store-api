import { FormArray, FormControl, FormGroup, NonNullableFormBuilder, ValidatorFn, Validators } from '@angular/forms';
import { CreateSaleRequest, Sale, SaleItemRequest, UpdateSaleRequest } from '../../../core/api/api-models';
import { FieldErrors } from '../../../core/api/api-error';
import { fromDateTimeLocal, toDateTimeLocal, toOffsetIso } from '../../../core/time/local-time';
import { MAX_QUANTITY } from '../pricing';

export const GUID_PATTERN = /^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$/;
const NAME_MAX_LENGTH = 100;
const SALE_NUMBER_MAX_LENGTH = 50;

export type SaleItemForm = FormGroup<{
  productId: FormControl<string>;
  productName: FormControl<string>;
  quantity: FormControl<number | null>;
  unitPrice: FormControl<number | null>;
}>;
export type PartyForm = FormGroup<{ id: FormControl<string>; name: FormControl<string> }>;
export type SaleForm = FormGroup<{
  saleNumber: FormControl<string>;
  saleDate: FormControl<string>;
  customer: PartyForm;
  branch: PartyForm;
  items: FormArray<SaleItemForm>;
}>;

const requiredTrimmed: ValidatorFn = control =>
  typeof control.value === 'string' && control.value.trim() === '' ? { required: true } : null;

const trimmedMaxLength = (max: number): ValidatorFn => control =>
  typeof control.value === 'string' && control.value.trim().length > max
    ? { maxlength: { requiredLength: max, actualLength: control.value.trim().length } }
    : null;

export const quantityValidator: ValidatorFn = control => {
  const value: unknown = control.value;
  if (value === null || value === '') return { required: true };
  if (typeof value !== 'number' || !Number.isInteger(value) || value < 1) return { quantityMin: true };
  if (value > MAX_QUANTITY) return { quantityMax: true };
  return null;
};

export const unitPriceValidator: ValidatorFn = control => {
  const value: unknown = control.value;
  if (value === null || value === '') return { required: true };
  if (typeof value !== 'number' || value <= 0) return { pricePositive: true };
  if (Math.abs(value * 100 - Math.round(value * 100)) > 1e-6) return { priceDecimals: true };
  return null;
};

/** R4: one line per product. Ids are compared case-insensitively, as Guids are. */
export const uniqueProductsValidator: ValidatorFn = control => {
  const seen = new Set<string>();
  const repeated = new Set<string>();
  for (const item of (control as FormArray<SaleItemForm>).controls) {
    const id = item.controls.productId.value.trim().toLowerCase();
    if (id === '') continue;
    (seen.has(id) ? repeated : seen).add(id);
  }
  return repeated.size > 0 ? { repeatedProduct: [...repeated] } : null;
};

const atLeastOneItem: ValidatorFn = control =>
  (control as FormArray).length === 0 ? { noItems: true } : null;

function createPartyForm(fb: NonNullableFormBuilder): PartyForm {
  return fb.group({
    id: fb.control('', [Validators.required, Validators.pattern(GUID_PATTERN)]),
    name: fb.control('', [requiredTrimmed, trimmedMaxLength(NAME_MAX_LENGTH)]),
  });
}

export function createItemForm(fb: NonNullableFormBuilder, item?: Partial<SaleItemRequest>): SaleItemForm {
  return fb.group({
    productId: fb.control(item?.productId ?? '', [Validators.required, Validators.pattern(GUID_PATTERN)]),
    productName: fb.control(item?.productName ?? '', [requiredTrimmed, trimmedMaxLength(NAME_MAX_LENGTH)]),
    quantity: fb.control<number | null>(item?.quantity ?? 1, quantityValidator),
    unitPrice: fb.control<number | null>(item?.unitPrice ?? null, unitPriceValidator),
  });
}

export function createSaleForm(fb: NonNullableFormBuilder, now: Date = new Date()): SaleForm {
  return fb.group({
    saleNumber: fb.control('', trimmedMaxLength(SALE_NUMBER_MAX_LENGTH)),
    saleDate: fb.control(toDateTimeLocal(now.toISOString()), Validators.required),
    customer: createPartyForm(fb),
    branch: createPartyForm(fb),
    items: fb.array<SaleItemForm>([createItemForm(fb)], [atLeastOneItem, uniqueProductsValidator]),
  });
}

export function addItem(fb: NonNullableFormBuilder, form: SaleForm, item?: Partial<SaleItemRequest>): void {
  form.controls.items.push(createItemForm(fb, item));
}

export function toUpdateRequest(form: SaleForm): UpdateSaleRequest {
  const value = form.getRawValue();
  const saleDate = fromDateTimeLocal(value.saleDate);
  if (!saleDate) {
    throw new Error('The sale date is not a valid date and time.');
  }
  return {
    saleDate: toOffsetIso(saleDate),
    customerId: value.customer.id,
    customerName: value.customer.name.trim(),
    branchId: value.branch.id,
    branchName: value.branch.name.trim(),
    items: value.items.map(item => ({
      productId: item.productId,
      productName: item.productName.trim(),
      quantity: item.quantity ?? 0,
      unitPrice: item.unitPrice ?? 0,
    })),
  };
}

/** A blank number is left out, so the API issues the next one (R12). */
export function toCreateRequest(form: SaleForm): CreateSaleRequest {
  const saleNumber = form.controls.saleNumber.value.trim();
  return saleNumber === '' ? toUpdateRequest(form) : { saleNumber, ...toUpdateRequest(form) };
}

/** Loads a sale for a PUT: its active lines only, since cancelled lines are history (R10). */
export function patchFromSale(fb: NonNullableFormBuilder, form: SaleForm, sale: Sale): void {
  form.controls.saleNumber.setValue(sale.saleNumber);
  form.controls.saleNumber.disable();
  form.controls.saleDate.setValue(toDateTimeLocal(sale.saleDate));
  form.controls.customer.setValue({ id: sale.customerId, name: sale.customerName });
  form.controls.branch.setValue({ id: sale.branchId, name: sale.branchName });
  form.controls.items.clear();
  for (const item of sale.items.filter(line => !line.isCancelled)) {
    addItem(fb, form, item);
  }
}

const PARTY_FIELDS: Readonly<Record<string, string>> = {
  customerId: 'customer.id', customerName: 'customer.name', branchId: 'branch.id', branchName: 'branch.name',
};

export function toControlPath(apiPath: string): string {
  return PARTY_FIELDS[apiPath] ?? apiPath.replace(/\[(\d+)\]/g, '.$1');
}

/** Shows each API validation message on its control; returns the messages no control owns. */
export function applyServerErrors(form: SaleForm, fieldErrors: FieldErrors): string[] {
  const unmatched: string[] = [];
  for (const [path, messages] of Object.entries(fieldErrors)) {
    const control = path === '' ? null : form.get(toControlPath(path));
    if (control) {
      control.setErrors({ server: messages.join(' ') });
      control.markAsTouched();
    } else {
      unmatched.push(...messages);
    }
  }
  return unmatched;
}
