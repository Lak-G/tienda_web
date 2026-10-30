namespace TiendaNube.Web.Services;

public record ResultadoOperacion(bool Exito, string? Error = null)
{
    public static ResultadoOperacion Ok() => new(true);
    public static ResultadoOperacion Fallo(string error) => new(false, error);
}

public record ResultadoOrden(bool Exito, int? OrdenId = null, string? Error = null)
{
    public static ResultadoOrden Ok(int ordenId) => new(true, ordenId);
    public static ResultadoOrden Fallo(string error) => new(false, null, error);
}
