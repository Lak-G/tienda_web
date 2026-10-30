using TiendaNube.Web.Models;
using TiendaNube.Web.Models.ViewModels;

namespace TiendaNube.Web.Services;

public interface IProductoService
{
    Task<IReadOnlyList<Categoria>> ObtenerCategoriasAsync();

    Task<PaginaResultado<ProductoTarjeta>> ObtenerCatalogoAsync(int? categoriaId, string? busqueda, int pagina, int tamanoPagina = 9);

    Task<ProductoDetalle?> ObtenerDetalleAsync(int id);
}
