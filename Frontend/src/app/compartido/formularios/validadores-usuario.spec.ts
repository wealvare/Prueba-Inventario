import { FormControl, FormGroup, ValidatorFn } from '@angular/forms';
import {
  contrasenaSegura,
  contrasenasIguales,
  VALIDADORES_CORREO,
  VALIDADORES_NOMBRE_USUARIO,
} from './validadores-usuario';

describe('Validadores de usuario', () => {
  const esValido = (valor: string, validadores: ValidatorFn | ValidatorFn[]) =>
    new FormControl(valor, { nonNullable: true, validators: validadores }).valid;

  it.each([
    ['Clave123', true],
    ['Ñandú123', true],
    ['Clave12', false],
    ['clave123', false],
    ['CLAVE123', false],
    ['ClaveClave', false],
    ['Aa1' + 'x'.repeat(98), false],
  ])('la contraseña %s es válida: %s', (valor, valida) => {
    expect(esValido(valor, contrasenaSegura)).toBe(valida);
  });

  it.each([
    ['ana.perez_1-a', true],
    [' ana ', true],
    ['ab', false],
    ['ana perez', false],
    ['añá', false],
    ['a'.repeat(51), false],
    ['  ', false],
  ])('el nombre de usuario %s es válido: %s', (valor, valido) => {
    expect(esValido(valor, VALIDADORES_NOMBRE_USUARIO)).toBe(valido);
  });

  it.each([
    ['ana@ejemplo.com', true],
    ['ana@ejemplo', false],
    ['ana', false],
    ['', false],
  ])('el correo %s es válido: %s', (valor, valido) => {
    expect(esValido(valor, VALIDADORES_CORREO)).toBe(valido);
  });

  it('exige que las contraseñas coincidan', () => {
    const grupo = new FormGroup(
      { contrasena: new FormControl('Clave123'), confirmacion: new FormControl('Clave124') },
      contrasenasIguales,
    );
    expect(grupo.errors).toEqual({ contrasenasDistintas: true });

    grupo.controls.confirmacion.setValue('Clave123');
    expect(grupo.errors).toBeNull();
  });
});
