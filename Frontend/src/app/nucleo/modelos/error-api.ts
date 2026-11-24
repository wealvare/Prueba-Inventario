export interface ProblemaApi {
  detail?: string;
  correlacion?: string;
  errors?: Record<string, string[]>;
}

export class ErrorApi extends Error {
  constructor(
    readonly estado: number,
    mensaje: string,
    readonly correlacion?: string,
    readonly errores: Record<string, string[]> = {},
  ) {
    super(mensaje);
    this.name = 'ErrorApi';
  }
}
