import { Pipe, PipeTransform } from '@angular/core';
import { formatDateTime, formatMoney } from './format';

@Pipe({ name: 'money' })
export class MoneyPipe implements PipeTransform {
  transform(value: number): string {
    return formatMoney(value);
  }
}

@Pipe({ name: 'dateTime' })
export class DateTimePipe implements PipeTransform {
  transform(value: string): string {
    return formatDateTime(value);
  }
}
