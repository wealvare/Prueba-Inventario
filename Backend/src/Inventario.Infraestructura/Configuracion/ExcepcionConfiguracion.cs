namespace Inventario.Infraestructura.Configuracion;

public sealed class ExcepcionConfiguracion(string mensaje) : Exception(mensaje);
