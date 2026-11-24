namespace Inventario.Aplicacion.RegistrosCliente.Dtos;

public sealed record RegistroCliente(
    NivelRegistroCliente Nivel,
    string Mensaje,
    string Ruta,
    string? Tipo,
    string? Correlacion);
