using DbUp;
using DbUp.Engine.Output;
using Inventario.Infraestructura.Configuracion;

namespace Inventario.Infraestructura.Persistencia;

public sealed class MigradorEsquema(OpcionesBaseDatos opciones)
{
    public void Ejecutar()
    {
        EnsureDatabase.For.SqlDatabase(opciones.CadenaConexion, new NoOpUpgradeLog());

        var resultado = DeployChanges.To
            .SqlDatabase(opciones.CadenaConexion)
            .WithScriptsEmbeddedInAssembly(typeof(MigradorEsquema).Assembly)
            .JournalToSqlTable("dbo", "VersionesEsquema")
            .WithTransactionPerScript()
            .LogToNowhere()
            .Build()
            .PerformUpgrade();

        if (!resultado.Successful)
            throw new InvalidOperationException(
                $"No se pudo aplicar el script '{resultado.ErrorScript?.Name}'.", resultado.Error);
    }
}
