namespace TiendaNube.Web.Services;

/// <param name="Conflicto">El registro cambió en la BD desde que se leyó (token xmin distinto).</param>
public record ResultadoOperacion(bool Exito, string? Error = null, bool Conflicto = false)
{
    public static ResultadoOperacion Ok() => new(true);
    public static ResultadoOperacion Fallo(string error) => new(false, error);
    public static ResultadoOperacion FalloPorConflicto(string error) => new(false, error, Conflicto: true);
}

public record ResultadoOrden(bool Exito, int? OrdenId = null, string? Error = null)
{
    public static ResultadoOrden Ok(int ordenId) => new(true, ordenId);
    public static ResultadoOrden Fallo(string error) => new(false, null, error);
}
