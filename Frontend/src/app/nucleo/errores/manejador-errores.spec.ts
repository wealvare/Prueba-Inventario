import { TestBed } from '@angular/core/testing';
import { ServicioNotificaciones } from '../servicios/servicio-notificaciones';
import { ServicioRegistro } from '../servicios/servicio-registro';
import { ManejadorErrores } from './manejador-errores';

describe('ManejadorErrores', () => {
  it('registra el error y avisa en español', () => {
    const registro = { error: vi.fn() };
    TestBed.configureTestingModule({
      providers: [ManejadorErrores, { provide: ServicioRegistro, useValue: registro }],
    });
    const error = new TypeError('Failed to fetch dynamically imported module');

    TestBed.inject(ManejadorErrores).handleError(error);

    expect(registro.error).toHaveBeenCalledWith(expect.any(String), { error });
    expect(TestBed.inject(ServicioNotificaciones).notificaciones()[0].mensaje).toBe(
      'No se pudo completar la acción. Revisa tu conexión o recarga la página.',
    );
  });
});
