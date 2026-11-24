namespace Inventario.Aplicacion.Autenticacion.Puertos.Salida;

public interface IHasheadorContrasena
{
    string Hashear(string contrasena);
    string HashFicticio { get; }
    bool Verificar(string contrasena, string hash);
}
