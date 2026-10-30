namespace TiendaNube.Web.Models;

public class DetalleOrden
{
    public int Id { get; set; }

    public int OrdenId { get; set; }
    public Orden? Orden { get; set; }

    public int ProductoId { get; set; }
    public Producto? Producto { get; set; }

    public int Cantidad { get; set; }

    // Precio al momento de la compra; no se recalcula si cambia el producto
    public decimal PrecioUnitario { get; set; }

    public decimal Subtotal { get; set; }
}
