using Inventario.Dominio.Comun;
using Inventario.Dominio.Excepciones;

namespace Inventario.Dominio.Productos;

public sealed class Producto
{
    public const int LongitudCodigo = 30;
    public const int LongitudNombre = 120;
    public const int LongitudDescripcion = 500;
    public const int LongitudCategoria = 80;

    private readonly List<LoteProducto> _lotes = [];
    private readonly List<int> _idsLotesEliminados = [];

    public Producto(string codigo, string nombre, string? descripcion, string categoria, DateOnly? fechaExpira = null)
    {
        Codigo = NormalizarCodigo(codigo);
        Nombre = Texto.Requerido(nombre, LongitudNombre, "nombre");
        Descripcion = Texto.Limitado(descripcion, LongitudDescripcion, "descripción");
        Categoria = Texto.Requerido(categoria, LongitudCategoria, "categoría");
        FechaExpira = fechaExpira;
        Activo = true;
    }

    public int Id { get; private set; }
    public string Codigo { get; private set; }
    public string Nombre { get; private set; }
    public string Descripcion { get; private set; }
    public string Categoria { get; private set; }
    public bool Activo { get; private set; }
    public DateOnly? FechaExpira { get; private set; }

    public IReadOnlyCollection<LoteProducto> Lotes => _lotes;
    public IReadOnlyCollection<int> IdsLotesEliminados => _idsLotesEliminados;

    public static Producto Reconstruir(
        int id,
        string codigo,
        string nombre,
        string descripcion,
        string categoria,
        bool activo,
        IEnumerable<LoteProducto> lotes,
        DateOnly? fechaExpira = null)
    {
        var producto = new Producto(codigo, nombre, descripcion, categoria, fechaExpira) { Id = id, Activo = activo };
        producto._lotes.AddRange(lotes);
        return producto;
    }

    public static string NormalizarCodigo(string codigo) =>
        Texto.Requerido(codigo, LongitudCodigo, "código").ToUpperInvariant();

    public void Actualizar(
        string codigo,
        string nombre,
        string? descripcion,
        string categoria,
        bool activo,
        DateOnly? fechaExpira = null)
    {
        Codigo = NormalizarCodigo(codigo);
        Nombre = Texto.Requerido(nombre, LongitudNombre, "nombre");
        Descripcion = Texto.Limitado(descripcion, LongitudDescripcion, "descripción");
        Categoria = Texto.Requerido(categoria, LongitudCategoria, "categoría");
        Activo = activo;
        FechaExpira = fechaExpira;
    }

    public void Desactivar() => Activo = false;

    public void SincronizarLotes(IReadOnlyCollection<DatosLote> lotes)
    {
        ValidarNumerosUnicos(lotes);

        var ids = lotes.Where(lote => lote.Id.HasValue).Select(lote => lote.Id!.Value).ToList();

        if (ids.Distinct().Count() != ids.Count)
            throw new ExcepcionReglaNegocio("No se puede enviar el mismo lote más de una vez.");

        if (ids.Any(id => _lotes.All(lote => lote.Id != id)))
            throw new ExcepcionLoteInexistente();

        _idsLotesEliminados.AddRange(_lotes.Where(lote => lote.Id != 0 && !ids.Contains(lote.Id)).Select(lote => lote.Id));
        _lotes.RemoveAll(lote => !ids.Contains(lote.Id));

        foreach (var datos in lotes)
        {
            if (datos.Id is null)
                _lotes.Add(new LoteProducto(datos.NumeroLote, datos.FechaIngreso, datos.Precio, datos.Cantidad));
            else
                _lotes.First(lote => lote.Id == datos.Id)
                    .Actualizar(datos.NumeroLote, datos.FechaIngreso, datos.Precio, datos.Cantidad);
        }
    }

    private static void ValidarNumerosUnicos(IEnumerable<DatosLote> lotes)
    {
        var repetido = lotes
            .Where(lote => !string.IsNullOrWhiteSpace(lote.NumeroLote))
            .GroupBy(lote => lote.NumeroLote.Trim(), StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(grupo => grupo.Count() > 1);

        if (repetido is not null)
            throw new ExcepcionLoteDuplicado(repetido.Key);
    }
}
