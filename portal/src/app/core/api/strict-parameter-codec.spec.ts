import { HttpParams } from '@angular/common/http';
import { describe, expect, it } from 'vitest';
import { StrictParameterCodec } from './strict-parameter-codec';

describe('StrictParameterCodec', () => {
  it('encodes a plus sign, which the default codec leaves as a space for the server', () => {
    const params = new HttpParams({ encoder: new StrictParameterCodec() })
      .set('_minSaleDate', '2026-09-24T00:00:00.000+05:30');

    expect(params.toString()).toBe('_minSaleDate=2026-09-24T00%3A00%3A00.000%2B05%3A30');
  });

  it('keeps the * wildcard readable', () => {
    const params = new HttpParams({ encoder: new StrictParameterCodec() }).set('customerName', '*silva');

    expect(params.toString()).toBe('customerName=*silva');
  });
});
