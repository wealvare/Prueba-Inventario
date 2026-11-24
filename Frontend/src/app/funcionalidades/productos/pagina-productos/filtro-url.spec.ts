import { convertToParamMap } from '@angular/router';
import { escribirFiltro, FILTRO_INICIAL, leerFiltro } from './filtro-url';

describe('Filtro en la URL', () => {
  it('usa los valores iniciales sin parámetros', () => {
    expect(leerFiltro(convertToParamMap({}))).toEqual(FILTRO_INICIAL);
  });

  it('ignora los valores no válidos', () => {
    const parametros = {
      pagina: '-1',
      tamanoPagina: '7',
      estado: 'Otro',
      precioMinimo: 'x',
      precioMaximo: '-5',
    };

    expect(leerFiltro(convertToParamMap(parametros))).toEqual(FILTRO_INICIAL);
  });

  it('solo escribe lo que difiere y se puede volver a leer', () => {
    const filtro = { ...FILTRO_INICIAL, pagina: 2, estado: 'Todos' as const, precioMinimo: 0 };

    const parametros = escribirFiltro(filtro);

    expect(parametros).toEqual({ pagina: 2, estado: 'Todos', precioMinimo: 0 });
    expect(leerFiltro(convertToParamMap(parametros))).toEqual(filtro);
  });
});
