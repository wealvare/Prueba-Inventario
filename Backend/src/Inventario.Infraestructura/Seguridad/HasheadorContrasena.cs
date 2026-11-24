using Inventario.Aplicacion.Autenticacion.Puertos.Salida;
using BC = BCrypt.Net.BCrypt;

namespace Inventario.Infraestructura.Seguridad;

public sealed class HasheadorContrasena : IHasheadorContrasena
{
    private const int FactorTrabajo = 12;

    public string HashFicticio { get; } = BC.EnhancedHashPassword(Guid.NewGuid().ToString(), FactorTrabajo);

    public string Hashear(string contrasena) => BC.EnhancedHashPassword(contrasena, FactorTrabajo);

    public bool Verificar(string contrasena, string hash)
    {
        try
        {
            return BC.EnhancedVerify(contrasena, hash);
        }
        catch (Exception error) when (error is BCrypt.Net.SaltParseException or ArgumentException)
        {
            return false;
        }
    }
}
