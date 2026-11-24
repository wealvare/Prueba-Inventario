export type TipoNotificacion = 'exito' | 'informacion' | 'error';

export interface Notificacion {
  id: number;
  tipo: TipoNotificacion;
  mensaje: string;
  correlacion?: string;
}
