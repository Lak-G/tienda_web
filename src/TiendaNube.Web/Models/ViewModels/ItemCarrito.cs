namespace TiendaNube.Web.Models.ViewModels;

// Lo único que se guarda en sesión; precios y stock siempre se leen de la BD
public class ItemCarrito
{
    public int ProductoId { get; set; }
    public int Cantidad { get; set; }
}
