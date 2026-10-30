namespace TiendaNube.Web.Models;

public class Orden
{
    public int Id { get; set; }

    public int ClienteId { get; set; }
    public Cliente? Cliente { get; set; }

    public DateTime Fecha { get; set; }

    public decimal Total { get; set; }

    public EstadoOrden Estado { get; set; } = EstadoOrden.Pendiente;

    public ICollection<DetalleOrden> Detalles { get; set; } = new List<DetalleOrden>();
}
