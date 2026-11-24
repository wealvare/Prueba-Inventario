namespace Inventario.Aplicacion.RegistrosCliente.Dtos;

public sealed record SolicitudRegistroCliente(
    NivelRegistroCliente? Nivel,
    string Mensaje,
    string Ruta,
    string? Tipo,
    string? Correlacion);
