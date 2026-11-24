import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { interceptorErrores } from '../interceptores/interceptor-errores';
import { interceptorToken } from '../interceptores/interceptor-token';
import { crearToken } from '../../pruebas/token-prueba';
import { URL_REGISTROS } from './envio-registros';
import { ServicioNotificaciones } from './servicio-notificaciones';
import { ServicioRegistro } from './servicio-registro';

describe('ServicioRegistro', () => {
  let servicio: ServicioRegistro;
  let controlador: HttpTestingController;

  beforeEach(() => {
    vi.useFakeTimers();
    vi.spyOn(console, 'error').mockImplementation(() => undefined);
    vi.spyOn(console, 'warn').mockImplementation(() => undefined);
    sessionStorage.setItem('inventario.token', crearToken());
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([interceptorErrores, interceptorToken])),
        provideHttpClientTesting(),
      ],
    });
    servicio = TestBed.inject(ServicioRegistro);
    controlador = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    controlador.verify();
    sessionStorage.clear();
    vi.useRealTimers();
    vi.restoreAllMocks();
  });

  const enviado = () => controlador.expectOne(URL_REGISTROS).request;

  it('envía los errores HTTP con su correlación, sin el token ni el mensaje del servidor', () => {
    servicio.error('El correo ana@ejemplo.com ya existe.', {
      metodo: 'GET',
      url: 'http://localhost:5165/api/productos?busqueda=privado',
      estado: 500,
      correlacion: 'c-1',
      ruta: '/productos?busqueda=privado',
    });

    const peticion = enviado();
    expect(peticion.method).toBe('POST');
    expect(peticion.headers.has('Authorization')).toBe(false);
    expect(peticion.body).toEqual({
      nivel: 'Error',
      mensaje: 'GET /api/productos respondió 500',
      ruta: '/productos',
      tipo: undefined,
      correlacion: 'c-1',
    });
  });

  it('envía el tipo y el mensaje de los errores no controlados', () => {
    servicio.error('Error no controlado.', { error: new TypeError('x is undefined') });

    expect(enviado().body).toMatchObject({
      mensaje: 'Error no controlado. x is undefined',
      tipo: 'TypeError',
    });
  });

  it.each([
    ['Bearer abc.def', 'abc.def'],
    ['contrasena=Secreta1', 'Secreta1'],
    ['{"contrasena":"Mi clave 1"}', 'Mi clave 1'],
    ['Unexpected token in JSON "dato privado"', 'dato privado'],
    ['usuario ana@ejemplo.com', 'ana@'],
    ['documento 0912345678', '0912345678'],
    [crearToken(), 'eyJ'],
  ])('oculta los datos sensibles de %s', (texto, prohibido) => {
    servicio.error('Fallo.', { error: new Error(texto) });

    expect(enviado().body.mensaje).not.toContain(prohibido);
  });

  it('recorta los textos al máximo que admite la Api', () => {
    servicio.error('ab '.repeat(400), { ruta: '/' + 'a/'.repeat(300) });

    const { mensaje, ruta } = enviado().body;
    expect(mensaje).toHaveLength(500);
    expect(ruta).toHaveLength(200);
  });

  it('envía las advertencias importantes y deja las demás en la consola', () => {
    servicio.advertencia('Dato no válido.');
    controlador.expectNone(URL_REGISTROS);

    servicio.advertenciaImportante('Acceso denegado.');

    expect(enviado().body.nivel).toBe('Advertencia');
    expect(console.warn).toHaveBeenCalledTimes(2);
  });

  it.each([0, 429, 500])(
    'si el envío falla con %i no reintenta, no avisa ni se registra',
    (estado) => {
      servicio.error('Primero.');
      const peticion = controlador.expectOne(URL_REGISTROS);

      if (estado === 0) {
        peticion.error(new ProgressEvent('error'));
      } else {
        peticion.flush(null, { status: estado, statusText: 'Error' });
      }

      servicio.error('Segundo.');
      controlador.expectNone(URL_REGISTROS);
      expect(console.error).toHaveBeenCalledTimes(2);
      expect(TestBed.inject(ServicioNotificaciones).notificaciones()).toEqual([]);

      vi.advanceTimersByTime(60_000);
      servicio.error('Tercero.');
      expect(enviado().body.mensaje).toBe('Tercero.');
    },
  );

  it('no repite el mismo error dentro de un minuto', () => {
    servicio.error('Repetido.');
    servicio.error('Repetido.');
    enviado();

    vi.advanceTimersByTime(60_000);
    servicio.error('Repetido.');
    enviado();
  });

  it('limita los envíos por minuto', () => {
    for (let i = 0; i < 20; i++) {
      servicio.error(`Error ${i}.`);
    }

    expect(controlador.match(URL_REGISTROS)).toHaveLength(5);
  });

  it('no lanza errores con datos inesperados', () => {
    expect(() => servicio.error('Fallo.', { url: 'http://[', metodo: 'GET' })).not.toThrow();
  });
});
