namespace Inventario.Pruebas;

internal static class Identidad
{
    public static void Asignar(object entidad, int id) =>
        entidad.GetType().GetProperty("Id")!.SetValue(entidad, id);
}
