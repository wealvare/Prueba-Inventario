import { PRODUCTO } from '../../../pruebas/productos-prueba';
import { cargarProducto, crearFormulario, crearLote, leerSolicitud } from './formulario-producto';

describe('Formulario de producto', () => {
  const valido = () => {
    const formulario = crearFormulario();
    cargarProducto(formulario, PRODUCTO);
    return formulario;
  };

  const loteValido = () => {
    const lote = crearLote();
    lote.patchValue({ numeroLote: 'L-9', precio: 1, cantidad: 1 });
    return lote;
  };

  it('es válido con los datos de un producto', () => {
    expect(valido().valid).toBe(true);
  });

  it('admite un producto sin lotes', () => {
    const formulario = valido();
    formulario.controls.lotes.clear();

    expect(formulario.valid).toBe(true);
  });

  it.each(['codigo', 'nombre', 'categoria'] as const)('exige %s', (campo) => {
    const formulario = valido();

    formulario.controls[campo].setValue('   ');

    expect(formulario.controls[campo].hasError('textoRequerido')).toBe(true);
  });

  it.each([
    ['codigo', 30],
    ['nombre', 120],
    ['categoria', 80],
    ['descripcion', 500],
  ] as const)('limita la longitud de %s a %i', (campo, longitud) => {
    const control = valido().controls[campo];

    control.setValue('a'.repeat(longitud));
    expect(control.valid).toBe(true);
    control.setValue('a'.repeat(longitud + 1));
    expect(control.hasError('maxlength')).toBe(true);
  });

  it.each([
    [null, 'required'],
    [0, 'mayorQueCero'],
    [-1, 'mayorQueCero'],
    [1.234, 'dosDecimales'],
    [1e14, 'max'],
  ])('rechaza el precio %s', (precio, error) => {
    const { precio: control } = loteValido().controls;

    control.setValue(precio);

    expect(control.hasError(error)).toBe(true);
  });

  it.each([0.01, 10, 1.1, 159900.55, 9_999_999_999_999.99])('acepta el precio %s', (precio) => {
    const lote = loteValido();

    lote.controls.precio.setValue(precio);

    expect(lote.valid).toBe(true);
  });

  it.each([
    [null, 'required'],
    [-1, 'min'],
    [1.5, 'entero'],
    [2_147_483_648, 'max'],
  ])('rechaza la cantidad %s', (cantidad, error) => {
    const { cantidad: control } = loteValido().controls;

    control.setValue(cantidad);

    expect(control.hasError(error)).toBe(true);
  });

  it('acepta cantidad cero', () => {
    const lote = loteValido();

    lote.controls.cantidad.setValue(0);

    expect(lote.valid).toBe(true);
  });

  it('exige número de lote y fecha de ingreso', () => {
    const lote = loteValido();

    lote.patchValue({ numeroLote: ' ', fechaIngreso: '' });

    expect(lote.controls.numeroLote.invalid).toBe(true);
    expect(lote.controls.fechaIngreso.invalid).toBe(true);
  });

  it('rechaza números de lote repetidos sin distinguir mayúsculas ni espacios', () => {
    const formulario = valido();

    formulario.controls.lotes.at(1).controls.numeroLote.setValue(' l-1 ');
    expect(formulario.controls.lotes.hasError('lotesRepetidos')).toBe(true);

    formulario.controls.lotes.removeAt(1);
    expect(formulario.valid).toBe(true);
  });

  it('no considera repetidos los números vacíos', () => {
    const formulario = valido();

    formulario.controls.lotes.controls.forEach((lote) => lote.controls.numeroLote.setValue(''));

    expect(formulario.controls.lotes.hasError('lotesRepetidos')).toBe(false);
  });

  it('arma la solicitud conservando los ids y sin espacios sobrantes', () => {
    const formulario = valido();
    formulario.controls.nombre.setValue(' Teclado nuevo ');
    formulario.controls.activo.setValue(false);
    formulario.controls.lotes.push(loteValido());

    const solicitud = leerSolicitud(formulario);

    expect(solicitud.nombre).toBe('Teclado nuevo');
    expect(solicitud.activo).toBe(false);
    expect(solicitud.lotes.map((lote) => lote.id)).toEqual([1, 2, null]);
  });
});
