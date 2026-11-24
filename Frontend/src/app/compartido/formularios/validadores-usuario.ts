import { AbstractControl, ValidationErrors, ValidatorFn, Validators } from '@angular/forms';
import { textoRequerido } from './validadores';

export const VALIDADORES_NOMBRE_USUARIO: ValidatorFn[] = [
  textoRequerido,
  Validators.maxLength(50),
  ({ value }: AbstractControl<string>) =>
    /^[A-Za-z0-9._-]{3,}$/.test(value.trim()) ? null : { nombreUsuario: true },
];

export const VALIDADORES_CORREO: ValidatorFn[] = [
  textoRequerido,
  Validators.maxLength(150),
  ({ value }: AbstractControl<string>) =>
    /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(value.trim()) ? null : { correo: true },
];

export function contrasenaSegura({ value }: AbstractControl<string>): ValidationErrors | null {
  const cumple =
    value.length >= 8 &&
    value.length <= 100 &&
    /\p{Lu}/u.test(value) &&
    /\p{Ll}/u.test(value) &&
    /\p{Nd}/u.test(value);

  return value === '' || cumple ? null : { contrasena: true };
}

export function contrasenasIguales(control: AbstractControl): ValidationErrors | null {
  const { contrasena, confirmacion } = control.value;
  return contrasena === confirmacion ? null : { contrasenasDistintas: true };
}
