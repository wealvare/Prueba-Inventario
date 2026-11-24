using Inventario.Dominio.Productos;

namespace Inventario.Infraestructura.Persistencia.Semilla;

internal static class CatalogoSemilla
{
    private static readonly DateOnly FechaBase = new(2026, 1, 10);

    public static IReadOnlyList<Producto> Crear() =>
    [
        Crear("TEC-001", "Teclado mecánico", "Teclado mecánico con iluminación", "Periféricos", (74.90m, 25), (79.50m, 40), (84.99m, 15)),
        Crear("RAT-001", "Ratón inalámbrico", "Ratón óptico de seis botones", "Periféricos", (24.90m, 60), (27.50m, 35)),
        Crear("CAM-001", "Cámara web HD", "Cámara web 1080p con micrófono", "Periféricos", (49.90m, 18), (54.99m, 22)),
        Crear("AUD-001", "Audífonos con micrófono", "Audífonos de diadema USB", "Audio", (39.90m, 30), (42.50m, 28), (45.99m, 12)),
        Crear("PAR-001", "Parlante portátil", "Parlante inalámbrico resistente al agua", "Audio", (69.90m, 14), (75.00m, 20)),
        Crear("MON-001", "Monitor 24 pulgadas", "Monitor Full HD con panel IPS", "Monitores", (159.90m, 10), (169.00m, 16)),
        Crear("MON-002", "Monitor 27 pulgadas", "Monitor QHD con panel IPS", "Monitores", (299.00m, 6), (315.50m, 8), (329.99m, 5)),
        Crear("POR-001", "Portátil 14 pulgadas", "Portátil con 16 GB de memoria y 512 GB", "Computadores", (899.00m, 7), (949.00m, 9)),
        Crear("POR-002", "Portátil 16 pulgadas", "Portátil con 32 GB de memoria y 1 TB", "Computadores", (1899.00m, 4), (1999.99m, 3)),
        Crear("SSD-001", "Unidad SSD 1 TB", "Unidad de estado sólido NVMe", "Almacenamiento", (84.90m, 32), (89.99m, 26), (94.50m, 18)),
        Crear("USB-001", "Memoria USB 64 GB", "Memoria USB 3.2", "Almacenamiento", (8.50m, 120), (9.25m, 90)),
        Crear("ROU-001", "Enrutador Wi-Fi 6", "Enrutador de doble banda", "Redes", (89.90m, 15), (96.50m, 11)),
        Crear("SWI-001", "Conmutador de 8 puertos", "Conmutador Gigabit no administrable", "Redes", (24.50m, 24), (26.90m, 0)),
        Crear("CAB-001", "Cable HDMI 2 metros", "Cable HDMI 2.1", "Accesorios", (5.90m, 80), (6.50m, 75), (7.25m, 60)),
        Crear("SOP-001", "Soporte para portátil", "Soporte plegable de aluminio", "Accesorios", (22.90m, 33), (25.50m, 21))
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
