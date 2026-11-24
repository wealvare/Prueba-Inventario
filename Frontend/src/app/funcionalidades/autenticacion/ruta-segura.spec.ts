import { rutaSegura } from './ruta-segura';

describe('rutaSegura', () => {
  it('conserva las rutas internas', () => {
    expect(rutaSegura('/productos/nuevo?a=1')).toBe('/productos/nuevo?a=1');
  });

  it.each([
    undefined,
    '',
    'https://otro-sitio.local',
    '//otro-sitio.local',
    '/\\otro-sitio.local',
    '/iniciar-sesion?retorno=/x',
  ])('reemplaza %s por la ruta por defecto', (retorno) => {
    expect(rutaSegura(retorno)).toBe('/productos');
  });
});
