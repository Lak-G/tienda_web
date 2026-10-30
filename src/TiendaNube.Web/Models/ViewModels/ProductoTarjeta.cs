namespace TiendaNube.Web.Models.ViewModels;

public class ProductoTarjeta
{
    public int Id { get; init; }
    public string Nombre { get; init; } = string.Empty;
    public decimal Precio { get; init; }
    public int Stock { get; init; }
    public string? ImagenUrl { get; init; }
    public string CategoriaNombre { get; init; } = string.Empty;
}
