import {
  HttpClient,
  HttpErrorResponse,
  HttpRequest,
  provideHttpClient,
  withInterceptors,
} from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { throwError } from 'rxjs';
import { entorno } from '../../../entornos/entorno';
import { ErrorApi } from '../modelos/error-api';
import { ServicioAutenticacion, URL_INICIO_SESION } from '../servicios/servicio-autenticacion';
import { ServicioNotificaciones } from '../servicios/servicio-notificaciones';
import { ServicioRegistro } from '../servicios/servicio-registro';
import { interceptorErrores } from './interceptor-errores';
import { interceptorToken } from './interceptor-token';

const URL = `${entorno.urlApi}/productos`;

describe('Interceptores', () => {
  const autenticacion = { obtenerToken: vi.fn(), expirarSesion: vi.fn() };
  const registro = { advertencia: vi.fn(), advertenciaImportante: vi.fn(), error: vi.fn() };
  let controlador: HttpTestingController;

  const fallar = (
    estado: number,
    cuerpo: object | string | null = null,
    headers: Record<string, string> = {},
    url = URL,
  ): ErrorApi => {
    let error!: ErrorApi;
    TestBed.inject(HttpClient)
      .get(url)
      .subscribe({ error: (e) => (error = e) });
    const peticion = controlador.expectOne(url);

    if (estado === 0) {
      peticion.error(new ProgressEvent('error'));
    } else {
      peticion.flush(cuerpo, { status: estado, statusText: 'Error', headers });
    }

    return error;
  };

  const notificaciones = () => TestBed.inject(ServicioNotificaciones).notificaciones();

  beforeEach(() => {
    vi.clearAllMocks();
    autenticacion.obtenerToken.mockReturnValue('token');
    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        provideHttpClient(withInterceptors([interceptorErrores, interceptorToken])),
        provideHttpClientTesting(),
        { provide: ServicioAutenticacion, useValue: autenticacion },
        { provide: ServicioRegistro, useValue: registro },
      ],
    });
    controlador = TestBed.inject(HttpTestingController);
  });

  afterEach(() => controlador.verify());

  describe('de token', () => {
    const autorizacion = (url: string) => {
      TestBed.inject(HttpClient).get(url).subscribe();
      const peticion = controlador.expectOne(url);
      peticion.flush({});
      return peticion.request.headers.get('Authorization');
    };

    it('agrega el token a las peticiones de la API', () => {
      expect(autorizacion(URL)).toBe('Bearer token');
    });

    it('no agrega nada sin sesión', () => {
      autenticacion.obtenerToken.mockReturnValue(null);

      expect(autorizacion(URL)).toBeNull();
    });

    it.each([
      URL_INICIO_SESION,
      'https://otro-sitio.local/datos',
      `${entorno.urlApi}.otro.local/datos`,
    ])('no envía el token a %s', (url) => {
      expect(autorizacion(url)).toBeNull();
    });
  });

  describe('de errores', () => {
    it.each([
      [0, 'No se pudo conectar con el servidor. Revisa tu conexión e inténtalo de nuevo.'],
      [400, 'Revisa los datos enviados.'],
      [403, 'No tienes permiso para realizar esta acción.'],
      [404, 'El recurso solicitado no existe.'],
      [409, 'La operación entra en conflicto con los datos actuales.'],
      [429, 'Demasiadas peticiones. Espera un momento antes de volver a intentarlo.'],
      [500, 'Ocurrió un error inesperado. Inténtalo de nuevo más tarde.'],
      [503, 'Ocurrió un error inesperado. Inténtalo de nuevo más tarde.'],
    ])('traduce el estado %i y lo notifica', (estado, mensaje) => {
      const error = fallar(estado);

      expect(error).toBeInstanceOf(ErrorApi);
      expect(error.estado).toBe(estado);
      expect(error.message).toBe(mensaje);
      expect(notificaciones().map(({ tipo, mensaje }) => [tipo, mensaje])).toEqual([
        ['error', mensaje],
      ]);
    });

    it('avisa y registra como error una respuesta correcta con JSON inválido', () => {
      let error!: ErrorApi;
      TestBed.runInInjectionContext(() =>
        interceptorErrores(new HttpRequest('GET', URL), () =>
          throwError(() => new HttpErrorResponse({ status: 200, error: { text: '<html>' } })),
        ),
      ).subscribe({ error: (e) => (error = e) });

      expect(error.message).toBe(
        'La respuesta del servidor no es válida. Inténtalo de nuevo más tarde.',
      );
      expect(registro.error).toHaveBeenCalledOnce();
      expect(notificaciones()).toHaveLength(1);
    });

    it('usa el detalle de ProblemDetails y conserva los errores por campo', () => {
      const error = fallar(400, {
        detail: 'Datos incorrectos.',
        correlacion: 'abc',
        errors: { codigo: ['El código es obligatorio.'] },
      });

      expect(error.message).toBe('Datos incorrectos.');
      expect(error.correlacion).toBe('abc');
      expect(error.errores).toEqual({ codigo: ['El código es obligatorio.'] });
    });

    it('en un 500 oculta el detalle y muestra la correlación', () => {
      fallar(500, { detail: 'detalle interno', correlacion: 'abc-123' });

      expect(notificaciones()[0].mensaje).not.toContain('detalle interno');
      expect(notificaciones()[0].correlacion).toBe('abc-123');
    });

    it('toma la correlación del encabezado si el cuerpo no es ProblemDetails', () => {
      expect(fallar(500, 'texto', { 'X-Correlacion': 'xyz' }).correlacion).toBe('xyz');
    });

    it('indica la espera cuando la API la informa', () => {
      expect(fallar(429, null, { 'Retry-After': '30' }).message).toBe(
        'Demasiadas peticiones. Vuelve a intentarlo en 30 segundos.',
      );
    });

    it('en un 401 expira la sesión', () => {
      fallar(401);

      expect(autenticacion.expirarSesion).toHaveBeenCalledOnce();
      expect(notificaciones()).toEqual([]);
    });

    it('en un 401 del inicio de sesión muestra el mensaje sin expirar la sesión', () => {
      fallar(
        401,
        { detail: 'El usuario o la contraseña no son correctos.' },
        {},
        URL_INICIO_SESION,
      );

      expect(autenticacion.expirarSesion).not.toHaveBeenCalled();
      expect(notificaciones()[0].mensaje).toBe('El usuario o la contraseña no son correctos.');
    });

    it('registra cada error con su nivel, sin cuerpos ni token', () => {
      fallar(409, { correlacion: 'c-1' });
      fallar(500, { correlacion: 'c-2' });

      expect(registro.advertencia).toHaveBeenCalledWith(expect.any(String), {
        metodo: 'GET',
        url: URL,
        estado: 409,
        correlacion: 'c-1',
      });
      expect(registro.error).toHaveBeenCalledWith(expect.any(String), {
        metodo: 'GET',
        url: URL,
        estado: 500,
        correlacion: 'c-2',
      });
    });

    it.each([403, 405, 429])('registra el estado %i como advertencia importante', (estado) => {
      fallar(estado);

      expect(registro.advertenciaImportante).toHaveBeenCalledOnce();
      expect(registro.advertencia).not.toHaveBeenCalled();
    });

    it('no repite una notificación con el mismo mensaje', () => {
      fallar(403);
      fallar(403);

      expect(notificaciones()).toHaveLength(1);
    });

    it('no interviene en peticiones ajenas a la API', () => {
      fallar(500, null, {}, 'https://otro-sitio.local/datos');

      expect(notificaciones()).toEqual([]);
      expect(registro.error).not.toHaveBeenCalled();
    });
  });
});
