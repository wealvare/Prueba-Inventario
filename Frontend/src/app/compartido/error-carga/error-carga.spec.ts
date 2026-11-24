import { TestBed } from '@angular/core/testing';
import { ErrorApi } from '../../nucleo/modelos/error-api';
import { ErrorCarga } from './error-carga';

describe('ErrorCarga', () => {
  const texto = async (error: Error) => {
    const fixture = TestBed.createComponent(ErrorCarga);
    fixture.componentRef.setInput('error', error);
    await fixture.whenStable();
    return (fixture.nativeElement as HTMLElement).textContent;
  };

  it('muestra el código de seguimiento en un error del servidor', async () => {
    const contenido = await texto(new ErrorApi(500, 'Falló.', 'abc-1'));

    expect(contenido).toContain('Falló.');
    expect(contenido).toContain('Código de seguimiento: abc-1');
  });

  it('no lo muestra en los demás errores', async () => {
    expect(await texto(new ErrorApi(404, 'No existe.', 'abc-1'))).not.toContain('abc-1');
  });
});
