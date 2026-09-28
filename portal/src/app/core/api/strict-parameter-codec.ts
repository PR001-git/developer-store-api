import { HttpParameterCodec } from '@angular/common/http';

/**
 * Encodes query keys and values with encodeURIComponent. Angular's default codec leaves `+` as is,
 * and the server reads it as a space, which breaks dates with a positive offset such as `+05:30`.
 */
export class StrictParameterCodec implements HttpParameterCodec {
  encodeKey(key: string): string {
    return encodeURIComponent(key);
  }

  encodeValue(value: string): string {
    return encodeURIComponent(value);
  }

  decodeKey(key: string): string {
    return decodeURIComponent(key);
  }

  decodeValue(value: string): string {
    return decodeURIComponent(value);
  }
}
