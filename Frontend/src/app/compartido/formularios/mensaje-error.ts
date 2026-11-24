import { AbstractControl } from '@angular/forms';

const MENSAJES: Record<string, (datos: never) => string> = {
  required: () => 'Este campo es obligatorio.',
  textoRequerido: () => 'Este campo es obligatorio.',
  maxlength: ({ requiredLength }: { requiredLength: number }) =>
    `No puede superar los ${requiredLength} caracteres.`,
  nombreUsuario: () =>
    'Usa al menos 3 caracteres: letras sin tilde, números, punto, guion o guion bajo.',
  correo: () => 'El correo no tiene un formato válido.',
  contrasena: () => 'Debe tener entre 8 y 100 caracteres, con mayúscula, minúscula y número.',
  min: ({ min }: { min: number }) => `Debe ser mayor o igual a ${min}.`,
  max: ({ max }: { max: number }) => `Debe ser menor o igual a ${max}.`,
  mayorQueCero: () => 'Debe ser mayor a cero.',
  dosDecimales: () => 'Admite máximo dos decimales.',
  entero: () => 'Debe ser un número entero.',
};

export function mensajeError(control: AbstractControl): string | null {
  if (!control.touched || !control.errors) {
    return null;
  }

  const [clave, datos] = Object.entries(control.errors)[0];
  return MENSAJES[clave]?.(datos as never) ?? 'El valor no es válido.';
}
