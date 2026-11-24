namespace Inventario.Aplicacion.Autenticacion.Puertos.Salida;

public interface IHasheadorContrasena
{
    string Hashear(string contrasena);
    bool Verificar(string contrasena, string hash);
}
