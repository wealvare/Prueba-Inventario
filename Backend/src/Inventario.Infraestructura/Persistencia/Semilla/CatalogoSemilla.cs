using Inventario.Dominio.Productos;

namespace Inventario.Infraestructura.Persistencia.Semilla;

internal static class CatalogoSemilla
{
    private static readonly DateOnly FechaBase = new(2026, 1, 10);

    public static IReadOnlyList<Producto> Crear() =>
    [
        Crear("TEC-001", "Teclado mecánico", "Teclado mecánico con iluminación", "Periféricos", (189000m, 25), (195500m, 40), (201900m, 15)),
        Crear("RAT-001", "Ratón inalámbrico", "Ratón óptico de seis botones", "Periféricos", (68000m, 60), (71500m, 35)),
        Crear("CAM-001", "Cámara web HD", "Cámara web 1080p con micrófono", "Periféricos", (142000m, 18), (149900m, 22)),
        Crear("AUD-001", "Audífonos con micrófono", "Audífonos de diadema USB", "Audio", (115000m, 30), (119900m, 28), (124500m, 12)),
        Crear("PAR-001", "Parlante portátil", "Parlante inalámbrico resistente al agua", "Audio", (210000m, 14), (225000m, 20)),
        Crear("MON-001", "Monitor 24 pulgadas", "Monitor Full HD con panel IPS", "Monitores", (620000m, 10), (648000m, 16)),
        Crear("MON-002", "Monitor 27 pulgadas", "Monitor QHD con panel IPS", "Monitores", (1180000m, 6), (1225000m, 8), (1249900m, 5)),
        Crear("POR-001", "Portátil 14 pulgadas", "Portátil con 16 GB de memoria y 512 GB", "Computadores", (2890000m, 7), (2975000m, 9)),
        Crear("POR-002", "Portátil 16 pulgadas", "Portátil con 32 GB de memoria y 1 TB", "Computadores", (4350000m, 4), (4480000m, 3)),
        Crear("SSD-001", "Unidad SSD 1 TB", "Unidad de estado sólido NVMe", "Almacenamiento", (298000m, 32), (315000m, 26), (322500m, 18)),
        Crear("USB-001", "Memoria USB 64 GB", "Memoria USB 3.2", "Almacenamiento", (32000m, 120), (34500m, 90)),
        Crear("ROU-001", "Enrutador Wi-Fi 6", "Enrutador de doble banda", "Redes", (289000m, 15), (305000m, 11)),
        Crear("SWI-001", "Conmutador de 8 puertos", "Conmutador Gigabit no administrable", "Redes", (98000m, 24), (102500m, 0)),
        Crear("CAB-001", "Cable HDMI 2 metros", "Cable HDMI 2.1", "Accesorios", (28500m, 80), (29900m, 75), (31200m, 60)),
        Crear("SOP-001", "Soporte para portátil", "Soporte plegable de aluminio", "Accesorios", (79000m, 33), (84500m, 21))
    ];

    private static Producto Crear(
        string codigo,
        string nombre,
        string descripcion,
        string categoria,
        params (decimal Precio, int Cantidad)[] lotes)
    {
        var producto = new Producto(codigo, nombre, descripcion, categoria);
        producto.SincronizarLotes(lotes
            .Select((lote, indice) => new DatosLote(
                null,
                $"{codigo}-L{indice + 1}",
                FechaBase.AddMonths(indice),
                lote.Precio,
                lote.Cantidad))
            .ToList());
        return producto;
    }
}
