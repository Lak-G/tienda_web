namespace TiendaNube.Web.Models.ViewModels;

public class OrdenResumen
{
    public int Id { get; init; }
    public DateTime Fecha { get; init; }
    public decimal Total { get; init; }
    public EstadoOrden Estado { get; init; }
    public string ClienteNombre { get; init; } = string.Empty;
    public string ClienteCorreo { get; init; } = string.Empty;
    public string? ClienteTelefono { get; init; }
    public string? ClienteDireccion { get; init; }
    public IReadOnlyList<LineaOrden> Lineas { get; init; } = [];
}

public class LineaOrden
{
    public string Producto { get; init; } = string.Empty;
    public int Cantidad { get; init; }
    public decimal PrecioUnitario { get; init; }
    public decimal Subtotal { get; init; }
}
