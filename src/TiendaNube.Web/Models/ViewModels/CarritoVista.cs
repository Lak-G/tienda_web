namespace TiendaNube.Web.Models.ViewModels;

public class LineaCarrito
{
    public int ProductoId { get; init; }
    public string Nombre { get; init; } = string.Empty;
    public string? ImagenUrl { get; init; }
    public decimal PrecioUnitario { get; init; }
    public int Cantidad { get; init; }
    public int StockDisponible { get; init; }

    public decimal Subtotal => PrecioUnitario * Cantidad;
    public bool HayStockSuficiente => Cantidad <= StockDisponible;
}

public class CarritoVista
{
    public IReadOnlyList<LineaCarrito> Lineas { get; init; } = [];

    // Productos que estaban en la sesión pero ya no existen o se desactivaron
    public IReadOnlyList<string> Avisos { get; init; } = [];

    public decimal Total => Lineas.Sum(l => l.Subtotal);
    public int TotalArticulos => Lineas.Sum(l => l.Cantidad);
    public bool EstaVacio => Lineas.Count == 0;
    public bool PuedePagar => !EstaVacio && Lineas.All(l => l.HayStockSuficiente);
}
