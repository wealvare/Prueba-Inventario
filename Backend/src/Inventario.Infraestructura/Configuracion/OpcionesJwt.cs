using System.Text;
using Microsoft.Extensions.Configuration;

namespace Inventario.Infraestructura.Configuracion;

public sealed record OpcionesJwt(string Clave, string Emisor, string Audiencia, int MinutosExpiracion)
{
    private const int BytesMinimosClave = 32;

    public static OpcionesJwt Leer(IConfiguration configuracion)
    {
        var opciones = new OpcionesJwt(
            configuracion.Requerido("Jwt:Clave"),
            configuracion.Requerido("Jwt:Emisor"),
            configuracion.Requerido("Jwt:Audiencia"),
            configuracion.EnteroPositivo("Jwt:MinutosExpiracion"));

        if (Encoding.UTF8.GetByteCount(opciones.Clave) < BytesMinimosClave)
            throw new InvalidOperationException($"La clave JWT debe tener al menos {BytesMinimosClave} bytes.");

        return opciones;
    }
}
