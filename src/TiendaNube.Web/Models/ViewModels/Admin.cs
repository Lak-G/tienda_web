namespace TiendaNube.Web.Models.ViewModels;

public class ProductoAdminFila
{
    public int Id { get; init; }
    public string Nombre { get; init; } = string.Empty;
    public string CategoriaNombre { get; init; } = string.Empty;
    public decimal Precio { get; init; }
    public int Stock { get; init; }
    public bool Activo { get; init; }
}

public class InfoEliminacionProducto
{
    public int Id { get; init; }
    public string Nombre { get; init; } = string.Empty;
    public bool Activo { get; init; }
    public int OrdenesAsociadas { get; init; }

    public bool SeraBorradoLogico => OrdenesAsociadas > 0;
}

public class CategoriaFila
{
    public int Id { get; init; }
    public string Nombre { get; init; } = string.Empty;
    public int CantidadProductos { get; init; }
}

public class OrdenFila
{
    public int Id { get; init; }
    public DateTime Fecha { get; init; }
    public string ClienteNombre { get; init; } = string.Empty;
    public int Articulos { get; init; }
    public decimal Total { get; init; }
    public EstadoOrden Estado { get; init; }
}
