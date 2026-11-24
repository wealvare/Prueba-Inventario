import { AbstractControl, ValidationErrors } from '@angular/forms';

export function textoRequerido(control: AbstractControl<string>): ValidationErrors | null {
  return control.value.trim() === '' ? { textoRequerido: true } : null;
}

export function mayorQueCero({ value }: AbstractControl<number | null>): ValidationErrors | null {
  return value !== null && value <= 0 ? { mayorQueCero: true } : null;
}

export function dosDecimales({ value }: AbstractControl<number | null>): ValidationErrors | null {
  return value !== null && Number(value.toFixed(2)) !== value ? { dosDecimales: true } : null;
}

export function entero({ value }: AbstractControl<number | null>): ValidationErrors | null {
  return value !== null && !Number.isInteger(value) ? { entero: true } : null;
}
