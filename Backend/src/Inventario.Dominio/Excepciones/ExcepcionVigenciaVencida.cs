namespace Inventario.Dominio.Excepciones;

public sealed class ExcepcionVigenciaVencida()
    : ExcepcionDominio("La vigencia de la cuenta ha vencido o no está asignada. Contacte a un administrador.");
